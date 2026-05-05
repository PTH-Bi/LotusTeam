using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Models;
using LotusTeam.Service;
using Microsoft.EntityFrameworkCore;

public class AssetService : IAssetService
{
    private readonly AppDbContext _context;

    public AssetService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AssetDto>> GetAllAssetsAsync()
    {
        return await _context.Assets
            .AsNoTracking()
            .Select(a => new AssetDto
            {
                AssetId = a.AssetId,
                AssetCode = a.AssetCode,
                AssetName = a.AssetName,
                Category = a.Category,
                AssetStatusId = a.AssetStatusId,
                SerialNumber = a.SerialNumber,
                Manufacturer = a.Manufacturer,
                Supplier = a.Supplier,
                PurchaseDate = a.PurchaseDate,
                Cost = a.Cost,
                Status = a.Status,
                WarrantyExpiry = a.WarrantyExpiry,
                LastMaintenanceDate = a.LastMaintenanceDate
            })
            .ToListAsync();
    }

    public async Task<AssetDto> CreateAssetAsync(CreateAssetDto dto)
    {
        var asset = new Asset
        {
            AssetCode = dto.AssetCode,
            AssetName = dto.AssetName,
            Category = dto.Category,
            AssetStatusId = dto.AssetStatusId,
            SerialNumber = dto.SerialNumber,
            Manufacturer = dto.Manufacturer,
            Supplier = dto.Supplier,
            PurchaseDate = dto.PurchaseDate,
            Cost = dto.Cost,
            WarrantyExpiry = dto.WarrantyExpiry,
            LastMaintenanceDate = dto.LastMaintenanceDate,
            Status = 1
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        return new AssetDto
        {
            AssetId = asset.AssetId,
            AssetCode = asset.AssetCode,
            AssetName = asset.AssetName,
            Category = asset.Category,
            AssetStatusId = asset.AssetStatusId,
            SerialNumber = asset.SerialNumber,
            Manufacturer = asset.Manufacturer,
            Supplier = asset.Supplier,
            PurchaseDate = asset.PurchaseDate,
            Cost = asset.Cost,
            Status = asset.Status,
            WarrantyExpiry = asset.WarrantyExpiry,
            LastMaintenanceDate = asset.LastMaintenanceDate
        };
    }

    public async Task<EmployeeAsset> AssignAssetAsync(AssignAssetDto dto)
    {
        var assignment = new EmployeeAsset
        {
            EmployeeID = dto.EmployeeID,
            AssetID = dto.AssetID,
            Notes = dto.Notes,
            AssignDate = DateTime.Now
        };

        _context.EmployeeAssets.Add(assignment);
        await _context.SaveChangesAsync();

        return assignment;
    }

    public async Task<bool> RevokeAssetAsync(int employeeAssetId)
    {
        var ass = await _context.EmployeeAssets
            .Include(x => x.Asset)
            .FirstOrDefaultAsync(x => x.AssignmentID == employeeAssetId); // ✅ đúng field

        if (ass == null)
            return false;

        // ❌ đã thu hồi rồi
        if (ass.ReturnDate != null)
            return false;

        ass.ReturnDate = DateTime.Now;

        // ✅ update trạng thái tài sản
        if (ass.Asset != null)
            ass.Asset.Status = 1; // Available

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<EmployeeAsset>> GetAssetHistoryAsync(int assetId)
    {
        return await _context.EmployeeAssets
            .Include(x => x.Employee)
            .Where(x => x.AssetID == assetId)
            .AsNoTracking()
            .ToListAsync();
    }
}