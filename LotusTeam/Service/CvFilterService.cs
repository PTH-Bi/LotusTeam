// Services/CvFilterService.cs
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace LotusTeam.Services
{
    /// <summary>
    /// Service lọc CV đơn giản - Sử dụng cho các trường hợp cần đánh giá nhanh
    /// </summary>
    [Obsolete("This service is kept for backward compatibility. Please use IMultiPositionCvFilterService for new features.")]
    public class CvFilterService
    {
        private readonly ILogger<CvFilterService> _logger;

        // Danh sách kỹ năng mở rộng hơn
        private readonly string[] _skills =
        {
            "c#", ".net", "sql", "react", "angular",
            "javascript", "typescript", "python", "java",
            "node.js", "vue", "html", "css", "mongodb",
            "docker", "kubernetes", "aws", "azure", "git"
        };

        // Từ khóa kinh nghiệm
        private readonly string[] _experienceKeywords =
        {
            "senior", "lead", "principal", "expert",
            "junior", "fresher", "intern"
        };

        public CvFilterService(ILogger<CvFilterService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Tính điểm CV dựa trên kỹ năng cơ bản
        /// </summary>
        public int CalculateScore(string cvText)
        {
            if (string.IsNullOrEmpty(cvText))
            {
                _logger.LogWarning("CV text is null or empty");
                return 0;
            }

            cvText = cvText.ToLower();
            int score = 0;
            var matchedSkills = new List<string>();

            // 1. Điểm kỹ năng (tối đa 60 điểm)
            foreach (var skill in _skills)
            {
                if (cvText.Contains(skill))
                {
                    score += 15; // Giảm từ 20 xuống 15 để có thêm các tiêu chí khác
                    matchedSkills.Add(skill);
                }
            }

            // 2. Điểm kinh nghiệm (tối đa 30 điểm)
            int experienceScore = CalculateExperienceScore(cvText);
            score += experienceScore;

            // 3. Bonus điểm chứng chỉ (tối đa 10 điểm)
            int certificateScore = CalculateCertificateScore(cvText);
            score += certificateScore;

            // Giới hạn điểm tối đa 100
            score = Math.Min(score, 100);

            _logger.LogDebug("CV Score: {Score} (Skills: {SkillCount}, Experience: {ExpScore}, Cert: {CertScore})",
                score, matchedSkills.Count, experienceScore, certificateScore);

            return score;
        }

        /// <summary>
        /// Tính điểm kinh nghiệm làm việc
        /// </summary>
        private int CalculateExperienceScore(string cvText)
        {
            int yearsOfExperience = GetYearsOfExperience(cvText);
            int score = 0;

            // Điểm theo số năm
            if (yearsOfExperience >= 5)
                score = 25;
            else if (yearsOfExperience >= 3)
                score = 20;
            else if (yearsOfExperience >= 1)
                score = 10;
            else if (yearsOfExperience > 0)
                score = 5;

            // Bonus cho senior
            if (Regex.IsMatch(cvText, @"\b(senior|lead|principal|expert)\b", RegexOptions.IgnoreCase))
                score += 10;

            // Trừ điểm cho junior/fresher
            if (Regex.IsMatch(cvText, @"\b(junior|fresher|intern|thực tập)\b", RegexOptions.IgnoreCase))
                score -= 5;

            return Math.Max(0, Math.Min(score, 30));
        }

        /// <summary>
        /// Lấy số năm kinh nghiệm từ CV
        /// </summary>
        private int GetYearsOfExperience(string cvText)
        {
            var patterns = new[]
            {
                @"(\d+)\+?\s*năm\s+kinh\s+nghiệm",
                @"(\d+)\+?\s*years?\s+experience",
                @"experience\s+of\s+(\d+)\+?\s*years?",
                @"(\d+)\+?\s*năm"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(cvText.ToLower(), pattern);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int years))
                {
                    return years;
                }
            }

            return 0;
        }

        /// <summary>
        /// Tính điểm chứng chỉ
        /// </summary>
        private int CalculateCertificateScore(string cvText)
        {
            int score = 0;
            var certificates = new[]
            {
                "azure", "aws", "docker", "kubernetes", "pmp",
                "scrum", "toeic", "ielts", "cissp", "ceh"
            };

            foreach (var cert in certificates)
            {
                if (cvText.Contains(cert))
                {
                    score += 2;
                }
            }

            return Math.Min(score, 10);
        }

        /// <summary>
        /// Kiểm tra CV có phù hợp không (điểm >= 40)
        /// </summary>
        public bool IsSuitable(int score)
        {
            bool isSuitable = score >= 40;
            _logger.LogDebug("CV suitability: {IsSuitable} (Score: {Score})", isSuitable, score);
            return isSuitable;
        }

        /// <summary>
        /// Kiểm tra CV có phù hợp với ngưỡng điểm tùy chỉnh
        /// </summary>
        public bool IsSuitable(int score, int minScore)
        {
            return score >= minScore;
        }

        /// <summary>
        /// Lấy danh sách kỹ năng tìm thấy trong CV
        /// </summary>
        public List<string> GetMatchedSkills(string cvText)
        {
            if (string.IsNullOrEmpty(cvText)) return new List<string>();

            cvText = cvText.ToLower();
            var matchedSkills = new List<string>();

            foreach (var skill in _skills)
            {
                if (cvText.Contains(skill))
                {
                    matchedSkills.Add(skill);
                }
            }

            return matchedSkills;
        }

        /// <summary>
        /// Đánh giá chi tiết CV
        /// </summary>
        public CvEvaluationResult EvaluateCv(string cvText)
        {
            var result = new CvEvaluationResult();

            if (string.IsNullOrEmpty(cvText))
            {
                result.Score = 0;
                result.IsSuitable = false;
                result.Message = "CV trống hoặc không có nội dung";
                return result;
            }

            cvText = cvText.ToLower();

            // Tính điểm
            result.Score = CalculateScore(cvText);
            result.IsSuitable = IsSuitable(result.Score);
            result.MatchedSkills = GetMatchedSkills(cvText);
            result.YearsOfExperience = GetYearsOfExperience(cvText);

            // Đánh giá
            if (result.Score >= 80)
                result.Message = "Rất phù hợp - Nên phỏng vấn ngay";
            else if (result.Score >= 60)
                result.Message = "Phù hợp - Có thể phỏng vấn";
            else if (result.Score >= 40)
                result.Message = "Tạm được - Cân nhắc thêm";
            else
                result.Message = "Chưa phù hợp - Lưu hồ sơ";

            // Gợi ý cải thiện
            if (result.MatchedSkills.Count < 3)
            {
                result.Suggestions = "CV thiếu các kỹ năng chính: C#, .NET, SQL, React, Angular";
            }
            else if (result.YearsOfExperience < 2)
            {
                result.Suggestions = "Kinh nghiệm làm việc còn ít, nên bổ sung thêm dự án thực tế";
            }

            _logger.LogInformation("CV evaluated: Score={Score}, Suitable={IsSuitable}",
                result.Score, result.IsSuitable);

            return result;
        }

        /// <summary>
        /// Đánh giá CV bất đồng bộ
        /// </summary>
        public async Task<CvEvaluationResult> EvaluateCvAsync(string cvText)
        {
            return await Task.Run(() => EvaluateCv(cvText));
        }

        /// <summary>
        /// So sánh 2 CV
        /// </summary>
        public int CompareCvs(string cvText1, string cvText2)
        {
            int score1 = CalculateScore(cvText1);
            int score2 = CalculateScore(cvText2);

            return score1.CompareTo(score2);
        }
    }

    /// <summary>
    /// Kết quả đánh giá CV
    /// </summary>
    public class CvEvaluationResult
    {
        public int Score { get; set; }
        public bool IsSuitable { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Suggestions { get; set; }
        public List<string> MatchedSkills { get; set; } = new();
        public int YearsOfExperience { get; set; }

        public override string ToString()
        {
            return $"Score: {Score}/100, Suitable: {IsSuitable}, Years: {YearsOfExperience}, Message: {Message}";
        }
    }
}