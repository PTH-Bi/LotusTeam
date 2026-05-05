using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LotusTeam.Models
{
    public class AssetStatus
    {
        [Key]
        public int StatusId { get; set; }

        [Required]
        [StringLength(50)]
        public string StatusCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string StatusName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }
}