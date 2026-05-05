using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CompanyBankAccountsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CompanyBankAccountsController(AppDbContext context)
    {
        _context = context;
    }

    // ================= GET ALL =================
    [HttpGet]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,ACCOUNTANT,FINANCE_MANAGER")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CompanyBankAccountDto>>>> GetAll()
    {
        var data = await _context.CompanyBankAccounts
            .Select(a => new CompanyBankAccountDto
            {
                CompanyBankAccountID = a.CompanyBankAccountID,
                CompanyID = a.CompanyID,
                BankPartnerID = a.BankPartnerID,
                AccountNumber = a.AccountNumber,
                AccountName = a.AccountName,
                Branch = a.Branch,
                IsDefault = a.IsDefault
            })
            .ToListAsync();

        return Ok(new ApiResponse<IEnumerable<CompanyBankAccountDto>>
        {
            Success = true,
            Message = "Lấy danh sách tài khoản công ty thành công",
            Data = data
        });
    }

    // ================= CREATE =================
    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,FINANCE_MANAGER,ACCOUNTANT")]
    public async Task<ActionResult<ApiResponse<CompanyBankAccountDto>>> Create([FromBody] CompanyBankAccountDto dto)
    {
        // ❗ Validate bank tồn tại
        var bankExists = await _context.BankPartners
            .AnyAsync(x => x.BankPartnerID == dto.BankPartnerID);

        if (!bankExists)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Ngân hàng không tồn tại"
            });
        }

        // ❗ Nếu set default → bỏ default cũ
        if (dto.IsDefault)
        {
            var oldDefaults = await _context.CompanyBankAccounts
                .Where(x => x.CompanyID == dto.CompanyID && x.IsDefault)
                .ToListAsync();

            foreach (var item in oldDefaults)
            {
                item.IsDefault = false;
            }
        }

        var account = new CompanyBankAccounts
        {
            CompanyID = dto.CompanyID,
            BankPartnerID = dto.BankPartnerID,
            AccountNumber = dto.AccountNumber,
            AccountName = dto.AccountName,
            Branch = dto.Branch,
            IsDefault = dto.IsDefault,
            CreatedDate = DateTime.Now
        };

        _context.CompanyBankAccounts.Add(account);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<CompanyBankAccountDto>
        {
            Success = true,
            Message = "Tạo tài khoản công ty thành công",
            Data = new CompanyBankAccountDto
            {
                CompanyBankAccountID = account.CompanyBankAccountID,
                CompanyID = account.CompanyID,
                BankPartnerID = account.BankPartnerID,
                AccountNumber = account.AccountNumber,
                AccountName = account.AccountName,
                Branch = account.Branch,
                IsDefault = account.IsDefault
            }
        });
    }

    // ================= UPDATE =================
    [HttpPut("{id}")]
    [Authorize(Roles = "SUPER_ADMIN,FINANCE_MANAGER")]
    public async Task<ActionResult<ApiResponse<CompanyBankAccountDto>>> Update(
        int id,
        [FromBody] CompanyBankAccountDto dto)
    {
        var account = await _context.CompanyBankAccounts.FindAsync(id);

        if (account == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Không tìm thấy tài khoản"
            });
        }

        // ❗ Nếu set default → reset cái khác
        if (dto.IsDefault)
        {
            var oldDefaults = await _context.CompanyBankAccounts
                .Where(x => x.CompanyID == account.CompanyID && x.IsDefault)
                .ToListAsync();

            foreach (var item in oldDefaults)
            {
                item.IsDefault = false;
            }
        }

        account.AccountNumber = dto.AccountNumber;
        account.AccountName = dto.AccountName;
        account.Branch = dto.Branch;
        account.IsDefault = dto.IsDefault;

        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<CompanyBankAccountDto>
        {
            Success = true,
            Message = "Cập nhật thành công",
            Data = new CompanyBankAccountDto
            {
                CompanyBankAccountID = account.CompanyBankAccountID,
                CompanyID = account.CompanyID,
                BankPartnerID = account.BankPartnerID,
                AccountNumber = account.AccountNumber,
                AccountName = account.AccountName,
                Branch = account.Branch,
                IsDefault = account.IsDefault
            }
        });
    }

    // ================= DELETE =================
    [HttpDelete("{id}")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var account = await _context.CompanyBankAccounts.FindAsync(id);

        if (account == null)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = "Không tìm thấy tài khoản"
            });
        }

        _context.CompanyBankAccounts.Remove(account);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "Xóa thành công",
            Data = true
        });
    }
}