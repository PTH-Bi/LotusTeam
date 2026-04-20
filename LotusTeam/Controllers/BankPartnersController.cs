using LotusTeam.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "ADMIN,HR")]
public class BankPartnersController : ControllerBase
{
    private readonly IBankPartnerService _service;

    public BankPartnersController(IBankPartnerService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BankPartnerDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BankPartnerDto>> Get(int id)
    {
        var bank = await _service.GetByIdAsync(id);

        if (bank == null)
            return NotFound();

        return Ok(bank);
    }

    [HttpPost]
    public async Task<ActionResult<BankPartnerDto>> Create(CreateBankPartnerDto dto)
    {
        var bank = await _service.CreateAsync(dto);

        return Ok(bank);
    }
}