using LotusTeam.DTOs;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    /// <summary>
    /// API quản lý tài sản (Assets)
    /// </summary>
    [ApiController]
    [Route("api/assets")]
    [Authorize] // yêu cầu đăng nhập
    public class AssetController : ControllerBase
    {
        private readonly IAssetService _service;

        /// <summary>
        /// Constructor inject AssetService
        /// </summary>
        public AssetController(IAssetService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ tài sản
        /// </summary>
        /// <returns>Danh sách tài sản</returns>
        /// <response code="200">Thành công</response>
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetAll()
        {
            var assets = await _service.GetAllAssetsAsync();

            return Ok(new ApiResponse<IEnumerable<object>>
            {
                Success = true,
                Message = "Lấy danh sách tài sản thành công",
                Data = assets
            });
        }

        /// <summary>
        /// Tạo mới tài sản
        /// </summary>
        /// <param name="dto">Thông tin tài sản</param>
        /// <returns>Tài sản vừa tạo</returns>
        /// <response code="200">Tạo thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateAssetDto dto)
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

            var asset = await _service.CreateAssetAsync(dto);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Tạo tài sản thành công",
                Data = asset
            });
        }

        /// <summary>
        /// Gán tài sản cho nhân viên
        /// </summary>
        /// <param name="dto">Thông tin gán tài sản</param>
        /// <returns>Kết quả gán</returns>
        /// <response code="200">Gán thành công</response>
        [HttpPost("assign")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<object>>> Assign([FromBody] AssignAssetDto dto)
        {
            var result = await _service.AssignAssetAsync(dto);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Gán tài sản thành công",
                Data = result
            });
        }

        /// <summary>
        /// Thu hồi tài sản (revoke)
        /// </summary>
        /// <param name="id">ID của bản ghi gán tài sản</param>
        /// <returns>Kết quả thu hồi</returns>
        /// <response code="200">Thu hồi thành công</response>
        /// <response code="404">Không tìm thấy</response>
        [HttpPost("revoke/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<ActionResult<ApiResponse<bool>>> Revoke(int id)
        {
            var result = await _service.RevokeAssetAsync(id);

            if (!result)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Không tìm thấy hoặc tài sản đã được thu hồi"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Thu hồi tài sản thành công",
                Data = true
            });
        }

        /// <summary>
        /// Lấy lịch sử sử dụng tài sản
        /// </summary>
        /// <param name="assetId">ID tài sản</param>
        /// <returns>Lịch sử sử dụng</returns>
        /// <response code="200">Thành công</response>
        [HttpGet("history/{assetId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> History(int assetId)
        {
            var history = await _service.GetAssetHistoryAsync(assetId);

            return Ok(new ApiResponse<IEnumerable<object>>
            {
                Success = true,
                Message = "Lấy lịch sử tài sản thành công",
                Data = history
            });
        }
    }
}