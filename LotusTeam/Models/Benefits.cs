namespace LotusTeam.Models
{
    public class Benefit
    {
        public int BenefitID { get; set; }
        public int EmployeeID { get; set; }
        public string InsuranceType { get; set; } = null!;
        public string? InsuranceNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? ContributionRate { get; set; }
        public decimal? CompanyContribution { get; set; }
        public decimal? EmployeeContribution { get; set; }

        public Employees Employee { get; set; } = null!;
    }

}
