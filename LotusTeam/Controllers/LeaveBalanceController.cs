using LotusTeam.Models;
using LotusTeam.Services;
using LotusTeam.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    /// <summary>
    /// API quản lý số dư ngày phép của nhân viên
    /// </summary>
    [ApiController]
    [Route("api/leave-balance")]
    [Authorize]
    public class LeaveBalanceController : ControllerBase
    {
        private readonly ILeaveBalanceService _service;
        private readonly ILogger<LeaveBalanceController> _logger;

        public LeaveBalanceController(ILeaveBalanceService service, ILogger<LeaveBalanceController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách số dư ngày phép của nhân viên (tất cả các năm)
        /// </summary>
        [HttpGet("employee/{employeeId}")]
        [ProducesResponseType(typeof(ApiResponse<List<LeaveBalanceDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<LeaveBalanceDto>>>> GetByEmployee(int employeeId)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                // Check permission
                var currentEmployeeId = GetCurrentEmployeeId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "EMPLOYEE";

                var canView = false;

                if (currentEmployeeId == employeeId)
                {
                    canView = true;
                }
                else if (new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "DIRECTOR" }.Contains(userRole))
                {
                    canView = true;
                }
                else if (userRole == "MANAGER" || userRole == "TEAM_LEADER")
                {
                    var isInSameDepartment = await _service.IsEmployeeInSameDepartment(currentEmployeeId, employeeId);
                    if (isInSameDepartment)
                    {
                        canView = true;
                    }
                }

                if (!canView)
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Bạn không có quyền xem thông tin số dư phép của nhân viên này"
                    });
                }

                var result = await _service.GetByEmployeeWithInfoAsync(employeeId);

                if (result == null || !result.Any())
                {
                    return Ok(new ApiResponse<List<LeaveBalanceDto>>
                    {
                        Success = true,
                        Data = new List<LeaveBalanceDto>(),
                        Message = "Nhân viên chưa có dữ liệu số dư phép"
                    });
                }

                return Ok(new ApiResponse<List<LeaveBalanceDto>>
                {
                    Success = true,
                    Data = result.ToList(),
                    Message = "Danh sách số dư ngày phép"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave balance for employee {EmployeeId} | TraceId: {TraceId}",
                    employeeId, traceId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin số dư phép",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin số dư ngày phép của nhân viên theo năm cụ thể
        /// </summary>
        [HttpGet("employee/{employeeId}/year/{year}")]
        [ProducesResponseType(typeof(ApiResponse<LeaveBalanceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<LeaveBalanceDto>>> GetByEmployeeAndYear(int employeeId, int year)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                var currentEmployeeId = GetCurrentEmployeeId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "EMPLOYEE";

                var canView = false;

                if (currentEmployeeId == employeeId)
                {
                    canView = true;
                }
                else if (new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "DIRECTOR" }.Contains(userRole))
                {
                    canView = true;
                }
                else if (userRole == "MANAGER" || userRole == "TEAM_LEADER")
                {
                    var isInSameDepartment = await _service.IsEmployeeInSameDepartment(currentEmployeeId, employeeId);
                    if (isInSameDepartment)
                    {
                        canView = true;
                    }
                }

                if (!canView)
                {
                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Bạn không có quyền xem thông tin số dư phép của nhân viên này"
                    });
                }

                var result = await _service.GetByEmployeeAndYearAsync(employeeId, year);

                if (result == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy thông tin số dư phép cho nhân viên ID {employeeId} năm {year}"
                    });
                }

                return Ok(new ApiResponse<LeaveBalanceDto>
                {
                    Success = true,
                    Data = result,
                    Message = $"Thông tin số dư ngày phép năm {year}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave balance for employee {EmployeeId} year {Year} | TraceId: {TraceId}",
                    employeeId, year, traceId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin số dư phép",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách số dư ngày phép của tất cả nhân viên
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,DIRECTOR")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<LeaveBalanceDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<IEnumerable<LeaveBalanceDto>>>> GetAll(
            [FromQuery] int? departmentId = null,
            [FromQuery] int? year = null)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                var result = await _service.GetAllAsync(year, departmentId);

                return Ok(new ApiResponse<IEnumerable<LeaveBalanceDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Danh sách số dư ngày phép"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all leave balances | TraceId: {TraceId}", traceId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách số dư phép",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Tạo mới hoặc cập nhật số dư ngày phép cho nhân viên
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        [ProducesResponseType(typeof(ApiResponse<LeaveBalanceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<LeaveBalanceDto>>> CreateOrUpdate([FromBody] CreateLeaveBalanceDto dto)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                // Validate
                if (dto.EmployeeID <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mã nhân viên không hợp lệ"
                    });
                }

                if (dto.Year < 2000 || dto.Year > DateTime.Now.Year + 1)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Năm không hợp lệ"
                    });
                }

                if (dto.AnnualQuota < 0 || dto.AnnualQuota > 30)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Số ngày phép năm phải từ 0 đến 30 ngày"
                    });
                }

                if (dto.UsedDays < 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Số ngày đã sử dụng không thể âm"
                    });
                }

                var model = new LeaveBalances
                {
                    EmployeeID = dto.EmployeeID,
                    Year = dto.Year,
                    AnnualQuota = dto.AnnualQuota,
                    UsedDays = dto.UsedDays,
                    UnpaidDays = dto.UnpaidDays,
                    ConsecutiveLeaveDays = dto.ConsecutiveLeaveDays,
                    LastLeaveEndDate = dto.LastLeaveEndDate,
                    IsReset = dto.IsReset,
                    UpdatedDate = DateTime.Now
                };

                var result = await _service.CreateOrUpdateAsync(model);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                return Ok(new ApiResponse<LeaveBalanceDto>
                {
                    Success = true,
                    Data = result.Data,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating leave balance | TraceId: {TraceId}", traceId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo/cập nhật số dư phép",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật số ngày đã sử dụng
        /// </summary>
        [HttpPut("employee/{employeeId}/use-days")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        [ProducesResponseType(typeof(ApiResponse<LeaveBalanceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<LeaveBalanceDto>>> UpdateUsedDays(int employeeId, [FromBody] UpdateUsedDaysDto dto)
        {
            var traceId = HttpContext.TraceIdentifier;

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

                var result = await _service.UpdateUsedDaysAsync(employeeId, dto.Year, dto.DaysToAdd);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                return Ok(new ApiResponse<LeaveBalanceDto>
                {
                    Success = true,
                    Data = result.Data,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating used days | TraceId: {TraceId}", traceId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật số ngày đã sử dụng",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Reset số dư ngày phép cho năm mới
        /// </summary>
        [HttpPost("reset")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<object>>> ResetLeaveBalance([FromBody] ResetLeaveBalanceDto dto)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                var result = await _service.ResetLeaveBalanceAsync(dto.EmployeeId, dto.NewYear, dto.CarryOverDays);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting leave balance | TraceId: {TraceId}", traceId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi reset số dư phép",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Xóa số dư ngày phép
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                var result = await _service.DeleteAsync(id);

                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy bản ghi số dư phép với ID {id}"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa số dư ngày phép thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting leave balance | Id: {Id} | TraceId: {TraceId}", id, traceId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa số dư phép",
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
                return userId;
            }
            return null;
        }

        #endregion
    }
}