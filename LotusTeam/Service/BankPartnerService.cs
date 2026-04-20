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
    }
}
