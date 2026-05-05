using LotusTeam.DTOs;
using LotusTeam.Models;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/performance")]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class PerformanceController : ControllerBase
    {
        private readonly IPerformanceService _service;
        private readonly ILogger<PerformanceController> _logger;

        public PerformanceController(IPerformanceService service, ILogger<PerformanceController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =====================================
        // CREATE REVIEW
        // =====================================

        /// <summary>
        /// Tạo đánh giá hiệu suất cho nhân viên
        /// </summary>
        /// <param name="dto">Thông tin đánh giá</param>
        /// <returns>Đánh giá vừa tạo</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        [HttpPost("review")]
        public async Task<IActionResult> CreateReview([FromBody] CreatePerformanceReviewDto dto)
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
                // Kiểm tra quyền: Manager chỉ được đánh giá nhân viên trong phòng ban của mình
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (currentUserRole == "MANAGER")
                {
                    var employee = await _service.GetEmployeeByIdAsync(dto.EmployeeID);
                    var managerDept = await _service.GetManagerDepartmentAsync(currentUserId);

                    if (employee?.DepartmentID != managerDept)
                    {
                        return Forbid();
                    }
                }

                var model = new PerformanceReview
                {
                    EmployeeId = dto.EmployeeID,
                    ReviewerId = dto.ReviewerId ?? currentUserId,
                    Score = dto.Score,
                    Comments = dto.Comments,
                    ReviewPeriod = dto.ReviewPeriod
                };

                var result = await _service.CreatePerformanceReviewAsync(model);

                var responseDto = new PerformanceReviewDto
                {
                    ReviewId = result.ReviewId,
                    EmployeeID = result.EmployeeId,
                    ReviewDate = result.ReviewDate,
                    ReviewerId = result.ReviewerId,
                    Score = result.Score,
                    Comments = result.Comments,
                    ReviewPeriod = result.ReviewPeriod
                };

                return Ok(new ApiResponse<PerformanceReviewDto>
                {
                    Success = true,
                    Message = "Tạo đánh giá hiệu suất thành công",
                    Data = responseDto,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo đánh giá hiệu suất cho nhân viên {EmployeeId}", dto.EmployeeID);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo đánh giá",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =====================================
        // REVIEW HISTORY
        // =====================================

        /// <summary>
        /// Lấy lịch sử đánh giá hiệu suất của nhân viên
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Danh sách đánh giá</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("review-history/{employeeId:int}")]
        public async Task<IActionResult> ReviewHistory(int employeeId)
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

            // Kiểm tra quyền: nhân viên chỉ xem được lịch sử của mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            try
            {
                var result = await _service.GetReviewHistoryAsync(employeeId);
                return Ok(new ApiResponse<List<PerformanceReviewDto>>
                {
                    Success = true,
                    Message = "Lấy lịch sử đánh giá thành công",
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
                _logger.LogError(ex, "Lỗi khi lấy lịch sử đánh giá cho nhân viên {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy lịch sử đánh giá",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =====================================
        // SKILLS
        // =====================================

        /// <summary>
        /// Lấy danh sách kỹ năng của nhân viên
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Danh sách kỹ năng</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("skills/{employeeId:int}")]
        public async Task<IActionResult> GetSkills(int employeeId)
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

            // Kiểm tra quyền: nhân viên chỉ xem được kỹ năng của mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            try
            {
                var result = await _service.GetEmployeeSkillsAsync(employeeId);
                return Ok(new ApiResponse<List<EmployeeSkillDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách kỹ năng thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy kỹ năng cho nhân viên {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách kỹ năng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Cập nhật kỹ năng cho nhân viên
        /// </summary>
        /// <param name="dto">Thông tin kỹ năng</param>
        /// <returns>Kết quả cập nhật</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        [HttpPost("skills")]
        public async Task<IActionResult> UpdateSkill([FromBody] UpdateEmployeeSkillDto dto)
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
                var skill = new EmployeeSkill
                {
                    EmployeeID = dto.EmployeeID,
                    SkillID = dto.SkillID,
                    ProficiencyLevel = dto.ProficiencyLevel,
                    VerifiedBy = dto.VerifiedBy,
                    Certification = dto.Certification
                };

                await _service.UpdateEmployeeSkillAsync(skill);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Cập nhật kỹ năng thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật kỹ năng cho nhân viên {EmployeeId}", dto.EmployeeID);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật kỹ năng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =====================================
        // TRAINING RECOMMEND
        // =====================================

        /// <summary>
        /// Gợi ý khóa đào tạo cho nhân viên (dựa trên kỹ năng còn thiếu)
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Danh sách khóa đào tạo đề xuất</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        [HttpGet("training-recommend/{employeeId:int}")]
        public async Task<IActionResult> RecommendTraining(int employeeId)
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

            try
            {
                var result = await _service.RecommendTrainingAsync(employeeId);
                return Ok(new ApiResponse<List<Training>>
                {
                    Success = true,
                    Message = "Lấy danh sách khóa đào tạo đề xuất thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gợi ý đào tạo cho nhân viên {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi gợi ý đào tạo",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =====================================
        // CAPABILITY
        // =====================================

        /// <summary>
        /// Lấy tổng quan năng lực của nhân viên (kỹ năng + đánh giá gần đây)
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Thông tin năng lực</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("capability/{employeeId:int}")]
        public async Task<IActionResult> Capability(int employeeId)
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

            // Kiểm tra quyền: nhân viên chỉ xem được năng lực của mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            try
            {
                var result = await _service.GetEmployeeCapabilityAsync(employeeId);
                return Ok(new ApiResponse<CapabilityDto>
                {
                    Success = true,
                    Message = "Lấy thông tin năng lực thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy năng lực cho nhân viên {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin năng lực",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }
    }
}