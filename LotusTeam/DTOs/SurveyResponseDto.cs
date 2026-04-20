namespace LotusTeam.DTOs
{
    public class SurveyResponseDto
    {
        public int ResponseID { get; set; }

        public int SurveyID { get; set; }

        public int? EmployeeID { get; set; }

        public DateTime SubmittedDate { get; set; }

        public string ResponseData { get; set; } = null!;
    }
}