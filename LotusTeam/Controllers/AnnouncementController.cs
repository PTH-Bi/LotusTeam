using LotusTeam.DTOs;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    /// <summary>
    /// API quản lý thông báo (Announcements)
    /// </summary>
    [ApiController]
    [Route("api/announcements")]
    [Authorize] // Bắt buộc đăng nhập
    public class AnnouncementController : ControllerBase
    {
        private readonly IAnnouncementService _service;

        public AnnouncementController(IAnnouncementService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy tất cả thông báo
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<ActionResult<ApiResponse<IEnumerable<AnnouncementDto>>>> GetAll()
        {
            var data = await _service.GetAllAsync();

            return Ok(new ApiResponse<IEnumerable<AnnouncementDto>>
            {
                Success = true,
                Message = "Lấy danh sách thông báo thành công",
                Data = data
            });
        }

        /// <summary>
        /// Lấy thông báo theo ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,EMPLOYEE,INTERN,PROBATION_STAFF")]
        public async Task<ActionResult<ApiResponse<AnnouncementDto>>> Get(int id)
        {
            var item = await _service.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound(new ApiResponse<AnnouncementDto>
                {
                    Success = false,
                    Message = $"Không tìm thấy thông báo ID = {id}"
                });
            }

            return Ok(new ApiResponse<AnnouncementDto>
            {
                Success = true,
                Message = "Lấy thông báo thành công",
                Data = item
            });
        }

        /// <summary>
        /// Tạo thông báo mới
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<AnnouncementDto>>> Create([FromBody] AnnouncementCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState
                });
            }

            var result = await _service.CreateAsync(dto);

            return Ok(new ApiResponse<AnnouncementDto>
            {
                Success = true,
                Message = "Tạo thông báo thành công",
                Data = result
            });
        }

        /// <summary>
        /// Cập nhật thông báo
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<AnnouncementDto>>> Update(int id, [FromBody] AnnouncementUpdateDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);

            if (updated == null)
            {
                return NotFound(new ApiResponse<AnnouncementDto>
                {
                    Success = false,
                    Message = $"Không tìm thấy thông báo ID = {id}"
                });
            }

            return Ok(new ApiResponse<AnnouncementDto>
            {
                Success = true,
                Message = "Cập nhật thông báo thành công",
                Data = updated
            });
        }

        /// <summary>
        /// Xóa thông báo
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Không tìm thấy thông báo ID = {id}"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Xóa thông báo thành công",
                Data = true
            });
        }
    }
}