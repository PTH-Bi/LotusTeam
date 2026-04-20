using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 


[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "ADMIN,HR")]
public class CompanyBankAccountsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CompanyBankAccountsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IEnumerable<CompanyBankAccountDto>> GetAll()
    {
        return await _context.CompanyBankAccounts
            .Select(a => new CompanyBankAccountDto
            {
                CompanyBankAccountID = a.CompanyBankAccountID,
                CompanyID = a.CompanyID,
                BankPartnerID = a.BankPartnerID,
                AccountNumber = a.AccountNumber,
                AccountName = a.AccountName,
                Branch = a.Branch,
                IsDefault = a.IsDefault
            }).ToListAsync();
    }

    [HttpPost]

    public async Task<IActionResult> Create(CreateCompanyBankAccountDto dto)
    {
        var account = new CompanyBankAccounts
        {
            CompanyID = dto.CompanyID,
            BankPartnerID = dto.BankPartnerID,
            AccountNumber = dto.AccountNumber,
            AccountName = dto.AccountName,
            Branch = dto.Branch,
            CreatedDate = DateTime.Now
        };

        _context.CompanyBankAccounts.Add(account);

        await _context.SaveChangesAsync();

        return Ok();
    }
}