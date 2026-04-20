using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using LotusTeam.Service;
using LotusTeam.Models;
using LotusTeam.DTOs;
using Microsoft.AspNetCore.Mvc;
using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using LotusTeam.Services;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _service;
        private readonly IRemoteAttendanceService _remoteService;

        public AttendanceController(
            IAttendanceService service,
            IRemoteAttendanceService remoteService)
        {
            _service = service;
            _remoteService = remoteService;
        }

        // ================= CÁ NHÂN =================
        [HttpGet("my/{employeeId:int}")]
        public async Task<IActionResult> MyAttendance(int employeeId)
        {
            var data = await _service.GetMyAttendanceAsync(employeeId);
            return Ok(new { success = true, data });
        }

        // ================= PHÒNG BAN =================
        [HttpGet("department/{departmentId:int}")]
        public async Task<IActionResult> DepartmentAttendance(
            int departmentId,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            var data = await _service.GetDepartmentAttendanceAsync(departmentId, from, to);
            return Ok(new { success = true, data });
        }

        // ================= CHẤM CÔNG THỦ CÔNG =================
        [HttpPost("manual")]
        public async Task<IActionResult> ManualCheck([FromBody] ManualAttendanceDto dto)
        {
            try
            {
                var attendance = new Attendances
                {
                    EmployeeID = dto.EmployeeID,
                    WorkDate = dto.WorkDate,
                    CheckIn = dto.CheckIn,
                    CheckOut = dto.CheckOut,
                    Notes = dto.Notes
                };

                var result = await _service.ManualCheckAsync(attendance);

                return Ok(new { success = true, data = result, message = "Chấm công thủ công thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ================= XÁC NHẬN CHẤM CÔNG =================
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmAttendance(
            [FromBody] List<long> ids,
            [FromServices] AppDbContext context)
        {
            var records = await context.Attendances
                .Where(x => ids.Contains(x.AttendanceID))
                .ToListAsync();

            if (!records.Any())
                return NotFound(new { success = false, message = "Không có dữ liệu" });

            foreach (var item in records)
            {
                item.IsConfirmed = true;
                item.ConfirmedAt = DateTime.Now;
                item.ConfirmedBy = 1; // TODO: lấy từ token
            }

            await context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xác nhận chấm công" });
        }

        // ================= DANH SÁCH ĐÃ XÁC NHẬN =================
        [HttpGet("confirmed")]
        public async Task<IActionResult> GetConfirmed(
            [FromServices] AppDbContext context)
        {
            var data = await context.Attendances
                .Include(x => x.Employee)
                .Include(x => x.AttendanceOvertimes)
                .Where(x => x.IsConfirmed)
                .OrderByDescending(x => x.WorkDate)
                .Select(x => new
                {
                    x.AttendanceID,
                    x.WorkDate,
                    x.CheckIn,
                    x.CheckOut,
                    x.WorkingHours,
                    EmployeeName = x.Employee.FullName,
                    TotalOT = x.AttendanceOvertimes.Sum(o => o.OvertimeHours)
                })
                .ToListAsync();

            return Ok(new { success = true, data });
        }

        // ================= EXPORT =================
        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromServices] AppDbContext context)
        {
            var data = await context.Attendances
                .Include(x => x.Employee)
                .Include(x => x.WorkType)
                .Include(x => x.AttendanceOvertimes)
                .Where(x => x.IsConfirmed)
                .OrderBy(x => x.WorkDate)
                .Select(x => new
                {
                    x.WorkDate,
                    EmployeeName = x.Employee.FullName,
                    CheckIn = x.CheckIn.HasValue ? x.CheckIn.Value.ToString(@"hh\:mm") : "",
                    CheckOut = x.CheckOut.HasValue ? x.CheckOut.Value.ToString(@"hh\:mm") : "",
                    WorkingHours = x.WorkingHours,
                    WorkType = x.WorkType != null ? x.WorkType.WorkTypeName : "Bình thường",
                    TotalOT = x.AttendanceOvertimes.Select(o => (decimal?)o.OvertimeHours).Sum() ?? 0,
                    Status = x.Status != null ? x.Status.StatusName : "Chưa xác định",
                    Notes = x.Notes
                })
                .ToListAsync();

            return Ok(new { success = true, data });
        }

        // ================= LOG THÔ =================
        [HttpGet("raw/{employeeId:int}")]
        public async Task<IActionResult> RawLog(int employeeId)
        {
            var data = await _service.GetRawAttendanceLogAsync(employeeId);
            return Ok(new { success = true, data });
        }

        // ================= ĐIỀU CHỈNH =================
        [HttpPut("adjust/{attendanceId:long}")]
        public async Task<IActionResult> Adjust(
            long attendanceId,
            [FromBody] AdjustAttendanceDto dto)
        {
            try
            {
                var ok = await _service.AdjustAttendanceAsync(
                    attendanceId,
                    dto.CheckIn,
                    dto.CheckOut,
                    dto.Reason);

                if (!ok)
                    return NotFound(new { success = false, message = "Không tìm thấy bản ghi" });

                return Ok(new { success = true, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ================= DANH SÁCH TĂNG CA =================
        [HttpGet("overtime")]
        public async Task<IActionResult> OvertimeList()
        {
            var data = await _service.GetOvertimeListAsync();
            return Ok(new { success = true, data });
        }

        // ================= ĐĂNG KÝ TĂNG CA =================
        [HttpPost("overtime")]
        public async Task<IActionResult> RegisterOvertime([FromBody] OvertimeRequestDto dto)
        {
            try
            {
                var overtime = new AttendanceOvertime
                {
                    AttendanceID = dto.AttendanceID,
                    RuleID = dto.RuleID,
                    OvertimeHours = dto.OvertimeHours
                };

                var result = await _service.RegisterOvertimeAsync(overtime);

                return Ok(new { success = true, data = result, message = "Đăng ký tăng ca thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ================= DUYỆT TĂNG CA =================
        [HttpPost("overtime/approve/{id:long}")]
        public async Task<IActionResult> ApproveOvertime(long id)
        {
            var ok = await _service.ApproveOvertimeAsync(id);

            if (!ok)
                return NotFound(new { success = false, message = "Không tìm thấy hoặc rule lỗi" });

            return Ok(new { success = true, message = "Duyệt thành công" });
        }

        // ================= THỐNG KÊ NHANH =================
        [HttpGet("stats/today")]
        public async Task<IActionResult> TodayStats([FromServices] AppDbContext context)
        {
            var today = DateTime.Today;

            var stats = await context.Attendances
                .Where(x => x.WorkDate == today)
                .GroupBy(x => 1)
                .Select(g => new
                {
                    TotalCheckedIn = g.Count(x => x.CheckIn != null),
                    TotalCheckedOut = g.Count(x => x.CheckOut != null),
                    OnTime = g.Count(x => x.Status != null && x.Status.StatusCode == "PRESENT"),
                    Late = g.Count(x => x.Status != null && x.Status.StatusCode == "LATE"),
                    EarlyLeave = g.Count(x => x.Status != null && x.Status.StatusCode == "EARLY_LEAVE"),
                    Absent = g.Count(x => x.Status != null && x.Status.StatusCode == "ABSENT")
                })
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                stats = new
                {
                    TotalCheckedIn = 0,
                    TotalCheckedOut = 0,
                    OnTime = 0,
                    Late = 0,
                    EarlyLeave = 0,
                    Absent = 0
                };
            }

            return Ok(new { success = true, data = stats });
        }

        // ================= CHẤM CÔNG KHUÔN MẶT =================

        /// <summary>
        /// Check-in bằng khuôn mặt
        /// </summary>
        [HttpPost("face-checkin")]
        public async Task<IActionResult> FaceCheckIn([FromBody] FaceCheckDto dto)
        {
            try
            {
                // Kiểm tra quyền chấm công từ xa
                var canScan = await _remoteService.CanFaceScanAsync(dto.EmployeeId);

                if (!canScan)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bạn không được phép chấm công từ xa (chưa được duyệt)"
                    });
                }

                var result = await _service.FaceCheckInAsync(dto.EmployeeId, dto.ImageBase64);

                if (!result.Success)
                {
                    return BadRequest(new { success = false, message = result.Message, confidence = result.Confidence });
                }

                return Ok(new { success = true, message = result.Message, confidence = result.Confidence });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Check-out bằng khuôn mặt
        /// </summary>
        [HttpPost("face-checkout")]
        public async Task<IActionResult> FaceCheckOut([FromBody] FaceCheckDto dto)
        {
            try
            {
                // Kiểm tra quyền chấm công từ xa
                var canScan = await _remoteService.CanFaceScanAsync(dto.EmployeeId);

                if (!canScan)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bạn không được phép chấm công từ xa (chưa được duyệt)"
                    });
                }

                var result = await _service.FaceCheckOutAsync(dto.EmployeeId, dto.ImageBase64);

                if (!result.Success)
                {
                    return BadRequest(new { success = false, message = result.Message, confidence = result.Confidence });
                }

                return Ok(new { success = true, message = result.Message, confidence = result.Confidence });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy lịch sử chấm công khuôn mặt
        /// </summary>
        [HttpGet("face-history/{employeeId}")]
        public async Task<IActionResult> GetFaceHistory(int employeeId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var data = await _service.GetFaceAttendanceHistoryAsync(employeeId, fromDate, toDate);
                return Ok(new { success = true, data, count = data.Count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Trạng thái chấm công khuôn mặt hôm nay
        /// </summary>
        [HttpGet("face-today/{employeeId}")]
        public async Task<IActionResult> GetFaceTodayStatus(int employeeId)
        {
            try
            {
                var data = await _service.GetTodayFaceAttendanceStatusAsync(employeeId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ================= CHẤM CÔNG TỪ XA (REMOTE) =================

        /// <summary>
        /// Gửi yêu cầu chấm công từ xa
        /// </summary>
        [HttpPost("remote/request")]
        public async Task<IActionResult> RequestRemote([FromBody] RemoteAttendanceRequestDto dto)
        {
            var ok = await _remoteService.RequestAsync(dto);

            if (!ok)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Bạn đã gửi yêu cầu cho ngày này rồi"
                });
            }

            return Ok(new { success = true, message = "Gửi yêu cầu thành công" });
        }

        /// <summary>
        /// Duyệt yêu cầu chấm công từ xa
        /// </summary>
        [HttpPut("remote/approve/{id}")]
        public async Task<IActionResult> ApproveRemote(int id)
        {
            var ok = await _remoteService.ApproveAsync(id, 1); // TODO: lấy từ token

            if (!ok)
                return NotFound(new { success = false, message = "Không tìm thấy yêu cầu" });

            return Ok(new { success = true, message = "Đã duyệt yêu cầu chấm công từ xa" });
        }

        /// <summary>
        /// Từ chối yêu cầu chấm công từ xa
        /// </summary>
        [HttpPut("remote/reject/{id}")]
        public async Task<IActionResult> RejectRemote(int id)
        {
            var ok = await _remoteService.RejectAsync(id);

            if (!ok)
                return NotFound(new { success = false, message = "Không tìm thấy yêu cầu" });

            return Ok(new { success = true, message = "Đã từ chối yêu cầu chấm công từ xa" });
        }

        /// <summary>
        /// Lấy danh sách yêu cầu chấm công từ xa
        /// </summary>
        [HttpGet("remote/list")]
        public async Task<IActionResult> GetRemoteRequests([FromServices] AppDbContext context)
        {
            var data = await context.RemoteAttendances
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new
                {
                    x.Id,
                    x.EmployeeId,
                    x.WorkDate,
                    x.Reason,
                    x.Status,
                    x.ApprovedBy,
                    x.ApprovedDate,
                    x.CreatedDate
                })
                .ToListAsync();

            return Ok(new { success = true, data });
        }

        /// <summary>
        /// Lấy danh sách yêu cầu chấm công từ xa theo nhân viên
        /// </summary>
        [HttpGet("remote/my/{employeeId}")]
        public async Task<IActionResult> GetMyRemoteRequests(int employeeId, [FromServices] AppDbContext context)
        {
            var data = await context.RemoteAttendances
                .Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new
                {
                    x.Id,
                    x.WorkDate,
                    x.Reason,
                    x.Status,
                    x.ApprovedDate,
                    x.CreatedDate
                })
                .ToListAsync();

            return Ok(new { success = true, data });
        }
    }
}