namespace LotusTeam.Models
{
    public class Position
    {
        public int PositionID { get; set; }
        public string PositionCode { get; set; } = null!;
        public string PositionName { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<Employees>? Employees { get; set; }
    }

}
