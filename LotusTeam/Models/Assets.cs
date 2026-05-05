using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore; // Thêm dòng này

namespace LotusTeam.Models
{
    public class Asset
    {
        [Key]
        public int AssetId { get; set; }

        [Required]
        [StringLength(50)]
        public string AssetCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string AssetName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Category { get; set; }

        public int? AssetStatusId { get; set; }

        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [StringLength(100)]
        public string? Manufacturer { get; set; }

        [StringLength(100)]
        public string? Supplier { get; set; }

        public DateOnly? PurchaseDate { get; set; }

        [Precision(18, 2)] // Cần Microsoft.EntityFrameworkCore
        public decimal? Cost { get; set; }

        public short Status { get; set; } = 1;

        public DateOnly? WarrantyExpiry { get; set; }

        public DateOnly? LastMaintenanceDate { get; set; }

        // Navigation properties
        [ForeignKey("AssetStatusId")]
        public virtual AssetStatus? AssetStatus { get; set; }

        public virtual ICollection<EmployeeAsset> EmployeeAssets { get; set; } = new List<EmployeeAsset>();
        public virtual ICollection<AssetIncidents> AssetIncidents { get; set; } = new List<AssetIncidents>();
    }
}