namespace LotusTeam.DTOs
{
    public class SurveyDto
    {
        public int SurveyID { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAnonymous { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int ResponseCount { get; set; }
    }

    public class CreateSurveyDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAnonymous { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class SubmitSurveyResponseDto
    {
        public int SurveyID { get; set; }
        public int? EmployeeID { get; set; }
        public string ResponseData { get; set; } = null!;
    }

    public class SurveyResponseDto
    {
        public int ResponseID { get; set; }
        public int SurveyID { get; set; }
        public int? EmployeeID { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string ResponseData { get; set; } = null!;
    }
}