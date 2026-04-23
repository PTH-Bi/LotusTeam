using LotusTeam.DTOs;
using LotusTeam.Models;

namespace LotusTeam.Service
{
    public interface ICompanyInfoService
    {
        Task<List<CompanyInfoDto>> GetAllAsync();
        Task<CompanyInfoDto?> GetByIdAsync(int id);
        Task<CompanyInfoDto> CreateAsync(CreateCompanyInfoDto dto);
        Task<bool> UpdateAsync(int id, UpdateCompanyInfoDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
