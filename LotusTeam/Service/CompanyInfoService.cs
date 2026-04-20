using LotusTeam.Data;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Service
{
    public class CompanyInfoService : ICompanyInfoService
    {
        private readonly AppDbContext _context;

        public CompanyInfoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CompanyInfo>> GetAllAsync()
        {
            return await _context.CompanyInfos
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<CompanyInfo?> GetByIdAsync(int id)
        {
            return await _context.CompanyInfos.FindAsync(id);
        }

        public async Task<CompanyInfo> CreateAsync(CompanyInfo model)
        {
            _context.CompanyInfos.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(CompanyInfo model)
        {
            var existing = await _context.CompanyInfos.FindAsync(model.CompanyID);
            if (existing == null) return false;

            existing.CompanyCode = model.CompanyCode;
            existing.CompanyName = model.CompanyName;
            existing.TaxCode = model.TaxCode;
            existing.Email = model.Email;
            existing.Phone = model.Phone;
            existing.Address = model.Address;
            existing.BankAccount = model.BankAccount;
            existing.BankName = model.BankName;
            existing.BankBranch = model.BankBranch;
            existing.Representative = model.Representative;
            existing.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.CompanyInfos.FindAsync(id);
            if (entity == null) return false;

            _context.CompanyInfos.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
