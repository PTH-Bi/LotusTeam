namespace LotusTeam.Models
{
    public class Skill
    {
        public int SkillID { get; set; }
        public string SkillName { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }

        public ICollection<EmployeeSkill>? EmployeeSkills { get; set; }
    }

}
