using System.Net;
using System.Net.Mail;

namespace LotusTeam.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // Method cũ - giữ để tương thích ngược
        public async Task NotifyHR(string fileName, int score)
        {
            await SendHRNotification(fileName, score, null, null, null, null);
        }

        // Method mới - gửi kèm vị trí
        public async Task NotifyHRWithPosition(string fileName, int score, string position, List<string> matchedSkills)
        {
            await SendHRNotification(fileName, score, position, matchedSkills, null, null);
        }

        // Method mới - gửi đầy đủ thông tin
        public async Task NotifyHRWithFullInfo(
            string candidateName,
            string fileName,
            int score,
            string position,
            List<string> matchedSkills,
            List<string> missingSkills)
        {
            await SendHRNotification(fileName, score, position, matchedSkills, missingSkills, candidateName);
        }

        // Method chính xử lý gửi email
        private async Task SendHRNotification(
            string fileName,
            int score,
            string? position,
            List<string>? matchedSkills,
            List<string>? missingSkills,
            string? candidateName)
        {
            try
            {
                var hrEmail = _config["Email:HR"] ?? "hr@lotusteam.com";
                var senderEmail = _config["Email:Sender"];
                var smtpServer = _config["Email:Smtp"];
                var port = int.Parse(_config["Email:Port"] ?? "587");
                var password = _config["Email:Password"];

                // Tạo subject dựa trên thông tin
                var subject = string.IsNullOrEmpty(position)
                    ? $"📄 CV Mới - Điểm: {score}"
                    : $"🎯 CV Phù Hợp - {position} - Điểm: {score}";

                // Tạo nội dung email HTML
                var body = BuildEmailBody(fileName, score, position, matchedSkills, missingSkills, candidateName);

                var mail = new MailMessage
                {
                    From = new MailAddress(senderEmail!),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(hrEmail);

                var smtp = new SmtpClient(smtpServer!)
                {
                    Port = port,
                    Credentials = new NetworkCredential(senderEmail, password),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(mail);
                _logger.LogInformation("HR notification sent for CV: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send HR notification for CV: {FileName}", fileName);
                throw;
            }
        }

        // Xây dựng nội dung email HTML
        private string BuildEmailBody(
            string fileName,
            int score,
            string? position,
            List<string>? matchedSkills,
            List<string>? missingSkills,
            string? candidateName)
        {
            // Xác định màu sắc dựa trên điểm số
            string scoreColor = score >= 70 ? "green" : (score >= 50 ? "orange" : "red");

            // Xác định khuyến nghị dựa trên điểm số
            string recommendation = score >= 70 ? "✅ Nên phỏng vấn ngay" :
                                    (score >= 50 ? "⚠️ Có thể xem xét" : "❌ Cần xem xét thêm");

            // Build HTML
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-top: none; border-radius: 0 0 10px 10px; }}
        .score {{ font-size: 36px; font-weight: bold; color: {scoreColor}; }}
        .info-row {{ margin: 15px 0; padding: 10px; background: white; border-radius: 5px; }}
        .badge {{ display: inline-block; padding: 5px 10px; margin: 3px; border-radius: 15px; font-size: 12px; }}
        .badge-success {{ background: #d4edda; color: #155724; }}
        .badge-warning {{ background: #fff3cd; color: #856404; }}
        .badge-danger {{ background: #f8d7da; color: #721c24; }}
        .footer {{ text-align: center; margin-top: 20px; padding: 10px; font-size: 12px; color: #6c757d; }}
        hr {{ margin: 20px 0; border: none; border-top: 1px solid #dee2e6; }}
        .button {{ display: inline-block; padding: 10px 20px; background: #007bff; color: white; text-decoration: none; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>📄 Thông báo CV mới</h2>
            <p>Hệ thống tuyển dụng LotusTeam</p>
        </div>
        <div class='content'>
            <div class='info-row'>
                <strong>📎 Tên file:</strong> {fileName}
            </div>";

            if (!string.IsNullOrEmpty(candidateName))
            {
                html += $@"
            <div class='info-row'>
                <strong>👤 Ứng viên:</strong> {candidateName}
            </div>";
            }

            if (!string.IsNullOrEmpty(position))
            {
                html += $@"
            <div class='info-row'>
                <strong>📌 Vị trí phù hợp nhất:</strong> {position}
            </div>";
            }

            html += $@"
            <div class='info-row'>
                <strong>⭐ Điểm đánh giá:</strong> <span class='score'>{score}/100</span>
            </div>";

            if (matchedSkills != null && matchedSkills.Any())
            {
                var skillsHtml = string.Join("", matchedSkills.Select(s => $"<span class='badge badge-success'>{s}</span>"));
                html += $@"
            <div class='info-row'>
                <strong>✅ Kỹ năng match được:</strong><br/>
                {skillsHtml}
            </div>";
            }

            if (missingSkills != null && missingSkills.Any())
            {
                var missingHtml = string.Join("", missingSkills.Select(s => $"<span class='badge badge-danger'>{s}</span>"));
                html += $@"
            <div class='info-row'>
                <strong>⚠️ Kỹ năng còn thiếu:</strong><br/>
                {missingHtml}
            </div>";
            }

            html += $@"
            <hr/>
            <div class='info-row'>
                <strong>💡 Khuyến nghị:</strong> {recommendation}
            </div>
            <div style='text-align: center; margin-top: 20px;'>
                <a href='{_config["AppUrl"]}/candidates' class='button'>📊 Xem danh sách ứng viên</a>
            </div>
        </div>
        <div class='footer'>
            <p>Email được gửi tự động từ hệ thống LotusTeam</p>
            <p>Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
            <p>© {DateTime.Now.Year} LotusTeam - Hệ thống tuyển dụng thông minh</p>
        </div>
    </div>
</body>
</html>";

            return html;
        }

        // Method gửi email thông báo cho ứng viên (nếu cần)
        public async Task NotifyCandidate(string toEmail, string candidateName, string position, int score)
        {
            try
            {
                var subject = $"Kết quả đánh giá hồ sơ - Vị trí {position}";

                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; }}
        .container {{ max-width: 500px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #28a745; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-top: none; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>📧 Cảm ơn bạn đã ứng tuyển</h2>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{candidateName}</strong>,</p>
            <p>Cảm ơn bạn đã gửi hồ sơ ứng tuyển cho vị trí <strong>{position}</strong> tại LotusTeam.</p>
            <p>Hồ sơ của bạn đã được hệ thống đánh giá với số điểm: <strong>{score}/100</strong></p>
            <p>Chúng tôi sẽ liên hệ với bạn trong thời gian sớm nhất nếu hồ sơ phù hợp.</p>
            <hr/>
            <p>Trân trọng,<br/><strong>Đội ngũ nhân sự LotusTeam</strong></p>
        </div>
        <div class='footer'>
            <p>Email được gửi tự động, vui lòng không trả lời email này.</p>
        </div>
    </div>
</body>
</html>";

                var mail = new MailMessage
                {
                    From = new MailAddress(_config["Email:Sender"]!),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);

                var smtp = new SmtpClient(_config["Email:Smtp"]!)
                {
                    Port = int.Parse(_config["Email:Port"]!),
                    Credentials = new NetworkCredential(_config["Email:Sender"], _config["Email:Password"]),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(mail);
                _logger.LogInformation("Candidate notification sent to: {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send candidate notification to: {ToEmail}", toEmail);
            }
        }

        // Method gửi email báo cáo tổng hợp hàng tuần (nếu cần)
        public async Task SendWeeklyReport(string toEmail, Dictionary<string, int> stats)
        {
            try
            {
                var subject = $"📊 Báo cáo tuyển dụng tuần {DateTime.Now:dd/MM/yyyy}";

                var statsHtml = string.Join("", stats.Select(s =>
                    $"<li><strong>{s.Key}:</strong> {s.Value} ứng viên</li>"));

                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #007bff; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-top: none; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>📊 Báo cáo tuyển dụng</h2>
            <p>Tuần {DateTime.Now:dd/MM/yyyy}</p>
        </div>
        <div class='content'>
            <h3>Tổng quan:</h3>
            <ul>
                {statsHtml}
            </ul>
            <hr/>
            <p><a href='{_config["AppUrl"]}/dashboard'>Xem chi tiết trên dashboard</a></p>
        </div>
    </div>
</body>
</html>";

                var mail = new MailMessage
                {
                    From = new MailAddress(_config["Email:Sender"]!),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);

                var smtp = new SmtpClient(_config["Email:Smtp"]!)
                {
                    Port = int.Parse(_config["Email:Port"]!),
                    Credentials = new NetworkCredential(_config["Email:Sender"], _config["Email:Password"]),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(mail);
                _logger.LogInformation("Weekly report sent to: {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send weekly report to: {ToEmail}", toEmail);
            }
        }
    }
}