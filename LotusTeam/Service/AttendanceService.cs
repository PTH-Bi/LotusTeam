using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LotusTeam.Service
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AttendanceService> _logger;

        // Giờ hành chính mặc định
        private readonly TimeSpan _officeStartTime = new TimeSpan(8, 0, 0);  // 8:00 AM
        private readonly TimeSpan _officeEndTime = new TimeSpan(17, 0, 0);    // 5:00 PM
        private readonly TimeSpan _lateThreshold = new TimeSpan(0, 15, 0);     // 15 phút
        private readonly TimeSpan _earlyLeaveThreshold = new TimeSpan(0, 30, 0); // 30 phút

        public AttendanceService(
            AppDbContext context,
            ILogger<AttendanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ================= KIỂM TRA GIỜ HÀNH CHÍNH =================
        private (bool isOnTime, string status, int lateMinutes) CheckOfficeHour(TimeSpan checkInTime)
        {
            if (checkInTime <= _officeStartTime.Add(_lateThreshold))
            {
                return (true, "PRESENT", 0);
            }
            else if (checkInTime <= _officeStartTime.Add(new TimeSpan(1, 0, 0)))
            {
                var lateMinutes = (int)(checkInTime - _officeStartTime).TotalMinutes;
                return (false, "LATE", lateMinutes);
            }
            else
            {
                return (false, "ABSENT", 0);
            }
        }

        private (bool isOnTime, string status, int earlyLeaveMinutes) CheckCheckOutTime(TimeSpan checkOutTime)
        {
            if (checkOutTime >= _officeEndTime.Subtract(_earlyLeaveThreshold))
            {
                return (true, "PRESENT", 0);
            }
            else
            {
                var earlyLeaveMinutes = (int)(_officeEndTime - checkOutTime).TotalMinutes;
                return (false, "EARLY_LEAVE", earlyLeaveMinutes);
            }
        }

        private decimal CalculateWorkingHours(TimeSpan checkIn, TimeSpan checkOut)
        {
            var totalHours = (checkOut - checkIn).TotalHours;

            // Trừ giờ nghỉ trưa (1 giờ) nếu làm full ngày
            var lunchStart = new TimeSpan(12, 0, 0);
            var lunchEnd = new TimeSpan(13, 0, 0);

            if (checkIn <= lunchStart && checkOut >= lunchEnd)
            {
                totalHours -= 1;
            }

            return (decimal)Math.Max(totalHours, 0);
        }

        // ================= LẤY STATUS ID TỪ CODE =================
        private async Task<short?> GetStatusIdByCodeAsync(string statusCode)
        {
            var status = await _context.StatusMasters
                .FirstOrDefaultAsync(s => s.StatusCode == statusCode && s.Module == "ATTENDANCE");

            return status?.StatusID;
        }

        // ================= XEM CÁ NHÂN =================
        public async Task<List<Attendances>> GetMyAttendanceAsync(int employeeId)
        {
            return await _context.Attendances
                .Include(a => a.WorkType)
                .Include(a => a.Status)
                .Where(a => a.EmployeeID == employeeId)
                .OrderByDescending(a => a.WorkDate)
                .ToListAsync();
        }

        // ================= PHÒNG BAN =================
        public async Task<List<Attendances>> GetDepartmentAttendanceAsync(
            int departmentId, DateTime from, DateTime to)
        {
            return await _context.Attendances
                .Include(a => a.Employee)
                .Include(a => a.Status)
                .Where(a =>
                    a.Employee.DepartmentID == departmentId &&
                    a.WorkDate >= from &&
                    a.WorkDate <= to)
                .ToListAsync();
        }

        // ================= CHẤM CÔNG THỦ CÔNG =================
        public async Task<Attendances> ManualCheckAsync(Attendances attendance)
        {
            attendance.CreatedDate = DateTime.Now;

            // Set status dựa trên giờ check-in/out
            var statusCode = "PRESENT";

            if (attendance.CheckIn.HasValue)
            {
                var (_, code, lateMinutes) = CheckOfficeHour(attendance.CheckIn.Value);
                statusCode = code;
                attendance.LateMinutes = lateMinutes;
            }

            var statusId = await GetStatusIdByCodeAsync(statusCode);
            if (statusId.HasValue)
            {
                attendance.StatusID = statusId.Value;
            }

            if (attendance.CheckIn != null && attendance.CheckOut != null)
            {
                attendance.WorkingHours = CalculateWorkingHours(attendance.CheckIn.Value, attendance.CheckOut.Value);
            }

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return attendance;
        }

        // ================= LOG =================
        public async Task<List<Attendances>> GetRawAttendanceLogAsync(int employeeId)
        {
            return await _context.Attendances
                .Include(a => a.Status)
                .Where(a => a.EmployeeID == employeeId)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
        }

        // ================= ĐIỀU CHỈNH =================
        public async Task<bool> AdjustAttendanceAsync(
            long attendanceId,
            TimeSpan? checkIn,
            TimeSpan? checkOut,
            string reason)
        {
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(x => x.AttendanceID == attendanceId);

            if (attendance == null)
                return false;

            attendance.CheckIn = checkIn;
            attendance.CheckOut = checkOut;
            attendance.Notes = reason;

            if (checkIn != null && checkOut != null)
            {
                attendance.WorkingHours = CalculateWorkingHours(checkIn.Value, checkOut.Value);

                // Cập nhật status dựa trên giờ mới
                var (_, statusCode, lateMinutes) = CheckOfficeHour(checkIn.Value);
                attendance.LateMinutes = lateMinutes;

                var statusId = await GetStatusIdByCodeAsync(statusCode);
                if (statusId.HasValue)
                {
                    attendance.StatusID = statusId.Value;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ================= TĂNG CA =================
        public async Task<List<AttendanceOvertime>> GetOvertimeListAsync()
        {
            return await _context.AttendanceOvertimes
                .Include(o => o.Attendance)
                .Include(o => o.OvertimeRule)
                .ToListAsync();
        }

        public async Task<AttendanceOvertime> RegisterOvertimeAsync(AttendanceOvertime overtime)
        {
            _context.AttendanceOvertimes.Add(overtime);
            await _context.SaveChangesAsync();
            return overtime;
        }

        public async Task<bool> ApproveOvertimeAsync(long overtimeId)
        {
            var ot = await _context.AttendanceOvertimes
                .Include(x => x.OvertimeRule)
                .FirstOrDefaultAsync(x => x.AttendanceOvertimeID == overtimeId);

            if (ot == null || ot.OvertimeRule == null)
                return false;

            ot.CalculatedAmount = ot.OvertimeHours * ot.OvertimeRule.Rate;

            await _context.SaveChangesAsync();
            return true;
        }

        // ================= CHẤM CÔNG KHUÔN MẶT =================

        /// <summary>
        /// Check-in bằng khuôn mặt
        /// </summary>
        public async Task<(bool Success, string Message, double Confidence)> FaceCheckInAsync(
            int employeeId,
            string imageBase64)
        {
            try
            {
                // 1. Kiểm tra nhân viên tồn tại
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

                if (employee == null)
                {
                    return (false, "Nhân viên không tồn tại", 0);
                }

                // 2. TODO: AI xử lý (tạm mock)
                double confidence = FakeFaceMatch(imageBase64);

                if (confidence < 0.7)
                {
                    return (false, "Khuôn mặt không khớp", confidence);
                }

                // 3. Kiểm tra đã check-in hôm nay chưa
                var today = DateTime.Today;

                var existing = await _context.FaceAttendances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employeeId &&
                        x.CheckInTime.HasValue &&
                        x.CheckInTime.Value.Date == today);

                if (existing != null && existing.CheckOutTime == null)
                {
                    return (false, "Bạn đã check-in hôm nay rồi. Vui lòng check-out!", confidence);
                }

                if (existing != null && existing.CheckOutTime != null)
                {
                    return (false, "Bạn đã hoàn thành chấm công hôm nay", confidence);
                }

                // 4. Lưu FaceAttendance
                var faceAttendance = new FaceAttendances
                {
                    EmployeeId = employeeId,
                    CheckInTime = DateTime.Now,
                    CapturedImage = imageBase64,
                    Confidence = confidence,
                    Method = "FACE",
                    CreatedDate = DateTime.Now
                };

                _context.FaceAttendances.Add(faceAttendance);
                await _context.SaveChangesAsync();

                // 5. Đồng bộ vào bảng Attendances
                var checkInTime = DateTime.Now.TimeOfDay;
                var (isOnTime, statusCode, lateMinutes) = CheckOfficeHour(checkInTime);

                var statusId = await GetStatusIdByCodeAsync(statusCode);

                var attendance = new Attendances
                {
                    EmployeeID = employeeId,
                    WorkDate = today,
                    CheckIn = checkInTime,
                    LateMinutes = lateMinutes,
                    StatusID = statusId,
                    CreatedDate = DateTime.Now,
                    Notes = isOnTime ? null : $"Check-in lúc {checkInTime}, trễ {lateMinutes} phút"
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Face check-in successful for employee {EmployeeId}, confidence: {Confidence}",
                    employeeId, confidence);

                return (true, $"Check-in thành công lúc {DateTime.Now:HH:mm:ss}", confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Face check-in failed for employee {EmployeeId}", employeeId);
                return (false, $"Lỗi hệ thống: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Check-out bằng khuôn mặt
        /// </summary>
        public async Task<(bool Success, string Message, double Confidence)> FaceCheckOutAsync(
            int employeeId,
            string imageBase64)
        {
            try
            {
                // 1. Kiểm tra nhân viên tồn tại
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

                if (employee == null)
                {
                    return (false, "Nhân viên không tồn tại", 0);
                }

                // 2. TODO: AI xử lý (tạm mock)
                double confidence = FakeFaceMatch(imageBase64);

                if (confidence < 0.7)
                {
                    return (false, "Khuôn mặt không khớp", confidence);
                }

                // 3. Tìm bản ghi check-in hôm nay
                var today = DateTime.Today;

                var faceAttendance = await _context.FaceAttendances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employeeId &&
                        x.CheckInTime.HasValue &&
                        x.CheckInTime.Value.Date == today);

                if (faceAttendance == null)
                {
                    return (false, "Chưa check-in hôm nay", confidence);
                }

                if (faceAttendance.CheckOutTime != null)
                {
                    return (false, "Bạn đã check-out rồi", confidence);
                }

                // 4. Cập nhật FaceAttendance
                faceAttendance.CheckOutTime = DateTime.Now;

                await _context.SaveChangesAsync();

                // 5. Đồng bộ vào bảng Attendances
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EmployeeID == employeeId && a.WorkDate == today);

                if (attendance != null)
                {
                    var checkOutTime = DateTime.Now.TimeOfDay;
                    attendance.CheckOut = checkOutTime;
                    attendance.WorkingHours = CalculateWorkingHours(attendance.CheckIn!.Value, checkOutTime);

                    // Kiểm tra về sớm
                    var (isOnTime, statusCode, earlyLeaveMinutes) = CheckCheckOutTime(checkOutTime);
                    attendance.EarlyLeaveMinutes = earlyLeaveMinutes;

                    if (!isOnTime && statusCode == "EARLY_LEAVE")
                    {
                        attendance.Notes = (attendance.Notes ?? "") + $", về sớm {earlyLeaveMinutes} phút";
                    }

                    await _context.SaveChangesAsync();
                }

                // 6. Tính tổng giờ làm việc
                var workingHours = attendance?.WorkingHours ?? 0;

                _logger.LogInformation("Face check-out successful for employee {EmployeeId}, confidence: {Confidence}",
                    employeeId, confidence);

                return (true, $"Check-out thành công lúc {DateTime.Now:HH:mm:ss}. Tổng giờ làm: {workingHours:F2}h", confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Face check-out failed for employee {EmployeeId}", employeeId);
                return (false, $"Lỗi hệ thống: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Lấy lịch sử chấm công khuôn mặt
        /// </summary>
        public async Task<List<FaceAttendanceDto>> GetFaceAttendanceHistoryAsync(
            int employeeId,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var query = _context.FaceAttendances
                    .Where(f => f.EmployeeId == employeeId);

                if (fromDate.HasValue)
                {
                    query = query.Where(f => f.CheckInTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);
                    query = query.Where(f => f.CheckInTime <= endDate);
                }

                var results = await query
                    .OrderByDescending(f => f.CheckInTime)
                    .Select(f => new FaceAttendanceDto
                    {
                        Id = f.Id,
                        EmployeeId = f.EmployeeId,
                        CheckInTime = f.CheckInTime,
                        CheckOutTime = f.CheckOutTime,
                        Confidence = f.Confidence ?? 0,
                        Method = f.Method,
                        CreatedDate = f.CreatedDate
                    })
                    .ToListAsync();

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get face attendance history failed for employee {EmployeeId}", employeeId);
                throw;
            }
        }

        /// <summary>
        /// Lấy trạng thái chấm công hôm nay
        /// </summary>
        public async Task<object> GetTodayFaceAttendanceStatusAsync(int employeeId)
        {
            try
            {
                var today = DateTime.Today;

                var faceAttendance = await _context.FaceAttendances
                    .FirstOrDefaultAsync(f => f.EmployeeId == employeeId && f.CheckInTime!.Value.Date == today);

                var attendance = await _context.Attendances
                    .Include(a => a.Status)
                    .FirstOrDefaultAsync(a => a.EmployeeID == employeeId && a.WorkDate == today);

                return new
                {
                    EmployeeId = employeeId,
                    Date = today,
                    IsCheckedIn = faceAttendance?.CheckInTime != null,
                    CheckInTime = faceAttendance?.CheckInTime,
                    IsCheckedOut = faceAttendance?.CheckOutTime != null,
                    CheckOutTime = faceAttendance?.CheckOutTime,
                    Confidence = faceAttendance?.Confidence ?? 0,
                    WorkingHours = attendance?.WorkingHours ?? 0,
                    LateMinutes = attendance?.LateMinutes ?? 0,
                    EarlyLeaveMinutes = attendance?.EarlyLeaveMinutes ?? 0,
                    Status = attendance?.Status?.StatusCode ?? "PENDING",
                    Method = faceAttendance?.Method ?? "N/A"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get today face attendance status failed for employee {EmployeeId}", employeeId);
                throw;
            }
        }

        /// <summary>
        /// Đồng bộ FaceAttendance vào Attendance (chạy hàng đêm hoặc manual)
        /// </summary>
        public async Task<int> SyncFaceAttendanceToAttendanceAsync(DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;
                var startDate = targetDate.Date;
                var endDate = startDate.AddDays(1);

                var faceAttendances = await _context.FaceAttendances
                    .Where(f => f.CheckInTime >= startDate && f.CheckInTime < endDate)
                    .ToListAsync();

                var syncedCount = 0;

                foreach (var face in faceAttendances)
                {
                    var existing = await _context.Attendances
                        .FirstOrDefaultAsync(a => a.EmployeeID == face.EmployeeId && a.WorkDate == startDate);

                    if (existing == null && face.CheckInTime.HasValue)
                    {
                        var checkInTime = face.CheckInTime.Value.TimeOfDay;
                        var (isOnTime, statusCode, lateMinutes) = CheckOfficeHour(checkInTime);
                        var statusId = await GetStatusIdByCodeAsync(statusCode);

                        var attendance = new Attendances
                        {
                            EmployeeID = face.EmployeeId,
                            WorkDate = startDate,
                            CheckIn = checkInTime,
                            LateMinutes = lateMinutes,
                            StatusID = statusId,
                            CreatedDate = DateTime.Now
                        };

                        if (face.CheckOutTime.HasValue)
                        {
                            var checkOutTime = face.CheckOutTime.Value.TimeOfDay;
                            attendance.CheckOut = checkOutTime;
                            attendance.WorkingHours = CalculateWorkingHours(checkInTime, checkOutTime);

                            var (_, _, earlyLeaveMinutes) = CheckCheckOutTime(checkOutTime);
                            attendance.EarlyLeaveMinutes = earlyLeaveMinutes;
                        }

                        _context.Attendances.Add(attendance);
                        syncedCount++;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Synced {Count} face attendance records for {Date}", syncedCount, startDate);

                return syncedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync face attendance failed");
                throw;
            }
        }

        // ================= MOCK AI (TẠM THỜI) =================
        private double FakeFaceMatch(string base64)
        {
            // TODO: thay bằng AI thật
            // Mock: trả về confidence random từ 0.7 đến 0.95
            return new Random().NextDouble() * (0.95 - 0.7) + 0.7;
        }
    }

    // ================= DTO =================
    public class FaceAttendanceDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public double Confidence { get; set; }
        public string? Method { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}