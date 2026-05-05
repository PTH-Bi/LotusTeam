using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;

namespace LotusTeam.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(AppDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<OverviewDashboardDto>> GetOverviewDashboardAsync()
        {
            try
            {
                var totalEmployees = await _context.Employees.CountAsync();
                var activeEmployees = await _context.Employees.CountAsync(e => e.Status == 1);
                var departments = await _context.Departments.CountAsync();
                var todayAttendance = await _context.Attendances
                    .CountAsync(a => a.WorkDate.Date == DateTime.Today);

                var overview = new OverviewDashboardDto
                {
                    TotalEmployees = totalEmployees,
                    ActiveEmployees = activeEmployees,
                    TotalDepartments = departments,
                    TodayAttendance = todayAttendance,
                    EmployeeStatus = new DashboardStatDto
                    {
                        Total = totalEmployees,
                        Active = activeEmployees,
                        Inactive = totalEmployees - activeEmployees
                    }
                };

                return new ApiResponse<OverviewDashboardDto>
                {
                    Success = true,
                    Data = overview,
                    Message = "Dashboard tổng quan",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overview dashboard");
                return new ApiResponse<OverviewDashboardDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy dashboard tổng quan",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<HRDashboardDto>> GetHRDashboardAsync()
        {
            try
            {
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                // Employee statistics by department
                var employeesByDepartment = await _context.Employees
                    .Include(e => e.Department)
                    .Where(e => e.Status == 1)
                    .GroupBy(e => e.DepartmentID)
                    .Select(g => new DepartmentStatDto
                    {
                        DepartmentId = g.Key,
                        DepartmentName = g.First().Department != null ?
                            g.First().Department.DepartmentName : "Không xác định",
                        EmployeeCount = g.Count()
                    })
                    .ToListAsync();

                // New hires this month
                var newHires = await _context.Employees
                    .CountAsync(e => e.HireDate >= startOfMonth && e.HireDate <= endOfMonth);

                // Upcoming birthdays
                var upcomingBirthdays = await _context.Employees
                    .Where(e => e.Status == 1 &&
                        e.DateOfBirth.Month == today.Month &&
                        e.DateOfBirth.Day >= today.Day)
                    .OrderBy(e => e.DateOfBirth.Day)
                    .Take(10)
                    .Select(e => new BirthdayDto
                    {
                        EmployeeId = e.EmployeeID,
                        EmployeeName = e.FullName,
                        DateOfBirth = e.DateOfBirth,
                        DepartmentName = e.Department != null ? e.Department.DepartmentName : ""
                    })
                    .ToListAsync();

                var hrDashboard = new HRDashboardDto
                {
                    Departments = employeesByDepartment,
                    NewHiresThisMonth = newHires,
                    UpcomingBirthdays = upcomingBirthdays
                };

                return new ApiResponse<HRDashboardDto>
                {
                    Success = true,
                    Data = hrDashboard,
                    Message = "Dashboard nhân sự",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving HR dashboard");
                return new ApiResponse<HRDashboardDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy dashboard nhân sự",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<AttendanceDashboardDto>> GetAttendanceDashboardAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var defaultFrom = DateTime.Today.AddDays(-30);
                var defaultTo = DateTime.Today;

                fromDate ??= defaultFrom;
                toDate ??= defaultTo;

                // Attendance statistics
                var attendanceStats = await _context.Attendances
                    .Where(a => a.WorkDate >= fromDate && a.WorkDate <= toDate)
                    .GroupBy(a => a.WorkDate.Date)
                    .Select(g => new AttendanceStatDto
                    {
                        Date = g.Key,
                        PresentCount = g.Count(a => a.CheckIn != null),
                        LateCount = g.Count(a => a.LateMinutes > 0),
                        AbsentCount = 0 // This would need employee count for the day
                    })
                    .OrderBy(s => s.Date)
                    .ToListAsync();

                // Today's attendance
                var todayAttendance = await _context.Attendances
                    .Include(a => a.Employee)
                        .ThenInclude(e => e.Department)
                    .Where(a => a.WorkDate.Date == DateTime.Today)
                    .Select(a => new TodayAttendanceDto
                    {
                        EmployeeId = a.EmployeeID,
                        EmployeeName = a.Employee.FullName,
                        DepartmentName = a.Employee.Department != null ?
                            a.Employee.Department.DepartmentName : "",
                        CheckIn = a.CheckIn,
                        CheckOut = a.CheckOut,
                        Status = GetAttendanceStatus(a)
                    })
                    .ToListAsync();

                var attendanceDashboard = new AttendanceDashboardDto
                {
                    DateRange = $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}",
                    Statistics = attendanceStats,
                    TodayAttendance = todayAttendance
                };

                return new ApiResponse<AttendanceDashboardDto>
                {
                    Success = true,
                    Data = attendanceDashboard,
                    Message = "Dashboard chấm công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attendance dashboard");
                return new ApiResponse<AttendanceDashboardDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy dashboard chấm công",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<PersonalDashboardDto>> GetPersonalDashboardAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user?.Employee == null)
                {
                    return new ApiResponse<PersonalDashboardDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin nhân viên",
                        StatusCode = 401
                    };
                }

                var employeeId = user.Employee.EmployeeID;
                var today = DateTime.Today;

                // Today's attendance
                var todayAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EmployeeID == employeeId &&
                                             a.WorkDate.Date == today);

                // Leave balance
                var leaveBalance = await CalculateLeaveBalanceAsync(employeeId);

                // Upcoming leaves
                var upcomingLeaves = await _context.LeaveRequests
                    .Include(l => l.LeaveType)
                    .Where(l => l.EmployeeID == employeeId &&
                               l.StartDate >= today &&
                               l.StatusID == 2) // Approved status
                    .OrderBy(l => l.StartDate)
                    .Take(5)
                    .Select(l => new LeaveDto
                    {
                        LeaveType = l.LeaveType.LeaveTypeName,
                        StartDate = l.StartDate,
                        EndDate = l.EndDate,
                        Days = l.NumberOfDays,
                        Status = "Đã duyệt"
                    })
                    .ToListAsync();

                // Recent payroll
                var recentPayroll = await _context.Payrolls
                    .Where(p => p.EmployeeID == employeeId)
                    .OrderByDescending(p => p.PayPeriod)
                    .Take(3)
                    .Select(p => new PayrollSummaryDto
                    {
                        Period = p.PayPeriod.ToString("MM/yyyy"),
                        GrossSalary = p.GrossSalary,
                        NetSalary = p.NetSalary,
                        Status = GetPayrollStatus(p.StatusID)
                    })
                    .ToListAsync();

                var personalDashboard = new PersonalDashboardDto
                {
                    EmployeeName = user.Employee.FullName,
                    EmployeeCode = user.Employee.EmployeeCode,
                    DepartmentName = user.Employee.Department?.DepartmentName,
                    PositionName = user.Employee.Position?.PositionName,
                    TodayAttendance = todayAttendance != null ? new TodayAttendanceStatusDto
                    {
                        CheckIn = todayAttendance.CheckIn,
                        CheckOut = todayAttendance.CheckOut,
                        Status = GetAttendanceStatus(todayAttendance),
                        WorkingHours = todayAttendance.WorkingHours
                    } : null,
                    LeaveBalance = leaveBalance,
                    UpcomingLeaves = upcomingLeaves,
                    RecentPayrolls = recentPayroll
                };

                return new ApiResponse<PersonalDashboardDto>
                {
                    Success = true,
                    Data = personalDashboard,
                    Message = "Dashboard cá nhân",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving personal dashboard");
                return new ApiResponse<PersonalDashboardDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy dashboard cá nhân",
                    StatusCode = 500
                };
            }
        }

        // ========================= Helper Methods =========================
        private async Task<LeaveBalanceDto> CalculateLeaveBalanceAsync(int employeeId)
        {
            // This is a simplified version
            // In production, you'd calculate based on company policy
            var currentYear = DateTime.Now.Year;

            var usedLeaves = await _context.LeaveRequests
                .Where(l => l.EmployeeID == employeeId &&
                           l.StartDate.Year == currentYear &&
                           l.StatusID == 2) // Approved
                .SumAsync(l => l.NumberOfDays);

            return new LeaveBalanceDto
            {
                AnnualLeave = 12, // Default annual leave
                SickLeave = 5,    // Default sick leave
                UsedAnnualLeave = (decimal)usedLeaves,
                RemainingAnnualLeave = 12 - (decimal)usedLeaves
            };
        }

        private string GetAttendanceStatus(Attendances attendance)
        {
            if (attendance.CheckIn == null)
                return "Chưa chấm công";

            if (attendance.LateMinutes > 0)
                return $"Đi muộn ({attendance.LateMinutes} phút)";

            return "Đúng giờ";
        }

        private string GetPayrollStatus(short statusId)
        {
            return statusId switch
            {
                1 => "Chưa xử lý",
                2 => "Đã xử lý",
                3 => "Đã thanh toán",
                _ => "Không xác định"
            };
        }

        // ========================= DTO Definitions =========================
        public class OverviewDashboardDto
        {
            public int TotalEmployees { get; set; }
            public int ActiveEmployees { get; set; }
            public int TotalDepartments { get; set; }
            public int TodayAttendance { get; set; }
            public DashboardStatDto EmployeeStatus { get; set; } = new();
        }

        public class HRDashboardDto
        {
            public List<DepartmentStatDto> Departments { get; set; } = new();
            public int NewHiresThisMonth { get; set; }
            public List<BirthdayDto> UpcomingBirthdays { get; set; } = new();
        }

        public class AttendanceDashboardDto
        {
            public string DateRange { get; set; } = string.Empty;
            public List<AttendanceStatDto> Statistics { get; set; } = new();
            public List<TodayAttendanceDto> TodayAttendance { get; set; } = new();
        }

        public class PersonalDashboardDto
        {
            public string EmployeeName { get; set; } = string.Empty;
            public string EmployeeCode { get; set; } = string.Empty;
            public string? DepartmentName { get; set; }
            public string? PositionName { get; set; }
            public TodayAttendanceStatusDto? TodayAttendance { get; set; }
            public LeaveBalanceDto LeaveBalance { get; set; } = new();
            public List<LeaveDto> UpcomingLeaves { get; set; } = new();
            public List<PayrollSummaryDto> RecentPayrolls { get; set; } = new();
        }

        public class DashboardStatDto
        {
            public int Total { get; set; }
            public int Active { get; set; }
            public int Inactive { get; set; }
        }

        public class DepartmentStatDto
        {
            public int? DepartmentId { get; set; }
            public string DepartmentName { get; set; } = string.Empty;
            public int EmployeeCount { get; set; }
        }

        public class BirthdayDto
        {
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public DateTime DateOfBirth { get; set; }
            public string DepartmentName { get; set; } = string.Empty;
        }

        public class AttendanceStatDto
        {
            public DateTime Date { get; set; }
            public int PresentCount { get; set; }
            public int LateCount { get; set; }
            public int AbsentCount { get; set; }
        }

        public class TodayAttendanceDto
        {
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public string DepartmentName { get; set; } = string.Empty;
            public TimeSpan? CheckIn { get; set; }
            public TimeSpan? CheckOut { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        public class TodayAttendanceStatusDto
        {
            public TimeSpan? CheckIn { get; set; }
            public TimeSpan? CheckOut { get; set; }
            public string Status { get; set; } = string.Empty;
            public decimal? WorkingHours { get; set; }
        }

        public class LeaveBalanceDto
        {
            public decimal AnnualLeave { get; set; }
            public decimal SickLeave { get; set; }
            public decimal UsedAnnualLeave { get; set; }
            public decimal RemainingAnnualLeave { get; set; }
        }

        public class LeaveDto
        {
            public string LeaveType { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public decimal Days { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        public class PayrollSummaryDto
        {
            public string Period { get; set; } = string.Empty;
            public decimal GrossSalary { get; set; }
            public decimal NetSalary { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}