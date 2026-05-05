// DTOs/PositionMatchResult.cs
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

    // Thêm các field mới về kinh nghiệm
    public int ExperienceYears { get; set; }
    public int ExperienceScore { get; set; }
    public int CertificateScore { get; set; }
    public int EducationScore { get; set; }

    // Đánh giá tổng quan
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