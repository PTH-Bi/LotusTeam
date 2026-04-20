using LotusTeam.Models;

namespace LotusTeam.Service
{
    public interface ICompanyInfoService
    {
        Task<List<CompanyInfo>> GetAllAsync();
        Task<CompanyInfo?> GetByIdAsync(int id);
        Task<CompanyInfo> CreateAsync(CompanyInfo model);
        Task<bool> UpdateAsync(CompanyInfo model);
        Task<bool> DeleteAsync(int id);
    }
}
