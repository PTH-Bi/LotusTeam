namespace LotusTeam.Models
{
    public class WorkType
    {
        public int WorkTypeID { get; set; }
        public string WorkTypeCode { get; set; } = null!;
        public string WorkTypeName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        public ICollection<Attendances>? Attendances { get; set; }
    }

}
