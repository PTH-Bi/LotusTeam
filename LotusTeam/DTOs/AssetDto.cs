namespace LotusTeam.DTOs
{
    public class AssetDto
    {
        public int AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int? AssetStatusId { get; set; }
        public string? SerialNumber { get; set; }
        public string? Manufacturer { get; set; }
        public string? Supplier { get; set; }
        public DateOnly? PurchaseDate { get; set; }
        public decimal? Cost { get; set; }
        public short Status { get; set; }
        public DateOnly? WarrantyExpiry { get; set; }
        public DateOnly? LastMaintenanceDate { get; set; }
    }
}