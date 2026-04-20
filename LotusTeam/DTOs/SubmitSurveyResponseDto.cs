namespace LotusTeam.DTOs
{
    public class SubmitSurveyResponseDto
    {
        public int SurveyID { get; set; }

        public int? EmployeeID { get; set; }

        public string ResponseData { get; set; } = null!;
    }
}