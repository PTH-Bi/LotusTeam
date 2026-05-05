namespace LotusTeam.Models
{
    public class SystemConfig
    {
        public int ConfigID { get; set; }
        public string ConfigKey { get; set; } = null!;
        public string? ConfigValue { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public User? UpdatedUser { get; set; }
    }

}
