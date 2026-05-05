using LotusTeam.Service;
using LotusTeam.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    /// <summary>
    /// API chấm công bằng nhận diện khuôn mặt
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu xác thực cho tất cả API
    public class FaceAttendanceController : ControllerBase
    {
        private readonly IFaceAttendanceService _service;
        private readonly ILogger<FaceAttendanceController> _logger;

        public FaceAttendanceController(
            IFaceAttendanceService service,
            ILogger<FaceAttendanceController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Chấm công Check-in bằng nhận diện khuôn mặt
        /// </summary>
        /// <remarks>
        /// **Role truy cập:** TẤT CẢ ROLE (EMPLOYEE, INTERN, PROBATION_STAFF, ...)
        /// 
        /// **Quy trình:**
        /// 1. Nhân viên chụp ảnh khuôn mặt
        /// 2. Hệ thống so sánh với ảnh mẫu đã đăng ký
        /// 3. Nếu độ tương đồng > ngưỡng (70%), chấm công thành công
        /// 
        /// **Lưu ý:** 
        /// - Chỉ được check-in 1 lần/ngày
        /// - Check-in sau 8:30 AM được tính là đi muộn
        /// - Ảnh phải được mã hóa base64, kích thước tối đa 2MB
        /// </remarks>
        [HttpPost("check-in")]
        [ProducesResponseType(typeof(ApiResponse<FaceCheckResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<FaceCheckResultDto>>> CheckIn([FromBody] FaceCheckDto dto)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                // Validate input
                if (dto.EmployeeId <= 0)
                {
                    _logger.LogWarning("Invalid EmployeeId at CheckIn | TraceId: {traceId}", traceId);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mã nhân viên không hợp lệ",
                        Errors = new { EmployeeId = "EmployeeId phải lớn hơn 0" }
                    });
                }

                if (string.IsNullOrEmpty(dto.ImageBase64))
                {
                    _logger.LogWarning("Empty image at CheckIn | TraceId: {traceId}", traceId);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Ảnh khuôn mặt không được để trống"
                    });
                }

                // Verify that employee is checking their own attendance
                var currentEmployeeId = GetCurrentEmployeeId();
                if (currentEmployeeId.HasValue && currentEmployeeId.Value != dto.EmployeeId)
                {
                    var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
                    // Only admins/HR can check-in for others
                    if (!new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF" }.Contains(userRole))
                    {
                        return StatusCode(403, new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Bạn chỉ có thể chấm công cho chính mình"
                        });
                    }
                }

                var result = await _service.CheckIn(dto.EmployeeId, dto.ImageBase64);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        "CheckIn failed | EmployeeId: {empId} | Reason: {msg} | TraceId: {traceId}",
                        dto.EmployeeId, result.Message, traceId);

                    return BadRequest(new ApiResponse<FaceCheckResultDto>
                    {
                        Success = false,
                        Message = result.Message,
                        Data = new FaceCheckResultDto
                        {
                            Confidence = result.Confidence,
                            IsMatched = false
                        }
                    });
                }

                _logger.LogInformation(
                    "CheckIn success | EmployeeId: {empId} | Confidence: {conf} | TraceId: {traceId}",
                    dto.EmployeeId, result.Confidence, traceId);

                return Ok(new ApiResponse<FaceCheckResultDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = new FaceCheckResultDto
                    {
                        Confidence = result.Confidence,
                        IsMatched = true,
                        CheckTime = DateTime.Now,
                        Status = GetAttendanceStatus(DateTime.Now)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception at CheckIn | EmployeeId: {empId} | TraceId: {traceId}",
                    dto.EmployeeId, traceId);

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi xử lý chấm công check-in",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Chấm công Check-out bằng nhận diện khuôn mặt
        /// </summary>
        /// <remarks>
        /// **Role truy cập:** TẤT CẢ ROLE (EMPLOYEE, INTERN, PROBATION_STAFF, ...)
        /// 
        /// **Quy trình:**
        /// 1. Nhân viên chụp ảnh khuôn mặt
        /// 2. Hệ thống so sánh với ảnh mẫu đã đăng ký
        /// 3. Nếu độ tương đồng > ngưỡng (70%), chấm công thành công
        /// 
        /// **Lưu ý:** 
        /// - Chỉ được check-out sau khi đã check-in trong ngày
        /// - Check-out trước 17:00 được tính là về sớm
        /// - Không thể check-out nếu chưa check-in
        /// </remarks>
        [HttpPost("check-out")]
        [ProducesResponseType(typeof(ApiResponse<FaceCheckResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<FaceCheckResultDto>>> CheckOut([FromBody] FaceCheckDto dto)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                if (dto.EmployeeId <= 0)
                {
                    _logger.LogWarning("Invalid EmployeeId at CheckOut | TraceId: {traceId}", traceId);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mã nhân viên không hợp lệ"
                    });
                }

                if (string.IsNullOrEmpty(dto.ImageBase64))
                {
                    _logger.LogWarning("Empty image at CheckOut | TraceId: {traceId}", traceId);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Ảnh khuôn mặt không được để trống"
                    });
                }

                // Verify permission
                var currentEmployeeId = GetCurrentEmployeeId();
                if (currentEmployeeId.HasValue && currentEmployeeId.Value != dto.EmployeeId)
                {
                    var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
                    if (!new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF" }.Contains(userRole))
                    {
                        return StatusCode(403, new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Bạn chỉ có thể chấm công cho chính mình"
                        });
                    }
                }

                var result = await _service.CheckOut(dto.EmployeeId, dto.ImageBase64);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        "CheckOut failed | EmployeeId: {empId} | Reason: {msg} | TraceId: {traceId}",
                        dto.EmployeeId, result.Message, traceId);

                    return BadRequest(new ApiResponse<FaceCheckResultDto>
                    {
                        Success = false,
                        Message = result.Message,
                        Data = new FaceCheckResultDto
                        {
                            Confidence = result.Confidence,
                            IsMatched = false
                        }
                    });
                }

                _logger.LogInformation(
                    "CheckOut success | EmployeeId: {empId} | Confidence: {conf} | TraceId: {traceId}",
                    dto.EmployeeId, result.Confidence, traceId);

                return Ok(new ApiResponse<FaceCheckResultDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = new FaceCheckResultDto
                    {
                        Confidence = result.Confidence,
                        IsMatched = true,
                        CheckTime = DateTime.Now,
                        Status = "Check-out thành công"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception at CheckOut | EmployeeId: {empId} | TraceId: {traceId}",
                    dto.EmployeeId, traceId);

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi xử lý chấm công check-out",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy lịch sử chấm công khuôn mặt của nhân viên
        /// </summary>
        /// <param name="employeeId">Mã nhân viên</param>
        /// <param name="fromDate">Ngày bắt đầu (tùy chọn)</param>
        /// <param name="toDate">Ngày kết thúc (tùy chọn)</param>
        /// <remarks>
        /// **Role truy cập:** 
        /// - EMPLOYEE, INTERN, PROBATION_STAFF: Chỉ xem lịch sử của chính mình
        /// - MANAGER, TEAM_LEADER: Xem lịch sử của nhân viên trong phòng
        /// - ADMIN, HR_MANAGER, HR_STAFF, DIRECTOR: Xem toàn bộ lịch sử
        /// 
        /// **Dữ liệu trả về:**
        /// - Thời gian check-in/check-out
        /// - Độ tin cậy nhận diện
        /// - Trạng thái (đúng giờ/đi muộn/về sớm)
        /// </remarks>
        [HttpGet("history/{employeeId}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<FaceAttendanceHistoryDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<IEnumerable<FaceAttendanceHistoryDto>>>> GetHistory(
            int employeeId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                if (employeeId <= 0)
                {
                    _logger.LogWarning("Invalid employeeId at History | TraceId: {traceId}", traceId);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mã nhân viên không hợp lệ"
                    });
                }

                // Check permission
                var currentEmployeeId = GetCurrentEmployeeId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "EMPLOYEE";

                var canViewHistory = false;

                if (currentEmployeeId.HasValue && currentEmployeeId.Value == employeeId)
                {
                    // View own history
                    canViewHistory = true;
                }
                else if (new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "DIRECTOR" }.Contains(userRole))
                {
                    // Admin/HR can view all
                    canViewHistory = true;
                }
                else if (userRole == "MANAGER" || userRole == "TEAM_LEADER")
                {
                    // Manager can view team members
                    var isInSameDepartment = await _service.IsEmployeeInSameDepartment(currentEmployeeId, employeeId);
                    if (isInSameDepartment)
                    {
                        canViewHistory = true;
                    }
                }

                if (!canViewHistory)
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Bạn không có quyền xem lịch sử chấm công của nhân viên này"
                    });
                }

                var data = await _service.GetHistory(employeeId, fromDate, toDate);

                _logger.LogInformation(
                    "GetHistory success | EmployeeId: {empId} | TraceId: {traceId}",
                    employeeId, traceId);

                return Ok(new ApiResponse<IEnumerable<Service.FaceAttendanceHistoryDto>>
                {
                    Success = true,
                    Message = "Lịch sử chấm công khuôn mặt",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception at GetHistory | EmployeeId: {empId} | TraceId: {traceId}",
                    employeeId, traceId);

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi lấy lịch sử chấm công",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Đăng ký khuôn mặt cho nhân viên (lần đầu)
        /// </summary>
        /// <remarks>
        /// **Role truy cập:** SUPER_ADMIN, ADMIN, HR_MANAGER, HR_STAFF
        /// 
        /// **Quy trình:**
        /// 1. Nhân viên chụp ảnh khuôn mặt lần đầu
        /// 2. Hệ thống lưu vector đặc trưng khuôn mặt
        /// 3. Sử dụng cho các lần chấm công sau
        /// 
        /// **Yêu cầu ảnh:**
        /// - Mặt nhìn thẳng, rõ ràng
        /// - Không đeo kính râm, khẩu trang
        /// - Ánh sáng đầy đủ
        /// </remarks>
        [HttpPost("register-face")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<object>>> RegisterFace([FromBody] RegisterFaceDto dto)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                if (dto.EmployeeId <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mã nhân viên không hợp lệ"
                    });
                }

                if (string.IsNullOrEmpty(dto.ImageBase64))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Ảnh khuôn mặt không được để trống"
                    });
                }

                var result = await _service.RegisterFace(dto.EmployeeId, dto.ImageBase64);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                _logger.LogInformation(
                    "Face registered | EmployeeId: {empId} | TraceId: {traceId}",
                    dto.EmployeeId, traceId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Đăng ký khuôn mặt thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception at RegisterFace | EmployeeId: {empId} | TraceId: {traceId}",
                    dto.EmployeeId, traceId);

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi đăng ký khuôn mặt",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin chấm công hôm nay của nhân viên
        /// </summary>
        /// <remarks>
        /// **Role truy cập:** TẤT CẢ ROLE
        /// 
        /// **Dữ liệu trả về:**
        /// - Đã check-in chưa? Thời gian check-in
        /// - Đã check-out chưa? Thời gian check-out
        /// - Số giờ làm việc hôm nay
        /// - Trạng thái (đúng giờ/đi muộn)
        /// </remarks>
        [HttpGet("today/{employeeId}")]
        [ProducesResponseType(typeof(ApiResponse<TodayAttendanceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<ActionResult<ApiResponse<TodayAttendanceDto>>> GetTodayAttendance(int employeeId)
        {
            try
            {
                if (employeeId <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mã nhân viên không hợp lệ"
                    });
                }

                // Check permission
                var currentEmployeeId = GetCurrentEmployeeId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "EMPLOYEE";

                if (currentEmployeeId != employeeId &&
                    !new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER" }.Contains(userRole))
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Bạn không có quyền xem thông tin này"
                    });
                }

                var data = await _service.GetTodayAttendance(employeeId);

                return Ok(new ApiResponse<Service.TodayAttendanceDto>
                {
                    Success = true,
                    Message = "Thông tin chấm công hôm nay",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today attendance for employee {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi lấy thông tin chấm công",
                    Errors = ex.Message
                });
            }
        }

        #region Helper Methods

        private int? GetCurrentEmployeeId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                // You might need to map UserId to EmployeeId
                // This depends on your User-Employee relationship
                return userId;
            }
            return null;
        }

        private string GetAttendanceStatus(DateTime checkTime)
        {
            var standardTime = new DateTime(checkTime.Year, checkTime.Month, checkTime.Day, 8, 30, 0);

            if (checkTime.TimeOfDay > standardTime.TimeOfDay)
            {
                var lateMinutes = (checkTime - standardTime).TotalMinutes;
                return $"Đi muộn {lateMinutes} phút";
            }

            return "Đúng giờ";
        }

        #endregion
    }

    #region DTO Classes

    /// <summary>
    /// DTO gửi lên khi chấm công bằng khuôn mặt
    /// </summary>
    public class FaceCheckDto
    {
        /// <summary>Mã nhân viên</summary>
        public int EmployeeId { get; set; }

        /// <summary>Ảnh khuôn mặt dạng Base64</summary>
        public string ImageBase64 { get; set; } = null!;
    }

    /// <summary>
    /// DTO đăng ký khuôn mặt
    /// </summary>
    public class RegisterFaceDto
    {
        /// <summary>Mã nhân viên</summary>
        public int EmployeeId { get; set; }

        /// <summary>Ảnh khuôn mặt dạng Base64</summary>
        public string ImageBase64 { get; set; } = null!;
    }

    /// <summary>
    /// Kết quả chấm công bằng khuôn mặt
    /// </summary>
    public class FaceCheckResultDto
    {
        /// <summary>Độ tin cậy nhận diện (0-100%)</summary>
        public double Confidence { get; set; }

        /// <summary>Khuôn mặt có khớp không</summary>
        public bool IsMatched { get; set; }

        /// <summary>Thời gian chấm công</summary>
        public DateTime? CheckTime { get; set; }

        /// <summary>Trạng thái chấm công</summary>
        public string? Status { get; set; }
    }

    /// <summary>
    /// Lịch sử chấm công khuôn mặt
    /// </summary>
    public class FaceAttendanceHistoryDto
    {
        /// <summary>ID bản ghi</summary>
        public int Id { get; set; }

        /// <summary>Mã nhân viên</summary>
        public int EmployeeId { get; set; }

        /// <summary>Tên nhân viên</summary>
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>Ngày chấm công</summary>
        public DateTime Date { get; set; }

        /// <summary>Thời gian check-in</summary>
        public DateTime? CheckInTime { get; set; }

        /// <summary>Thời gian check-out</summary>
        public DateTime? CheckOutTime { get; set; }

        /// <summary>Số giờ làm việc</summary>
        public double? WorkingHours { get; set; }

        /// <summary>Độ tin cậy check-in</summary>
        public double? CheckInConfidence { get; set; }

        /// <summary>Độ tin cậy check-out</summary>
        public double? CheckOutConfidence { get; set; }

        /// <summary>Trạng thái</summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Thông tin chấm công hôm nay
    /// </summary>
    public class TodayAttendanceDto
    {
        /// <summary>Đã check-in chưa</summary>
        public bool HasCheckedIn { get; set; }

        /// <summary>Thời gian check-in</summary>
        public DateTime? CheckInTime { get; set; }

        /// <summary>Đã check-out chưa</summary>
        public bool HasCheckedOut { get; set; }

        /// <summary>Thời gian check-out</summary>
        public DateTime? CheckOutTime { get; set; }

        /// <summary>Số giờ làm việc</summary>
        public double? WorkingHours { get; set; }

        /// <summary>Trạng thái</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Ghi chú</summary>
        public string? Note { get; set; }
    }

    #endregion
}