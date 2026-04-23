using LotusTeam.Data;
using LotusTeam.DTOs;
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

        // ================= GET ALL =================
        public async Task<List<CompanyInfoDto>> GetAllAsync()
        {
            return await _context.CompanyInfos
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new CompanyInfoDto
                {
                    CompanyID = x.CompanyID,
                    CompanyCode = x.CompanyCode,
                    CompanyName = x.CompanyName,
                    TaxCode = x.TaxCode,
                    Email = x.Email,
                    Phone = x.Phone,
                    Address = x.Address,
                    BankAccount = x.BankAccount,
                    BankName = x.BankName,
                    BankBranch = x.BankBranch,
                    Representative = x.Representative,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        // ================= GET BY ID =================
        public async Task<CompanyInfoDto?> GetByIdAsync(int id)
        {
            return await _context.CompanyInfos
                .Where(x => x.CompanyID == id)
                .Select(x => new CompanyInfoDto
                {
                    CompanyID = x.CompanyID,
                    CompanyCode = x.CompanyCode,
                    CompanyName = x.CompanyName,
                    TaxCode = x.TaxCode,
                    Email = x.Email,
                    Phone = x.Phone,
                    Address = x.Address,
                    BankAccount = x.BankAccount,
                    BankName = x.BankName,
                    BankBranch = x.BankBranch,
                    Representative = x.Representative,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();
        }

        // ================= CREATE =================
        public async Task<CompanyInfoDto> CreateAsync(CreateCompanyInfoDto dto)
        {
            var entity = new CompanyInfo
            {
                CompanyCode = dto.CompanyCode,
                CompanyName = dto.CompanyName,
                TaxCode = dto.TaxCode,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                BankAccount = dto.BankAccount,
                BankName = dto.BankName,
                BankBranch = dto.BankBranch,
                Representative = dto.Representative,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            _context.CompanyInfos.Add(entity);
            await _context.SaveChangesAsync();

            return new CompanyInfoDto
            {
                CompanyID = entity.CompanyID,
                CompanyCode = entity.CompanyCode,
                CompanyName = entity.CompanyName,
                TaxCode = entity.TaxCode,
                Email = entity.Email,
                Phone = entity.Phone,
                Address = entity.Address,
                BankAccount = entity.BankAccount,
                BankName = entity.BankName,
                BankBranch = entity.BankBranch,
                Representative = entity.Representative,
                IsActive = entity.IsActive
            };
        }

        // ================= UPDATE =================
        public async Task<bool> UpdateAsync(int id, UpdateCompanyInfoDto dto)
        {
            var entity = await _context.CompanyInfos.FindAsync(id);
            if (entity == null) return false;

            entity.CompanyCode = dto.CompanyCode;
            entity.CompanyName = dto.CompanyName;
            entity.TaxCode = dto.TaxCode;
            entity.Email = dto.Email;
            entity.Phone = dto.Phone;
            entity.Address = dto.Address;
            entity.BankAccount = dto.BankAccount;
            entity.BankName = dto.BankName;
            entity.BankBranch = dto.BankBranch;
            entity.Representative = dto.Representative;
            entity.IsActive = dto.IsActive;
            entity.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        // ================= DELETE =================
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.CompanyInfos.FindAsync(id);
            if (entity == null) return false;

            // ❗ Soft delete
            entity.IsActive = false;
            entity.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}