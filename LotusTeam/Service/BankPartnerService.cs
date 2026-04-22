using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;


namespace LotusTeam.Service
{
    public class BankPartnerService : IBankPartnerService
    {
        private readonly AppDbContext _context;

        public BankPartnerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<BankPartnerDto>> GetAllAsync()
        {
            return await _context.BankPartners
                .Select(b => new BankPartnerDto
                {
                    BankPartnerID = b.BankPartnerID,
                    BankCode = b.BankCode,
                    BankName = b.BankName,
                    ShortName = b.ShortName,
                    SwiftCode = b.SwiftCode,
                    IsActive = b.IsActive
                })
                .ToListAsync();
        }

        public async Task<BankPartnerDto?> GetByIdAsync(int id)
        {
            return await _context.BankPartners
                .Where(b => b.BankPartnerID == id)
                .Select(b => new BankPartnerDto
                {
                    BankPartnerID = b.BankPartnerID,
                    BankCode = b.BankCode,
                    BankName = b.BankName,
                    ShortName = b.ShortName,
                    SwiftCode = b.SwiftCode,
                    IsActive = b.IsActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<BankPartnerDto> CreateAsync(CreateBankPartnerDto dto)
        {
            var bank = new BankPartner
            {
                BankCode = dto.BankCode,
                BankName = dto.BankName,
                ShortName = dto.ShortName,
                SwiftCode = dto.SwiftCode,
                CreatedDate = DateTime.Now
            };

            _context.BankPartners.Add(bank);

            await _context.SaveChangesAsync();

            return new BankPartnerDto
            {
                BankPartnerID = bank.BankPartnerID,
                BankCode = bank.BankCode,
                BankName = bank.BankName
            };
        }

        public async Task<BankPartnerDto?> UpdateAsync(int id, UpdateBankPartnerDto dto)
        {
            var bank = await _context.BankPartners.FindAsync(id);

            if (bank == null)
                return null;

            // check trùng BankCode
            var isDuplicate = await _context.BankPartners
                .AnyAsync(x => x.BankCode == dto.BankCode && x.BankPartnerID != id);

            if (isDuplicate)
                throw new Exception("Mã ngân hàng đã tồn tại");

            bank.BankCode = dto.BankCode;
            bank.BankName = dto.BankName;
            bank.ShortName = dto.ShortName;
            bank.SwiftCode = dto.SwiftCode;
            bank.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return new BankPartnerDto
            {
                BankPartnerID = bank.BankPartnerID,
                BankCode = bank.BankCode,
                BankName = bank.BankName,
                ShortName = bank.ShortName,
                SwiftCode = bank.SwiftCode,
                IsActive = bank.IsActive
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var bank = await _context.BankPartners.FindAsync(id);

            if (bank == null)
                return false;

            // ❗ Check đang được dùng không
            var isUsed = await _context.CompanyBankAccounts
                .AnyAsync(x => x.BankPartnerID == id);

            if (isUsed)
                throw new Exception("Ngân hàng đang được sử dụng, không thể xóa");

            // ✅ Soft delete
            bank.IsActive = false;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
