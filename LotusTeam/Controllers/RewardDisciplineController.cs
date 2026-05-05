using LotusTeam.DTOs;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/reward-discipline")]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class RewardDisciplineController : ControllerBase
    {
        private readonly IRewardDisciplineService _service;
        private readonly ILogger<RewardDisciplineController> _logger;

        public RewardDisciplineController(IRewardDisciplineService service, ILogger<RewardDisciplineController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =========================================
        // GET REWARDS BY EMPLOYEE
        // =========================================

        /// <summary>
        /// Lấy danh sách khen thưởng của nhân viên
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Danh sách khen thưởng</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("reward/{employeeId}")]
        public async Task<IActionResult> GetRewards(int employeeId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "EmployeeId không hợp lệ",
                    StatusCode = 400
                });
            }

            // Kiểm tra quyền: nhân viên chỉ xem được khen thưởng của mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            try
            {
                var result = await _service.GetRewardsAsync(employeeId);
                return Ok(new ApiResponse<List<RewardDisciplineDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách khen thưởng thành công",
                    Data = result,
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = result.Count,
                        Page = 1,
                        PageSize = result.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách khen thưởng cho nhân viên {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách khen thưởng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =========================================
        // GET DISCIPLINES BY EMPLOYEE
        // =========================================

        /// <summary>
        /// Lấy danh sách kỷ luật của nhân viên
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Danh sách kỷ luật</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("discipline/{employeeId}")]
        public async Task<IActionResult> GetDisciplines(int employeeId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "EmployeeId không hợp lệ",
                    StatusCode = 400
                });
            }

            // Kiểm tra quyền: nhân viên chỉ xem được kỷ luật của mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            try
            {
                var result = await _service.GetDisciplinesAsync(employeeId);
                return Ok(new ApiResponse<List<RewardDisciplineDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách kỷ luật thành công",
                    Data = result,
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = result.Count,
                        Page = 1,
                        PageSize = result.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách kỷ luật cho nhân viên {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách kỷ luật",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =========================================
        // ADD REWARD
        // =========================================

        /// <summary>
        /// Thêm khen thưởng cho nhân viên (Chỉ HR và Admin)
        /// </summary>
        /// <param name="dto">Thông tin khen thưởng</param>
        /// <returns>Kết quả thêm</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        [HttpPost("reward")]
        public async Task<IActionResult> AddReward([FromBody] CreateRewardDisciplineDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    StatusCode = 400
                });
            }

            try
            {
                await _service.AddRewardAsync(dto);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Thêm khen thưởng thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm khen thưởng cho nhân viên {EmployeeId}", dto.EmployeeID);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi thêm khen thưởng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =========================================
        // ADD DISCIPLINE
        // =========================================

        /// <summary>
        /// Thêm kỷ luật cho nhân viên (Chỉ HR và Admin)
        /// </summary>
        /// <param name="dto">Thông tin kỷ luật</param>
        /// <returns>Kết quả thêm</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        [HttpPost("discipline")]
        public async Task<IActionResult> AddDiscipline([FromBody] CreateRewardDisciplineDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    StatusCode = 400
                });
            }

            try
            {
                await _service.AddDisciplineAsync(dto);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Thêm kỷ luật thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm kỷ luật cho nhân viên {EmployeeId}", dto.EmployeeID);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi thêm kỷ luật",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }
    }
}