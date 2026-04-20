using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    [Table("PayrollComponents")]
    public class PayrollComponents
    {
        [Key]
        public int PayrollComponentID { get; set; }

        [Required]
        [StringLength(50)]
        public string ComponentCode { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string ComponentName { get; set; } = null!;

        public bool IsTaxable { get; set; }

        public bool IsActive { get; set; }
    }
}
