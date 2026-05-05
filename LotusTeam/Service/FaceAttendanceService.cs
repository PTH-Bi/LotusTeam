using LotusTeam.Data;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace LotusTeam.Service
{
    public class FaceAttendanceService : IFaceAttendanceService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FaceAttendanceService> _logger;
        private readonly IFaceRecognitionProvider _faceProvider;


        public FaceAttendanceService(AppDbContext context, ILogger<FaceAttendanceService> logger, IFaceRecognitionProvider faceProvider)
        {
            _context = context;
            _logger = logger;
            _faceProvider = faceProvider;           

        }

        // =============================
        // CHECK IN
        // =============================
        public async Task<FaceCheckResult> CheckIn(int employeeId, string imageBase64)
        {
            var result = new FaceCheckResult();

            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(x => x.EmployeeID == employeeId);

                if (employee == null)
                {
                    result.Success = false;
                    result.Message = "Nhân viên không tồn tại";
                    return result;
                }

                // Mock face recognition (replace with actual AI service later)
                var registered = await _context.FaceAttendances
                    .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Method == "REGISTER");

                if (registered?.CapturedImage == null)
                {
                    result.Success = false;
                    result.Message = "Nhân viên chưa đăng ký khuôn mặt";
                    return result;
                }

                var matchResult = await _faceProvider.MatchFace(imageBase64, registered.CapturedImage);

                if (!matchResult.IsMatch || matchResult.Confidence < 0.7)
                {
                    result.Success = false;
                    result.Message = matchResult.ErrorMessage ?? $"Khuôn mặt không khớp ({matchResult.Confidence:P0})";
                    result.Confidence = matchResult.Confidence;
                    return result;
                }

                double confidence = matchResult.Confidence;

                if (confidence < 0.7)
                {
                    result.Success = false;
                    result.Message = $"Khuôn mặt không khớp (độ tin cậy: {confidence:P0})";
                    result.Confidence = confidence;
                    return result;
                }

                var today = DateTime.Today;

                var existing = await _context.FaceAttendances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employeeId &&
                        x.CheckInTime.HasValue &&
                        x.CheckInTime.Value.Date == today);

                if (existing != null)
                {
                    result.Success = false;
                    result.Message = "Đã check-in hôm nay";
                    result.Confidence = confidence;
                    return result;
                }

                var attendance = new FaceAttendances
                {
                    EmployeeId = employeeId,
                    CheckInTime = DateTime.Now,
                    CapturedImage = imageBase64.Length > 100 ? imageBase64.Substring(0, 100) + "..." : imageBase64,
                    Confidence = confidence,
                    Method = "FACE",
                    CreatedDate = DateTime.Now
                };

                _context.FaceAttendances.Add(attendance);
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = "Check-in thành công";
                result.Confidence = confidence;
                result.CheckTime = DateTime.Now;
                result.AttendanceId = attendance.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CheckIn for employee {EmployeeId}", employeeId);
                result.Success = false;
                result.Message = "Lỗi hệ thống khi xử lý check-in";
            }

            return result;
        }

        // =============================
        // CHECK OUT
        // =============================
        public async Task<FaceCheckResult> CheckOut(int employeeId, string imageBase64)
        {
            var result = new FaceCheckResult();

            try
            {
                var today = DateTime.Today;

                var attendance = await _context.FaceAttendances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employeeId &&
                        x.CheckInTime.HasValue &&
                        x.CheckInTime.Value.Date == today);

                if (attendance == null)
                {
                    result.Success = false;
                    result.Message = "Chưa check-in hôm nay";
                    return result;
                }

                if (attendance.CheckOutTime != null)
                {
                    result.Success = false;
                    result.Message = "Đã check-out rồi";
                    return result;
                }

                // Mock face recognition
                var registered = await _context.FaceAttendances
                    .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Method == "REGISTER");

                var matchResult = await _faceProvider.MatchFace(imageBase64, registered?.CapturedImage ?? "");

                if (!matchResult.IsMatch || matchResult.Confidence < 0.7)
                {
                    result.Success = false;
                    result.Message = matchResult.ErrorMessage ?? $"Khuôn mặt không khớp ({matchResult.Confidence:P0})";
                    result.Confidence = matchResult.Confidence;
                    return result;
                }

                double confidence = matchResult.Confidence;

                if (confidence < 0.7)
                {
                    result.Success = false;
                    result.Message = $"Khuôn mặt không khớp (độ tin cậy: {confidence:P0})";
                    result.Confidence = confidence;
                    return result;
                }

                attendance.CheckOutTime = DateTime.Now;
                attendance.Confidence = confidence;

                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = "Check-out thành công";
                result.Confidence = confidence;
                result.CheckTime = DateTime.Now;
                result.AttendanceId = attendance.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CheckOut for employee {EmployeeId}", employeeId);
                result.Success = false;
                result.Message = "Lỗi hệ thống khi xử lý check-out";
            }

            return result;
        }

        // =============================
        // GET HISTORY
        // =============================
        public async Task<IEnumerable<FaceAttendanceHistoryDto>> GetHistory(int employeeId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.FaceAttendances
                    .Include(x => x.Employee)
                        .ThenInclude(e => e.Department)
                    .Where(x => x.EmployeeId == employeeId);

                if (fromDate.HasValue)
                    query = query.Where(x => x.CreatedDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(x => x.CreatedDate <= toDate.Value);

                var history = await query
                    .OrderByDescending(x => x.CreatedDate)
                    .Select(x => new FaceAttendanceHistoryDto
                    {
                        Id = x.Id,
                        EmployeeId = x.EmployeeId,
                        EmployeeName = x.Employee != null ? x.Employee.FullName : "",
                        EmployeeCode = x.Employee != null ? x.Employee.EmployeeCode : "",
                        DepartmentName = x.Employee != null && x.Employee.Department != null ? x.Employee.Department.DepartmentName : "",
                        Date = x.CreatedDate.Date,
                        CheckInTime = x.CheckInTime,
                        CheckOutTime = x.CheckOutTime,
                        WorkingHours = x.CheckInTime.HasValue && x.CheckOutTime.HasValue
                            ? (x.CheckOutTime.Value - x.CheckInTime.Value).TotalHours
                            : null,
                        CheckInConfidence = x.Confidence,
                        CheckOutConfidence = x.Confidence,
                        LateMinutes = x.CheckInTime.HasValue && x.CheckInTime.Value.TimeOfDay > new TimeSpan(8, 30, 0)
                            ? (int)(x.CheckInTime.Value.TimeOfDay - new TimeSpan(8, 30, 0)).TotalMinutes
                            : 0,
                        Status = GetAttendanceStatus(x)
                    })
                    .ToListAsync();

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history for employee {EmployeeId}", employeeId);
                return new List<FaceAttendanceHistoryDto>();
            }
        }

        // =============================
        // REGISTER FACE (Mock - lưu vào FaceAttendances như một bản ghi đặc biệt)
        // =============================
        public async Task<FaceRegisterResult> RegisterFace(int employeeId, string imageBase64)
        {
            var result = new FaceRegisterResult();

            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(x => x.EmployeeID == employeeId);

                if (employee == null)
                {
                    result.Success = false;
                    result.Message = "Nhân viên không tồn tại";
                    return result;
                }

                // Validate image quality (mock)
                var imageQuality = await _faceProvider.ValidateImageQuality(imageBase64);
                if (imageQuality < 0.5)
                {
                    result.Success = false;
                    result.Message = "Ảnh khuôn mặt không đạt chất lượng. Vui lòng chụp lại với ánh sáng tốt hơn, mặt nhìn thẳng.";
                    result.ImageQuality = imageQuality;
                    return result;
                }

                // Check if already registered (has any attendance record with REGISTER method)
                var existing = await _context.FaceAttendances
                    .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Method == "REGISTER");

                if (existing != null)
                {
                    // Update existing registration
                    existing.CapturedImage = imageBase64.Length > 100 ? imageBase64.Substring(0, 100) + "..." : imageBase64;
                    existing.CreatedDate = DateTime.Now;
                    existing.Confidence = imageQuality;
                }
                else
                {
                    // Create new registration record
                    var registration = new FaceAttendances
                    {
                        EmployeeId = employeeId,
                        CheckInTime = null,
                        CheckOutTime = null,
                        CapturedImage = imageBase64.Length > 100 ? imageBase64.Substring(0, 100) + "..." : imageBase64,
                        Confidence = imageQuality,
                        Method = "REGISTER",
                        CreatedDate = DateTime.Now
                    };
                    _context.FaceAttendances.Add(registration);
                }

                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = "Đăng ký khuôn mặt thành công";
                result.ImageQuality = imageQuality;
                result.FaceDataId = existing?.Id ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering face for employee {EmployeeId}", employeeId);
                result.Success = false;
                result.Message = "Lỗi hệ thống khi đăng ký khuôn mặt";
            }

            return result;
        }

        // =============================
        // GET TODAY ATTENDANCE
        // =============================
        public async Task<TodayAttendanceDto> GetTodayAttendance(int employeeId)
        {
            try
            {
                var today = DateTime.Today;
                var attendance = await _context.FaceAttendances
                    .FirstOrDefaultAsync(x => x.EmployeeId == employeeId &&
                        x.CreatedDate.Date == today &&
                        x.Method == "FACE");

                var result = new TodayAttendanceDto
                {
                    HasCheckedIn = attendance?.CheckInTime != null,
                    CheckInTime = attendance?.CheckInTime,
                    CheckInConfidence = attendance?.Confidence,
                    HasCheckedOut = attendance?.CheckOutTime != null,
                    CheckOutTime = attendance?.CheckOutTime,
                    CheckOutConfidence = attendance?.Confidence,
                    CanCheckOut = attendance?.CheckInTime != null && attendance?.CheckOutTime == null
                };

                if (result.HasCheckedIn && result.HasCheckedOut)
                {
                    result.WorkingHours = (result.CheckOutTime.Value - result.CheckInTime.Value).TotalHours;
                }

                if (result.HasCheckedIn && result.CheckInTime.Value.TimeOfDay > new TimeSpan(8, 30, 0))
                {
                    result.LateMinutes = (int)(result.CheckInTime.Value.TimeOfDay - new TimeSpan(8, 30, 0)).TotalMinutes;
                    result.Status = $"Đi muộn {result.LateMinutes} phút";
                }
                else if (result.HasCheckedIn)
                {
                    result.Status = "Đúng giờ";
                }
                else
                {
                    result.Status = "Chưa check-in";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today attendance for employee {EmployeeId}", employeeId);
                return new TodayAttendanceDto
                {
                    HasCheckedIn = false,
                    HasCheckedOut = false,
                    Status = "Lỗi hệ thống"
                };
            }
        }

        // =============================
        // CHECK EMPLOYEE IN SAME DEPARTMENT
        // =============================
        public async Task<bool> IsEmployeeInSameDepartment(int? managerEmployeeId, int targetEmployeeId)
        {
            try
            {
                if (!managerEmployeeId.HasValue)
                    return false;

                var manager = await _context.Employees
                    .FirstOrDefaultAsync(x => x.EmployeeID == managerEmployeeId.Value);

                var target = await _context.Employees
                    .FirstOrDefaultAsync(x => x.EmployeeID == targetEmployeeId);

                if (manager == null || target == null)
                    return false;

                return manager.DepartmentID == target.DepartmentID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking department for manager {ManagerId} and employee {TargetId}",
                    managerEmployeeId, targetEmployeeId);
                return false;
            }
        }

        // =============================
        // DELETE FACE DATA
        // =============================
        public async Task<FaceDeleteResult> DeleteFaceData(int employeeId)
        {
            var result = new FaceDeleteResult();

            try
            {
                var faceRecords = await _context.FaceAttendances
                    .Where(x => x.EmployeeId == employeeId && x.Method == "REGISTER")
                    .ToListAsync();

                if (!faceRecords.Any())
                {
                    result.Success = true;
                    result.Message = "Không có dữ liệu khuôn mặt để xóa";
                    result.DeletedCount = 0;
                    return result;
                }

                _context.FaceAttendances.RemoveRange(faceRecords);
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = "Xóa dữ liệu khuôn mặt thành công";
                result.DeletedCount = faceRecords.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting face data for employee {EmployeeId}", employeeId);
                result.Success = false;
                result.Message = "Lỗi hệ thống khi xóa dữ liệu khuôn mặt";
            }

            return result;
        }

        // =============================
        // UPDATE FACE DATA
        // =============================
        public async Task<FaceUpdateResult> UpdateFaceData(int employeeId, string imageBase64)
        {
            var result = new FaceUpdateResult();

            try
            {
                // First delete old data, then register new
                await DeleteFaceData(employeeId);
                var registerResult = await RegisterFace(employeeId, imageBase64);

                result.Success = registerResult.Success;
                result.Message = registerResult.Message;
                result.UpdatedAt = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating face data for employee {EmployeeId}", employeeId);
                result.Success = false;
                result.Message = "Lỗi hệ thống khi cập nhật khuôn mặt";
            }

            return result;
        }

        // =============================
        // GET ATTENDANCE STATISTICS
        // =============================
        public async Task<FaceAttendanceStatisticsDto> GetAttendanceStatistics(int employeeId, int month, int year)
        {
            try
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var attendances = await _context.FaceAttendances
                    .Where(x => x.EmployeeId == employeeId &&
                               x.Method == "FACE" &&
                               x.CreatedDate >= startDate &&
                               x.CreatedDate <= endDate)
                    .ToListAsync();

                var totalWorkingDays = DateTime.DaysInMonth(year, month);
                var daysPresent = attendances.Count(x => x.CheckInTime.HasValue);
                var daysLate = attendances.Count(x => x.CheckInTime.HasValue &&
                    x.CheckInTime.Value.TimeOfDay > new TimeSpan(8, 30, 0));
                var daysEarlyLeave = attendances.Count(x => x.CheckOutTime.HasValue &&
                    x.CheckOutTime.Value.TimeOfDay < new TimeSpan(17, 0, 0));

                var totalWorkingHours = attendances
                    .Where(x => x.CheckInTime.HasValue && x.CheckOutTime.HasValue)
                    .Sum(x => (x.CheckOutTime.Value - x.CheckInTime.Value).TotalHours);

                var statistics = new FaceAttendanceStatisticsDto
                {
                    Month = month,
                    Year = year,
                    TotalWorkingDays = totalWorkingDays,
                    DaysPresent = daysPresent,
                    DaysAbsent = totalWorkingDays - daysPresent,
                    DaysLate = daysLate,
                    DaysEarlyLeave = daysEarlyLeave,
                    TotalWorkingHours = totalWorkingHours,
                    OvertimeHours = totalWorkingHours > totalWorkingDays * 8 ? totalWorkingHours - totalWorkingDays * 8 : 0,
                    AttendanceRate = totalWorkingDays > 0 ? (double)daysPresent / totalWorkingDays * 100 : 0,
                    DailyDetails = new List<DailyAttendanceStatDto>()
                };

                // Daily details
                for (int day = 1; day <= totalWorkingDays; day++)
                {
                    var date = new DateTime(year, month, day);
                    var attendance = attendances.FirstOrDefault(x => x.CreatedDate.Date == date);
                    var dayOfWeek = date.DayOfWeek.ToString();

                    statistics.DailyDetails.Add(new DailyAttendanceStatDto
                    {
                        Date = date,
                        DayOfWeek = dayOfWeek,
                        CheckInTime = attendance?.CheckInTime,
                        CheckOutTime = attendance?.CheckOutTime,
                        WorkingHours = attendance?.CheckInTime != null && attendance?.CheckOutTime != null
                            ? (attendance.CheckOutTime.Value - attendance.CheckInTime.Value).TotalHours
                            : null,
                        Status = attendance?.CheckInTime != null ? "Đã đi làm" : "Vắng",
                        StatusColor = attendance?.CheckInTime != null ? "green" : "red"
                    });
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance statistics for employee {EmployeeId}", employeeId);
                return new FaceAttendanceStatisticsDto();
            }
        }

        // =============================
        // GET FACE REGISTRATION STATUS
        // =============================
        public async Task<FaceRegistrationStatusDto> GetFaceRegistrationStatus(int employeeId)
        {
            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(x => x.EmployeeID == employeeId);

                var faceData = await _context.FaceAttendances
                    .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Method == "REGISTER");

                return new FaceRegistrationStatusDto
                {
                    IsRegistered = faceData != null,
                    EmployeeId = employeeId,
                    EmployeeName = employee?.FullName ?? "",
                    RegisteredAt = faceData?.CreatedDate,
                    ImageCount = faceData != null ? 1 : 0,
                    AverageImageQuality = faceData?.Confidence ?? 0,
                    LastUpdatedAt = faceData?.CreatedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting face registration status for employee {EmployeeId}", employeeId);
                return new FaceRegistrationStatusDto
                {
                    IsRegistered = false,
                    EmployeeId = employeeId
                };
            }
        }

        // =============================
        // GET UNREGISTERED EMPLOYEES
        // =============================
        public async Task<IEnumerable<UnregisteredFaceEmployeeDto>> GetUnregisteredEmployees(int? departmentId = null)
        {
            try
            {
                var registeredEmployeeIds = await _context.FaceAttendances
                    .Where(x => x.Method == "REGISTER")
                    .Select(x => x.EmployeeId)
                    .Distinct()
                    .ToListAsync();

                var query = _context.Employees
                    .Include(e => e.Department)
                    .Where(e => e.Status == 1 && !registeredEmployeeIds.Contains(e.EmployeeID));

                if (departmentId.HasValue)
                    query = query.Where(e => e.DepartmentID == departmentId);

                var employees = await query
                    .Select(e => new UnregisteredFaceEmployeeDto
                    {
                        EmployeeId = e.EmployeeID,
                        EmployeeCode = e.EmployeeCode,
                        FullName = e.FullName,
                        DepartmentName = e.Department != null ? e.Department.DepartmentName : "",
                        Email = e.Email,
                        HireDate = e.HireDate
                    })
                    .ToListAsync();

                return employees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unregistered employees");
                return new List<UnregisteredFaceEmployeeDto>();
            }
        }

        // =============================
        // COMPARE FACES
        // =============================
        public async Task<double> CompareFaces(string imageBase64_1, string imageBase64_2)
        {
            // This is a mock implementation
            // In production, integrate with actual face recognition API
            await Task.Delay(10); // Simulate async work
            return FakeFaceMatch(imageBase64_2);
        }

        // =============================
        // HELPER METHODS
        // =============================
        private string GetAttendanceStatus(FaceAttendances attendance)
        {
            if (attendance.CheckInTime == null)
                return "Chưa check-in";

            if (attendance.CheckInTime.Value.TimeOfDay > new TimeSpan(8, 30, 0))
            {
                var lateMinutes = (int)(attendance.CheckInTime.Value.TimeOfDay - new TimeSpan(8, 30, 0)).TotalMinutes;
                return $"Đi muộn {lateMinutes} phút";
            }

            if (attendance.CheckOutTime == null)
                return "Đã check-in, chưa check-out";

            return "Hoàn thành";
        }

        private async Task<double> ValidateImageQuality(string imageBase64)
        {
            // Mock image quality assessment
            await Task.Delay(10);

            if (string.IsNullOrEmpty(imageBase64))
                return 0;

            var base64Length = imageBase64.Length;
            if (base64Length < 10000) // Too small
                return 0.3;

            if (base64Length > 5000000) // Too large (>5MB)
                return 0.4;

            return new Random().NextDouble() * (0.95 - 0.6) + 0.6;
        }

        private double FakeFaceMatch(string base64)
        {
            // Mock confidence between 0.7 and 0.95
            return new Random().NextDouble() * (0.95 - 0.7) + 0.7;
        }
    }
}