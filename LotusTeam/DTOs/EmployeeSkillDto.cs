namespace LotusTeam.DTOs
{
    public class EmployeeSkillDto
    {
        public int SkillID { get; set; }
        public string SkillName { get; set; } = "";
        public byte? ProficiencyLevel { get; set; }
    }
}