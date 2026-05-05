using LotusTeam.Services;
using LotusTeam.Models;
using LotusTeam.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/leave")]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveService _service;

        public LeaveController(ILeaveService service)
        {
            _service = service;
        }

        // ================= LOẠI NGHỈ =================

        /// <summary>
        /// Lấy danh sách tất cả các loại nghỉ
        /// </summary>
        /// <returns>Danh sách loại nghỉ</returns>
        [HttpGet("types")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE,INTERN,PROBATION_STAFF")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            var result = await _service.GetLeaveTypesAsync();
            return Ok(new ApiResponse<List<LeaveType>>
            {
                Success = true,
                Message = "Lấy danh sách loại nghỉ thành công",
                Data = result,
                StatusCode = 200
            });
        }

        /// <summary>
        /// Tạo mới loại nghỉ (Chỉ quản trị cấp cao)
        /// </summary>
        /// <param name="dto">Thông tin loại nghỉ mới</param>
        /// <returns>Loại nghỉ vừa tạo</returns>
        [HttpPost("types")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")] // Chỉ Super Admin, Admin, HR Manager mới tạo loại nghỉ
        public async Task<IActionResult> CreateLeaveType([FromBody] CreateLeaveTypeDto dto)
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

            var leaveType = new LeaveType
            {
                LeaveTypeCode = dto.LeaveTypeCode,
                LeaveTypeName = dto.LeaveTypeName,
                IsPaid = dto.IsPaid,
                Description = dto.Description,
                IsActive = dto.IsActive
            };

            var result = await _service.CreateLeaveTypeAsync(leaveType);

            return Ok(new ApiResponse<LeaveType>
            {
                Success = true,
                Message = "Tạo loại nghỉ thành công",
                Data = result,
                StatusCode = 200
            });
        }

        // ================= ĐƠN NGHỈ =================

        /// <summary>
        /// Tạo đơn xin nghỉ mới
        /// </summary>
        /// <param name="dto">Thông tin đơn nghỉ</param>
        /// <returns>Đơn nghỉ vừa tạo</returns>
        [HttpPost("request")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE,INTERN,PROBATION_STAFF")]
        public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestDto dto)
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

            // Kiểm tra người dùng chỉ được tạo đơn cho chính mình (trừ các role quản lý)
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Các role được phép tạo đơn cho người khác
            var canCreateForOthers = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!canCreateForOthers.Contains(currentUserRole) && currentUserId != dto.EmployeeID)
            {
                return Forbid();
            }

            var request = new LeaveRequest
            {
                EmployeeID = dto.EmployeeID,
                LeaveTypeID = dto.LeaveTypeID,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                NumberOfDays = dto.NumberOfDays,
                Reason = dto.Reason
            };

            var result = await _service.CreateLeaveRequestAsync(request);

            return Ok(new ApiResponse<LeaveRequest>
            {
                Success = true,
                Message = "Tạo đơn xin nghỉ thành công",
                Data = result,
                StatusCode = 200
            });
        }

        // ================= CÁ NHÂN =================

        /// <summary>
        /// Lấy danh sách đơn nghỉ của nhân viên (đang chờ duyệt và đã duyệt)
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Danh sách đơn nghỉ</returns>
        [HttpGet("my/{employeeId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE,INTERN,PROBATION_STAFF")]
        public async Task<IActionResult> MyLeave(int employeeId)
        {
            // Kiểm tra quyền: chỉ các role quản lý hoặc chính nhân viên đó mới xem được
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            var result = await _service.GetMyLeaveAsync(employeeId);

            return Ok(new ApiResponse<List<LeaveRequest>>
            {
                Success = true,
                Message = "Lấy danh sách đơn nghỉ thành công",
                Data = result,
                StatusCode = 200
            });
        }

        /// <summary>
        /// Lấy lịch sử nghỉ phép của nhân viên
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Lịch sử nghỉ phép</returns>
        [HttpGet("history/{employeeId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE,INTERN,PROBATION_STAFF")]
        public async Task<IActionResult> LeaveHistory(int employeeId)
        {
            // Kiểm tra quyền: chỉ các role quản lý hoặc chính nhân viên đó mới xem được
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            var result = await _service.GetLeaveHistoryAsync(employeeId);

            return Ok(new ApiResponse<List<LeaveRequest>>
            {
                Success = true,
                Message = "Lấy lịch sử nghỉ phép thành công",
                Data = result,
                StatusCode = 200
            });
        }

        // ================= DUYỆT =================

        /// <summary>
        /// Duyệt đơn nghỉ (Chỉ cấp quản lý và HR)
        /// </summary>
        /// <param name="leaveId">ID đơn nghỉ</param>
        /// <param name="approverId">ID người duyệt</param>
        /// <returns>Kết quả duyệt</returns>
        [HttpPost("approve/{leaveId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER")] // Các role có quyền duyệt
        public async Task<IActionResult> ApproveLeave(int leaveId, int approverId)
        {
            // Kiểm tra người duyệt phải là chính người đang thực hiện action
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId != approverId)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "ID người duyệt không khớp với người dùng hiện tại",
                    StatusCode = 400
                });
            }

            var result = await _service.ApproveLeaveAsync(leaveId, approverId);

            if (result)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Duyệt đơn nghỉ thành công",
                    StatusCode = 200
                });
            }

            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Không tìm thấy đơn nghỉ hoặc không thể duyệt",
                StatusCode = 404
            });
        }

        /// <summary>
        /// Từ chối đơn nghỉ (Chỉ cấp quản lý và HR)
        /// </summary>
        /// <param name="leaveId">ID đơn nghỉ</param>
        /// <param name="approverId">ID người duyệt</param>
        /// <returns>Kết quả từ chối</returns>
        [HttpPost("reject/{leaveId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER")] // Các role có quyền từ chối
        public async Task<IActionResult> RejectLeave(int leaveId, int approverId)
        {
            // Kiểm tra người duyệt phải là chính người đang thực hiện action
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId != approverId)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "ID người duyệt không khớp với người dùng hiện tại",
                    StatusCode = 400
                });
            }

            var result = await _service.RejectLeaveAsync(leaveId, approverId);

            if (result)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Từ chối đơn nghỉ thành công",
                    StatusCode = 200
                });
            }

            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Không tìm thấy đơn nghỉ hoặc không thể từ chối",
                StatusCode = 404
            });
        }

        // ================= SỐ DƯ =================

        /// <summary>
        /// Lấy số ngày nghỉ còn lại của nhân viên theo loại nghỉ
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <param name="leaveTypeId">ID loại nghỉ</param>
        /// <returns>Số ngày nghỉ còn lại</returns>
        [HttpGet("balance")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE,INTERN,PROBATION_STAFF")]
        public async Task<IActionResult> LeaveBalance(int employeeId, int leaveTypeId)
        {
            // Kiểm tra quyền: chỉ các role quản lý hoặc chính nhân viên đó mới xem được
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            var result = await _service.GetLeaveBalanceAsync(employeeId, leaveTypeId);

            return Ok(new ApiResponse<decimal>
            {
                Success = true,
                Message = "Lấy số dư ngày nghỉ thành công",
                Data = result,
                StatusCode = 200
            });
        }

        // ================= LỊCH NGHỈ =================

        /// <summary>
        /// Lấy lịch nghỉ của toàn công ty trong khoảng thời gian
        /// </summary>
        /// <param name="from">Từ ngày</param>
        /// <param name="to">Đến ngày</param>
        /// <returns>Danh sách các đơn nghỉ</returns>
        [HttpGet("calendar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,DIRECTOR")]
        public async Task<IActionResult> LeaveCalendar([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            if (from > to)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc",
                    StatusCode = 400
                });
            }

            var result = await _service.GetLeaveCalendarAsync(from, to);

            return Ok(new ApiResponse<List<LeaveRequest>>
            {
                Success = true,
                Message = "Lấy lịch nghỉ thành công",
                Data = result,
                StatusCode = 200
            });
        }
    }
}