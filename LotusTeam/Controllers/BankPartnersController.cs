using LotusTeam.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BankPartnersController : ControllerBase
{
    private readonly IBankPartnerService _service;

    public BankPartnersController(IBankPartnerService service)
    {
        _service = service;
    }

    // ================= GET ALL =================
    [HttpGet]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT,FINANCE_MANAGER")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BankPartnerDto>>>> GetAll()
    {
        var data = await _service.GetAllAsync();

        return Ok(new ApiResponse<IEnumerable<BankPartnerDto>>
        {
            Success = true,
            Message = "Lấy danh sách ngân hàng thành công",
            Data = data
        });
    }

    // ================= GET BY ID =================
    [HttpGet("{id}")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT,FINANCE_MANAGER")]
    public async Task<ActionResult<ApiResponse<BankPartnerDto>>> Get(int id)
    {
        var bank = await _service.GetByIdAsync(id);

        if (bank == null)
        {
            return NotFound(new ApiResponse<BankPartnerDto>
            {
                Success = false,
                Message = "Không tìm thấy ngân hàng"
            });
        }

        return Ok(new ApiResponse<BankPartnerDto>
        {
            Success = true,
            Data = bank
        });
    }

    // ================= CREATE =================
    [HttpPost]
    [Authorize(Roles = "ADMIN,SUPER_ADMIN,FINANCE_MANAGER,ACCOUNTANT")]
    public async Task<ActionResult<ApiResponse<BankPartnerDto>>> Create([FromBody] CreateBankPartnerDto dto)
    {
        var bank = await _service.CreateAsync(dto);

        return Ok(new ApiResponse<BankPartnerDto>
        {
            Success = true,
            Message = "Tạo ngân hàng thành công",
            Data = bank
        });
    }

    // ================= UPDATE =================
    [HttpPut("{id}")]
    [Authorize(Roles = "SUPER_ADMIN,FINANCE_MANAGER,ACCOUNTANT")]
    public async Task<ActionResult<ApiResponse<BankPartnerDto>>> Update(int id, [FromBody] UpdateBankPartnerDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);

        if (updated == null)
        {
            return NotFound(new ApiResponse<BankPartnerDto>
            {
                Success = false,
                Message = "Không tìm thấy ngân hàng"
            });
        }

        return Ok(new ApiResponse<BankPartnerDto>
        {
            Success = true,
            Message = "Cập nhật thành công",
            Data = updated
        });
    }

    // ================= DELETE =================
    [HttpDelete("{id}")]
    [Authorize(Roles = "SUPER_ADMIN,FINANCE_MANAGER")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = "Không tìm thấy ngân hàng"
            });
        }

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "Xóa thành công",
            Data = true
        });
    }
}