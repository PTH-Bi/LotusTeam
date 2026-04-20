namespace LotusTeam.Models
{
    public class EmployeeSkill
    {
        public int EmployeeSkillID { get; set; }
        public int EmployeeID { get; set; }
        public int SkillID { get; set; }
        public byte? ProficiencyLevel { get; set; }
        public string? Certification { get; set; }
        public int? VerifiedBy { get; set; } // EmployeeID của người xác nhận
        public DateTime? VerifiedDate { get; set; }

        public Employees Employee { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
        public Employees? Verifier { get; set; }
    }
    
}
