namespace LotusTeam.Models
{
    public class Holiday
    {
        public int HolidayID { get; set; }
        public DateTime HolidayDate { get; set; }
        public string HolidayName { get; set; } = null!;
        public bool IsPaid { get; set; }
        public string? Description { get; set; }
    }

}
