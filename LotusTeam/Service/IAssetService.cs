using LotusTeam.DTOs;
using LotusTeam.Models;

namespace LotusTeam.Service
{
    public interface IAssetService
    {
        Task<List<AssetDto>> GetAllAssetsAsync();
        Task<AssetDto> CreateAssetAsync(CreateAssetDto dto);

        Task<EmployeeAsset> AssignAssetAsync(AssignAssetDto dto);
        Task<bool> RevokeAssetAsync(int id);

        Task<List<EmployeeAsset>> GetAssetHistoryAsync(int assetId);
    }
}