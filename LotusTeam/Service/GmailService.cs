using Google.Apis.Gmail.v1;
using LotusTeam.Data;
using LotusTeam.Helpers;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LotusTeam.Services
{
    public class GmailService
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GmailService> _logger;
        private readonly PdfParserService _pdfParser;
        private readonly CvFilterService _cvFilter;
        private readonly EmailService _emailService;
        private readonly IMultiPositionCvFilterService _multiPositionFilter;


        public GmailService(
            IConfiguration config,
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<GmailService> logger,
            PdfParserService pdfParser,
            CvFilterService cvFilter,
            EmailService emailService,
            IMultiPositionCvFilterService multiPositionFilter)
        {
            _config = config;
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _pdfParser = pdfParser;
            _cvFilter = cvFilter;
            _emailService = emailService;
            _multiPositionFilter = multiPositionFilter;
        }

        // =====================================================
        // CHECK EMAIL
        // =====================================================

        public async Task CheckUnreadEmailsAsync()
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken)) return;

                var url =
                    "https://gmail.googleapis.com/gmail/v1/users/me/messages?q=is:unread has:attachment (filename:pdf OR filename:doc OR filename:docx)";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return;

                var result = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(result);

                if (!doc.RootElement.TryGetProperty("messages", out var messages))
                    return;

                foreach (var msg in messages.EnumerateArray())
                {
                    var messageId = msg.GetProperty("id").GetString();
                    if (string.IsNullOrEmpty(messageId)) continue;

                    bool exists = await _context.ProcessedEmails
                        .AnyAsync(x => x.MessageId == messageId);

                    if (exists) continue;

                    var resultProcess =
                        await ProcessMessageAsync(messageId, accessToken);

                    if (!resultProcess.HasCv) continue;

                    _context.ProcessedEmails.Add(new ProcessedEmails
                    {
                        MessageId = messageId,
                        SenderEmail = resultProcess.Sender,
                        Subject = resultProcess.Subject,
                        ProcessedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await MarkAsReadAsync(messageId, accessToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckUnreadEmailsAsync error");
            }
        }

        // =====================================================
        // PROCESS MESSAGE
        // =====================================================

        private async Task<(bool HasCv, string? Sender, string? Subject)>
            ProcessMessageAsync(string messageId, string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{messageId}?format=full");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return (false, null, null);

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var payload = doc.RootElement.GetProperty("payload");

                string? sender = null;
                string? subject = null;

                if (payload.TryGetProperty("headers", out var headers))
                {
                    foreach (var header in headers.EnumerateArray())
                    {
                        var name = header.GetProperty("name").GetString();
                        var value = header.GetProperty("value").GetString();

                        if (name == "From") sender = value;
                        if (name == "Subject") subject = value;
                    }
                }

                var hasCv =
                    await ExtractAttachmentsAsync(payload, messageId, accessToken);

                return (hasCv, sender, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessMessageAsync error");
                return (false, null, null);
            }
        }

        // =====================================================
        // EXTRACT ATTACHMENTS
        // =====================================================

        private async Task<bool> ExtractAttachmentsAsync(
            JsonElement payload,
            string messageId,
            string accessToken)
        {
            bool foundCv = false;

            if (payload.TryGetProperty("filename", out var filenameProp))
            {
                var filename = filenameProp.GetString();

                if (!string.IsNullOrEmpty(filename) &&
                    IsValidCvFile(filename) &&
                    payload.TryGetProperty("body", out var body) &&
                    body.TryGetProperty("attachmentId", out var attachmentIdProp))
                {
                    var attachmentId = attachmentIdProp.GetString();

                    if (!string.IsNullOrEmpty(attachmentId))
                    {
                        await DownloadAttachmentAsync(
                            messageId,
                            attachmentId,
                            filename,
                            accessToken);

                        foundCv = true;
                    }
                }
            }

            if (payload.TryGetProperty("parts", out var parts))
            {
                foreach (var part in parts.EnumerateArray())
                {
                    if (await ExtractAttachmentsAsync(part, messageId, accessToken))
                        foundCv = true;
                }
            }

            return foundCv;
        }

        // =====================================================
        // DOWNLOAD + PROCESS CV
        // =====================================================

        private async Task DownloadAttachmentAsync(
            string messageId,
            string attachmentId,
            string filename,
            string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{messageId}/attachments/{attachmentId}");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var data = doc.RootElement.GetProperty("data").GetString();
                if (string.IsNullOrEmpty(data)) return;

                var bytes = DecodeBase64Url(data);

                var folder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Uploads",
                    "CV");

                Directory.CreateDirectory(folder);

                var uniqueFileName = $"{Guid.NewGuid()}_{filename}";
                var filePath = Path.Combine(folder, uniqueFileName);

                await File.WriteAllBytesAsync(filePath, bytes);

                _logger.LogInformation("Saved CV: {File}", filePath);

                // ===============================
                // PARSE + SAVE CV
                // ===============================

                if (Path.GetExtension(filename).ToLower() == ".pdf")
                {
                    var text = _pdfParser.ExtractText(filePath);

                    if (string.IsNullOrWhiteSpace(text)) return;

                    // TÍNH ĐIỂM CHO TẤT CẢ VỊ TRÍ
                    var allPositionScores = await _multiPositionFilter.CalculateScoresForAllPositions(text);
                    var bestMatch = allPositionScores.FirstOrDefault();

                    // TẠO CANDIDATE
                    var candidate = await CreateCandidateFromCvAsync(uniqueFileName, text, bestMatch);

                    if (bestMatch != null)
                    {
                        // LƯU CV
                        var cv = new CandidateCVs
                        {
                            CandidateID = candidate.CandidateId,
                            FileName = uniqueFileName,
                            FilePath = filePath,
                            CvText = text,
                            Score = bestMatch.Score,
                            IsSuitable = bestMatch.IsSuitable,
                            BestMatchedPosition = bestMatch.PositionName,
                            IsViewedByHR = false,
                            CreatedAt = DateTime.Now
                        };

                        _context.CandidateCVs.Add(cv);
                        await _context.SaveChangesAsync();

                        // LƯU MATCH CHO TẤT CẢ VỊ TRÍ (hoặc chỉ lưu top 3)
                        var topMatches = allPositionScores.Take(3);
                        foreach (var match in topMatches)
                        {
                            var positionMatch = new CandidatePositionMatch
                            {
                                CandidateId = candidate.CandidateId,
                                JobPositionId = match.PositionId ?? 0,
                                CandidateCVID = cv.CandidateCVID,
                                TotalScore = match.Score,
                                IsSuitable = match.IsSuitable,
                                MatchedSkills = string.Join(", ", match.MatchedSkills),
                                MatchedAt = DateTime.Now
                            };

                            // Chỉ thêm nếu JobPositionId hợp lệ
                            if (match.PositionId.HasValue && match.PositionId.Value > 0)
                            {
                                _context.CandidatePositionMatches.Add(positionMatch);
                            }
                        }

                        await _context.SaveChangesAsync();

                        // CẬP NHẬT APPLIED POSITION CHO CANDIDATE
                        if (bestMatch.IsSuitable)
                        {
                            candidate.AppliedPosition = bestMatch.PositionName;
                            candidate.StatusId = 2; // Under Review
                            await _context.SaveChangesAsync();

                            // GỬI EMAIL THÔNG BÁO CHO HR
                            await _emailService.NotifyHRWithFullInfo(
                                candidate.FullName,
                                uniqueFileName,
                                bestMatch.Score,
                                bestMatch.PositionName,
                                bestMatch.MatchedSkills,
                                bestMatch.MissingRequiredSkills);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownloadAttachmentAsync error");
            }
        }

        // =====================================================
        // CREATE / GET CANDIDATE
        // =====================================================

        private async Task<Candidates> CreateCandidateFromCvAsync(string fileName, string content, PositionMatchResult? bestMatch)
        {
            var email = Regex.Match(content,
                @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")
                .Value;

            var phone = Regex.Match(content, @"\+?\d{9,12}").Value;

            // Lấy tên từ dòng đầu tiên có độ dài hợp lý
            var nameLine = content
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(x => x.Length > 5 && x.Length < 100 && !x.Contains("@") && !x.Contains("http"));

            var existed = await _context.Candidates
                .FirstOrDefaultAsync(x => x.Email == email);

            if (existed != null)
                return existed;

            var candidate = new Candidates
            {
                FullName = !string.IsNullOrEmpty(nameLine) ? nameLine.Trim() : "Unknown",
                Email = string.IsNullOrEmpty(email) ? null : email,
                Phone = string.IsNullOrEmpty(phone) ? null : phone,
                ResumePath = fileName,
                ResumeContent = content,
                AppliedPosition = bestMatch?.PositionName ?? "Unknown",
                AppliedDate = DateTime.Now,
                StatusId = 1, // New
                Notes = bestMatch != null ? $"Best match: {bestMatch.PositionName} with score {bestMatch.Score}" : null
            };

            _context.Candidates.Add(candidate);
            await _context.SaveChangesAsync();

            return candidate;
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static bool IsValidCvFile(string filename)
        {
            var ext = Path.GetExtension(filename).ToLower();
            return ext == ".pdf" || ext == ".doc" || ext == ".docx";
        }

        private static byte[] DecodeBase64Url(string input)
        {
            string padded = input.Replace('-', '+').Replace('_', '/');

            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            return Convert.FromBase64String(padded);
        }

        // =====================================================
        // TOKEN
        // =====================================================

        public async Task<string?> GetAccessTokenAsync()
        {
            var tokenEntity = await _context.GoogleTokens
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (tokenEntity == null)
                return null;

            var refreshToken = TokenEncryption.Decrypt(tokenEntity.RefreshToken);

            var values = new Dictionary<string, string>
            {
                { "client_id", _config["Google:ClientId"]! },
                { "client_secret", _config["Google:ClientSecret"]! },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" }
            };

            var response = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(values));

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("access_token").GetString();
        }

        // =====================================================
        // MARK READ
        // =====================================================

        private async Task MarkAsReadAsync(string messageId, string accessToken)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{messageId}/modify");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var content = new { removeLabelIds = new[] { "UNREAD" } };

            request.Content = new StringContent(
                JsonSerializer.Serialize(content),
                Encoding.UTF8,
                "application/json");

            await _httpClient.SendAsync(request);
        }
    }
}