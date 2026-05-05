using LotusTeam.DTOs;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CompanyInfoController : ControllerBase
    {
        private readonly ICompanyInfoService _service;

        public CompanyInfoController(ICompanyInfoService service)
        {
            _service = service;
        }

        // ================= GET ALL =================
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CompanyInfoDto>>>> GetAll()
        {
            var data = await _service.GetAllAsync();

            return Ok(new ApiResponse<IEnumerable<CompanyInfoDto>>
            {
                Success = true,
                Message = "Lấy danh sách công ty thành công",
                Data = data
            });
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT")]
        public async Task<ActionResult<ApiResponse<CompanyInfoDto>>> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
            {
                return NotFound(new ApiResponse<CompanyInfoDto>
                {
                    Success = false,
                    Message = "Không tìm thấy công ty"
                });
            }

            return Ok(new ApiResponse<CompanyInfoDto>
            {
                Success = true,
                Data = result
            });
        }

        // ================= CREATE =================
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<CompanyInfoDto>>> Create([FromBody] CreateCompanyInfoDto dto)
        {
            var created = await _service.CreateAsync(dto);

            return Ok(new ApiResponse<CompanyInfoDto>
            {
                Success = true,
                Message = "Tạo công ty thành công",
                Data = created
            });
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateCompanyInfoDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);

            if (!result)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy công ty"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Cập nhật thành công",
                Data = true
            });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy công ty"
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
}