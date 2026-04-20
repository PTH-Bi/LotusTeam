namespace LotusTeam.DTOs
{
    public class CreateSurveyDto
    {
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsAnonymous { get; set; }

        public int? CreatedBy { get; set; }
    }
}