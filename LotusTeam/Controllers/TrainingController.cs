using LotusTeam.DTOs;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/training")]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class TrainingController : ControllerBase
    {
        private readonly ITrainingService _service;
        private readonly ILogger<TrainingController> _logger;

        public TrainingController(ITrainingService service, ILogger<TrainingController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ======================
        // COURSE LIST
        // ======================

        /// <summary>
        /// Lấy danh sách tất cả khóa học
        /// </summary>
        /// <returns>Danh sách khóa học</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses()
        {
            try
            {
                var result = await _service.GetAllCoursesAsync();
                return Ok(new ApiResponse<List<CourseDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách khóa học thành công",
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
                _logger.LogError(ex, "Lỗi khi lấy danh sách khóa học");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách khóa học",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // ======================
        // CREATE COURSE
        // ======================

        /// <summary>
        /// Tạo mới khóa học (Chỉ HR)
        /// </summary>
        /// <param name="dto">Thông tin khóa học</param>
        /// <returns>Khóa học vừa tạo</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        [HttpPost("courses")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
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
                var result = await _service.CreateCourseAsync(dto);
                return Ok(new ApiResponse<CourseDto>
                {
                    Success = true,
                    Message = "Tạo khóa học thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo khóa học");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo khóa học",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // ======================
        // ENROLL EMPLOYEE
        // ======================

        /// <summary>
        /// Đăng ký khóa học cho nhân viên (Chỉ HR)
        /// </summary>
        /// <param name="dto">Thông tin đăng ký</param>
        /// <returns>Kết quả đăng ký</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        [HttpPost("enroll")]
        public async Task<IActionResult> EnrollEmployee([FromBody] EnrollEmployeeDto dto)
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
                await _service.EnrollEmployeeAsync(dto);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Đăng ký khóa học thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký khóa học cho nhân viên {EmployeeId}", dto.EmployeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi đăng ký khóa học",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // ======================
        // EMPLOYEE COURSES
        // ======================

        /// <summary>
        /// Lấy danh sách khóa học của nhân viên
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Danh sách khóa học</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetEmployeeCourses(int employeeId)
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

            // Kiểm tra quyền: nhân viên chỉ xem được khóa học của mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            try
            {
                var result = await _service.GetEmployeeCoursesAsync(employeeId);
                return Ok(new ApiResponse<List<CourseDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách khóa học của nhân viên thành công",
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
                _logger.LogError(ex, "Lỗi khi lấy danh sách khóa học của nhân viên {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách khóa học",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // ======================
        // UPDATE RESULT
        // ======================

        /// <summary>
        /// Cập nhật kết quả đào tạo (Chỉ HR)
        /// </summary>
        /// <param name="enrollmentId">ID đăng ký</param>
        /// <param name="statusId">ID trạng thái</param>
        /// <param name="completionDate">Ngày hoàn thành</param>
        /// <param name="certificatePath">Đường dẫn chứng chỉ</param>
        /// <returns>Kết quả cập nhật</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        [HttpPut("result/{enrollmentId}")]
        public async Task<IActionResult> UpdateResult(
            int enrollmentId,
            [FromQuery] short statusId,
            [FromQuery] DateOnly? completionDate,
            [FromQuery] string? certificatePath)
        {
            if (enrollmentId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "EnrollmentId không hợp lệ",
                    StatusCode = 400
                });
            }

            try
            {
                await _service.UpdateTrainingResultAsync(enrollmentId, statusId, completionDate, certificatePath);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Cập nhật kết quả đào tạo thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật kết quả đào tạo EnrollmentId {EnrollmentId}", enrollmentId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật kết quả đào tạo",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // ======================
        // CERTIFICATES
        // ======================

        /// <summary>
        /// Lấy danh sách chứng chỉ của nhân viên
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Danh sách chứng chỉ</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("certificates/{employeeId}")]
        public async Task<IActionResult> GetCertificates(int employeeId)
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

            // Kiểm tra quyền: nhân viên chỉ xem được chứng chỉ của mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            try
            {
                var result = await _service.GetEmployeeCertificatesAsync(employeeId);
                return Ok(new ApiResponse<List<CourseDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách chứng chỉ thành công",
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
                _logger.LogError(ex, "Lỗi khi lấy danh sách chứng chỉ của nhân viên {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách chứng chỉ",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }
    }
}