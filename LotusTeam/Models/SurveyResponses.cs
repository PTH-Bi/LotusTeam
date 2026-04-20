namespace LotusTeam.Models
{
    public class SurveyResponse
    {
        public int ResponseID { get; set; }
        public int SurveyID { get; set; }
        public int? EmployeeID { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string ResponseData { get; set; } = null!;

        public Survey Survey { get; set; } = null!;
        public Employees? Employee { get; set; }
    }

}
