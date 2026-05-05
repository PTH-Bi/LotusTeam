using System.ComponentModel.DataAnnotations;

namespace LotusTeam.DTOs
{
    public class RewardDisciplineDto
    {
        public int RDID { get; set; }

        public int EmployeeID { get; set; }

        public byte Type { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime RDDate { get; set; }

        public string? DecisionNumber { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public short? StatusID { get; set; }
    }


}