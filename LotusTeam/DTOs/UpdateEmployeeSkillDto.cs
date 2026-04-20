namespace LotusTeam.DTOs
{
    public class UpdateEmployeeSkillDto
    {
        public int EmployeeID { get; set; }

        public int SkillID { get; set; }

        public byte? ProficiencyLevel { get; set; }

        public int? VerifiedBy { get; set; }

        public string? Certification { get; set; }
    }
}