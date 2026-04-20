namespace LotusTeam.Models
{
    public class StatusMasters
    {
        public short StatusID { get; set; }
        public string StatusCode { get; set; } = null!;
        public string StatusName { get; set; } = null!;
        public string Module { get; set; } = null!;
        public bool IsActive { get; set; }
    }

}
