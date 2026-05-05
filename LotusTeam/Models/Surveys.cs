namespace LotusTeam.Models
{
    public class Survey
    {
        public int SurveyID { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAnonymous { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        public User? Creator { get; set; }
        public ICollection<SurveyResponse>? SurveyResponses { get; set; }
    }

}
