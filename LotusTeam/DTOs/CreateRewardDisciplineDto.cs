namespace LotusTeam.DTOs
{
    public class CreateRewardDisciplineDto
    {
        public int EmployeeID { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public string? DecisionNumber { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public short? StatusID { get; set; }
    }
}