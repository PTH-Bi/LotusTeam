namespace LotusTeam.Models
{
    public class RewardsDisciplines
    {
        public int RDID { get; set; }
        public int? RequestID { get; set; }
        public int EmployeeID { get; set; }
        public byte Type { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime RDDate { get; set; }
        public string? DecisionNumber { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public short? StatusID { get; set; }

        public Employees Employee { get; set; } = null!;
    }

}
