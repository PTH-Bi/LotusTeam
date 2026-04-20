namespace LotusTeam.Models
{
    public class Gender
    {
        public byte GenderID { get; set; }
        public string GenderCode { get; set; } = null!;
        public string GenderName { get; set; } = null!;

        public ICollection<Employees>? Employees { get; set; }
    }

}
