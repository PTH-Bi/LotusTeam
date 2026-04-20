// Services/IMultiPositionCvFilterService.cs
using LotusTeam.Models;

namespace LotusTeam.Services
{
    public interface IMultiPositionCvFilterService
    {
        Task<List<PositionMatchResult>> CalculateScoresForAllPositions(string cvText);
        Task<PositionMatchResult?> GetBestMatchPosition(string cvText);
        Task<List<CandidateWithPositionMatch>> GetCandidatesByPosition(string positionName, int minScore = 40);
        Task<JobPosition?> CreateJobPosition(JobPositionDto dto);
        Task<JobSkill?> AddSkillToPosition(int positionId, JobSkillDto dto);
        Task<List<JobPositionDto>> GetAllActivePositions();
        Task<PositionStatisticsDto> GetPositionStatistics();
        Task InitializeDefaultPositionsAsync();
        Task<PositionMatchResult?> CalculateScoreForPosition(int positionId, string cvText);
        void InvalidatePositionCache();
    }

    public class PositionMatchResult
    {
        public int? PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Score { get; set; }
        public bool IsSuitable { get; set; }
        public int MinScoreRequired { get; set; }
        public List<string> MatchedSkills { get; set; } = new();
        public List<string> MissingRequiredSkills { get; set; } = new();

        // Thêm các property về kinh nghiệm
        public int ExperienceYears { get; set; }
        public int ExperienceScore { get; set; }
        public int CertificateScore { get; set; }
        public int EducationScore { get; set; }

        public string Summary => GetSummary();

        private string GetSummary()
        {
            if (Score >= 80)
                return "Rất phù hợp - Nên phỏng vấn ngay";
            if (Score >= 60)
                return "Phù hợp - Có thể phỏng vấn";
            if (Score >= 40)
                return "Tạm được - Cân nhắc thêm";
            return "Chưa phù hợp - Lưu hồ sơ";
        }
    }

    public class CandidateWithPositionMatch
    {
        public int CandidateId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public int Score { get; set; }
        public string? MatchedSkills { get; set; }
        public DateTime AppliedDate { get; set; }
    }

    public class JobPositionDto
    {
        public int? JobPositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MinScoreRequired { get; set; } = 40;
        public bool IsActive { get; set; } = true;
        public List<JobSkillDto> Skills { get; set; } = new();
    }

    public class JobSkillDto
    {
        public int? JobSkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public int Weight { get; set; } = 20;
        public bool IsRequired { get; set; } = false;
    }

    public class PositionStatisticsDto
    {
        public int TotalPositions { get; set; }
        public int TotalCandidates { get; set; }
        public List<PositionStats> PositionStats { get; set; } = new();
    }

    public class PositionStats
    {
        public string PositionName { get; set; } = string.Empty;
        public int TotalCandidates { get; set; }
        public int SuitableCandidates { get; set; }
        public double AverageScore { get; set; }
    }
}