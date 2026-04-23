using Microsoft.AspNetCore.Mvc;
using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LotusTeam.API.Controllers
{
    /// <summary>
    /// API Dashboard - Thống kê và báo cáo tổng quan
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(AppDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Overview Dashboard

        /// <summary>
        /// Lấy dashboard tổng quan (dành cho tất cả nhân viên)
        /// </summary>
        [HttpGet("overview")]
        public async Task<ActionResult<ApiResponse<OverviewDashboardDto>>> GetOverviewDashboard()
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

                return Ok(new ApiResponse<OverviewDashboardDto>
                {
                    Success = true,
                    Message = "Dashboard tổng quan",
                    Data = overview
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overview dashboard");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy dashboard tổng quan",
                });
            }
        }

        #endregion

        #region HR Dashboard

        /// <summary>
        /// Lấy dashboard nhân sự (thống kê phòng ban, nhân viên mới, sinh nhật sắp tới)
        /// </summary>
        [HttpGet("hr")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,DIRECTOR,MANAGER")]
        public async Task<ActionResult<ApiResponse<HRDashboardDto>>> GetHRDashboard()
        {
            try
            {
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                // Get current user info
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(userIdClaim, out int currentUserId);

                var currentUser = await _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.UserID == currentUserId);
                var currentEmployeeId = currentUser?.EmployeeID;

                // Filter employees based on role
                IQueryable<Employees> employeeQuery = _context.Employees;

                // MANAGER only sees their department
                if (userRole == "MANAGER" && currentEmployeeId.HasValue)
                {
                    var managerEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeID == currentEmployeeId);

                    if (managerEmployee?.DepartmentID != null)
                    {
                        employeeQuery = employeeQuery.Where(e => e.DepartmentID == managerEmployee.DepartmentID);
                    }
                }

                // Employee statistics by department
                var employeesByDepartment = await employeeQuery
                    .Where(e => e.Status == 1)
                    .Include(e => e.Department)
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
                int newHires = 0;
                if (new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "DIRECTOR" }.Contains(userRole))
                {
                    newHires = await _context.Employees
                        .CountAsync(e => e.HireDate >= startOfMonth && e.HireDate <= endOfMonth);
                }

                // Upcoming birthdays
                var birthdayQuery = _context.Employees
                    .Where(e => e.Status == 1 &&
                        e.DateOfBirth.Month == today.Month &&
                        e.DateOfBirth.Day >= today.Day);

                if (userRole == "MANAGER" && currentEmployeeId.HasValue)
                {
                    var managerEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeID == currentEmployeeId);

                    if (managerEmployee?.DepartmentID != null)
                    {
                        birthdayQuery = birthdayQuery.Where(e => e.DepartmentID == managerEmployee.DepartmentID);
                    }
                }

                var upcomingBirthdays = await birthdayQuery
                    .OrderBy(e => e.DateOfBirth.Day)
                    .Take(10)
                    .Select(e => new BirthdayDto
                    {
                        EmployeeId = e.EmployeeID,
                        EmployeeName = e.FullName,
                        DateOfBirth = e.DateOfBirth,
                        DepartmentName = e.Department != null ? e.Department.DepartmentName : "",
                        Age = DateTime.Now.Year - e.DateOfBirth.Year
                    })
                    .ToListAsync();

                var hrDashboard = new HRDashboardDto
                {
                    Departments = employeesByDepartment,
                    NewHiresThisMonth = newHires,
                    UpcomingBirthdays = upcomingBirthdays,
                    TotalEmployees = await employeeQuery.CountAsync(e => e.Status == 1)
                };

                return Ok(new ApiResponse<HRDashboardDto>
                {
                    Success = true,
                    Message = "Dashboard nhân sự",
                    Data = hrDashboard
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving HR dashboard");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy dashboard nhân sự",
                });
            }
        }

        #endregion

        #region Attendance Dashboard

        /// <summary>
        /// Lấy dashboard chấm công (thống kê điểm danh)
        /// </summary>
        [HttpGet("attendance")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,DIRECTOR,MANAGER")]
        public async Task<ActionResult<ApiResponse<AttendanceDashboardDto>>> GetAttendanceDashboard(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var defaultFrom = DateTime.Today.AddDays(-30);
                var defaultTo = DateTime.Today;

                fromDate ??= defaultFrom;
                toDate ??= defaultTo;

                // Validate date range
                if (fromDate > toDate)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc"
                    });
                }

                // Get current user info
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(userIdClaim, out int currentUserId);

                var currentUser = await _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.UserID == currentUserId);
                var currentEmployeeId = currentUser?.EmployeeID;

                // Filter attendance based on role
                IQueryable<Attendances> attendanceQuery = _context.Attendances
                    .Include(a => a.Employee)
                        .ThenInclude(e => e.Department)
                    .Where(a => a.WorkDate >= fromDate && a.WorkDate <= toDate);

                // MANAGER only sees their department
                if (userRole == "MANAGER" && currentEmployeeId.HasValue)
                {
                    var managerEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeID == currentEmployeeId);

                    if (managerEmployee?.DepartmentID != null)
                    {
                        attendanceQuery = attendanceQuery.Where(a => a.Employee.DepartmentID == managerEmployee.DepartmentID);
                    }
                }

                // Get total employees count for absence calculation
                var totalEmployeesQuery = _context.Employees.Where(e => e.Status == 1);
                if (userRole == "MANAGER" && currentEmployeeId.HasValue)
                {
                    var managerEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeID == currentEmployeeId);
                    if (managerEmployee?.DepartmentID != null)
                    {
                        totalEmployeesQuery = totalEmployeesQuery.Where(e => e.DepartmentID == managerEmployee.DepartmentID);
                    }
                }
                var totalEmployees = await totalEmployeesQuery.CountAsync();

                // Attendance statistics
                var attendanceStats = await attendanceQuery
                    .GroupBy(a => a.WorkDate.Date)
                    .Select(g => new AttendanceStatDto
                    {
                        Date = g.Key,
                        PresentCount = g.Count(a => a.CheckIn != null),
                        LateCount = g.Count(a => a.LateMinutes > 0),
                        AbsentCount = totalEmployees - g.Count(a => a.CheckIn != null)
                    })
                    .OrderBy(s => s.Date)
                    .ToListAsync();

                // Today's attendance
                var todayAttendanceQuery = _context.Attendances
                    .Include(a => a.Employee)
                        .ThenInclude(e => e.Department)
                    .Where(a => a.WorkDate.Date == DateTime.Today);

                if (userRole == "MANAGER" && currentEmployeeId.HasValue)
                {
                    var managerEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeID == currentEmployeeId);

                    if (managerEmployee?.DepartmentID != null)
                    {
                        todayAttendanceQuery = todayAttendanceQuery.Where(a => a.Employee.DepartmentID == managerEmployee.DepartmentID);
                    }
                }

                var todayAttendance = await todayAttendanceQuery
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
                    TodayAttendance = todayAttendance,
                    Summary = new AttendanceSummaryDto
                    {
                        TotalDays = attendanceStats.Count,
                        AveragePresent = attendanceStats.Any() ? (int)attendanceStats.Average(s => s.PresentCount) : 0,
                        AverageLate = attendanceStats.Any() ? (int)attendanceStats.Average(s => s.LateCount) : 0
                    }
                };

                return Ok(new ApiResponse<AttendanceDashboardDto>
                {
                    Success = true,
                    Message = "Dashboard chấm công",
                    Data = attendanceDashboard
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attendance dashboard");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy dashboard chấm công",
                });
            }
        }

        #endregion

        #region Personal Dashboard

        /// <summary>
        /// Lấy dashboard cá nhân (thông tin riêng của nhân viên đang đăng nhập)
        /// </summary>
        [HttpGet("personal")]
        public async Task<ActionResult<ApiResponse<PersonalDashboardDto>>> GetPersonalDashboard()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không xác định được người dùng"
                    });
                }

                var user = await _context.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(u => u.Employee)
                        .ThenInclude(e => e.Position)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user?.Employee == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không tìm thấy thông tin nhân viên"
                    });
                }

                var employeeId = user.Employee.EmployeeID;
                var today = DateTime.Today;
                var currentYear = DateTime.Now.Year;

                // Today's attendance
                var todayAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EmployeeID == employeeId &&
                                             a.WorkDate.Date == today);

                // Leave balance (assuming 12 days annual leave per year)
                var usedLeave = await _context.LeaveRequests
                    .Where(l => l.EmployeeID == employeeId &&
                               l.StatusID == 2 && // Approved
                               l.StartDate.Year == currentYear)
                    .SumAsync(l => l.NumberOfDays);

                var leaveBalance = new LeaveBalanceDto
                {
                    AnnualLeave = 12,
                    Used = usedLeave,
                    Remaining = 12 - usedLeave,
                    SickLeave = 0,
                    UnpaidLeave = 0
                };

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
                        LeaveType = l.LeaveType != null ? l.LeaveType.LeaveTypeName : "Nghỉ phép",
                        StartDate = l.StartDate,
                        EndDate = l.EndDate,
                        Days = l.NumberOfDays,
                        Status = "Đã duyệt"
                    })
                    .ToListAsync();

                // Recent payroll (last 3 months)
                var recentPayrolls = await _context.Payrolls
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
                    RecentPayrolls = recentPayrolls
                };

                return Ok(new ApiResponse<PersonalDashboardDto>
                {
                    Success = true,
                    Message = "Dashboard cá nhân",
                    Data = personalDashboard
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving personal dashboard");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy dashboard cá nhân",
                });
            }
        }

        #endregion

        #region Leave Dashboard (Manager Only)

        /// <summary>
        /// Lấy dashboard nghỉ phép (dành cho quản lý)
        /// </summary>
        [HttpGet("leave")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,MANAGER")]
        public async Task<ActionResult<ApiResponse<LeaveDashboardDto>>> GetLeaveDashboard()
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(userIdClaim, out int currentUserId);

                var currentUser = await _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.UserID == currentUserId);
                var currentEmployeeId = currentUser?.EmployeeID;

                // Filter leave requests based on role
                IQueryable<LeaveRequest> leaveQuery = _context.LeaveRequests
                    .Include(l => l.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(l => l.LeaveType);

                if (userRole == "MANAGER" && currentEmployeeId.HasValue)
                {
                    var managerEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeID == currentEmployeeId);

                    if (managerEmployee?.DepartmentID != null)
                    {
                        leaveQuery = leaveQuery.Where(l => l.Employee.DepartmentID == managerEmployee.DepartmentID);
                    }
                }

                // Pending leaves
                var pendingLeaves = await leaveQuery
                    .Where(l => l.StatusID == 1) // Pending
                    .OrderBy(l => l.StartDate)
                    .Take(20)
                    .Select(l => new PendingLeaveDto
                    {
                        LeaveId = l.LeaveID,
                        EmployeeName = l.Employee.FullName,
                        DepartmentName = l.Employee.Department != null ? l.Employee.Department.DepartmentName : "",
                        LeaveType = l.LeaveType != null ? l.LeaveType.LeaveTypeName : "",
                        StartDate = l.StartDate,
                        EndDate = l.EndDate,
                        Days = l.NumberOfDays,
                        Reason = l.Reason ?? ""
                    })
                    .ToListAsync();

                // Monthly statistics
                var startOfYear = new DateTime(DateTime.Now.Year, 1, 1);
                var monthlyStats = await leaveQuery
                    .Where(l => l.StartDate >= startOfYear && l.StatusID == 2)
                    .GroupBy(l => new { l.StartDate.Year, l.StartDate.Month })
                    .Select(g => new MonthlyLeaveStatDto
                    {
                        Month = g.Key.Month,
                        Year = g.Key.Year,
                        TotalDays = g.Sum(l => l.NumberOfDays),
                        RequestCount = g.Count()
                    })
                    .OrderBy(s => s.Year)
                    .ThenBy(s => s.Month)
                    .ToListAsync();

                var leaveDashboard = new LeaveDashboardDto
                {
                    PendingLeaves = pendingLeaves,
                    MonthlyStatistics = monthlyStats,
                    TotalPending = pendingLeaves.Count
                };

                return Ok(new ApiResponse<LeaveDashboardDto>
                {
                    Success = true,
                    Message = "Dashboard nghỉ phép",
                    Data = leaveDashboard
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leave dashboard");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy dashboard nghỉ phép",
                });
            }
        }

        #endregion

        #region Helper Methods

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

        #endregion
    }

    #region DTO Classes

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
        public int TotalEmployees { get; set; }
    }

    public class AttendanceDashboardDto
    {
        public string DateRange { get; set; } = string.Empty;
        public List<AttendanceStatDto> Statistics { get; set; } = new();
        public List<TodayAttendanceDto> TodayAttendance { get; set; } = new();
        public AttendanceSummaryDto Summary { get; set; } = new();
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

    public class LeaveDashboardDto
    {
        public List<PendingLeaveDto> PendingLeaves { get; set; } = new();
        public List<MonthlyLeaveStatDto> MonthlyStatistics { get; set; } = new();
        public int TotalPending { get; set; }
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
        public int Age { get; set; }
    }

    public class AttendanceStatDto
    {
        public DateTime Date { get; set; }
        public int PresentCount { get; set; }
        public int LateCount { get; set; }
        public int AbsentCount { get; set; }
    }

    public class AttendanceSummaryDto
    {
        public int TotalDays { get; set; }
        public int AveragePresent { get; set; }
        public int AverageLate { get; set; }
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
        public decimal Used { get; set; }
        public decimal Remaining { get; set; }
        public decimal SickLeave { get; set; }
        public decimal UnpaidLeave { get; set; }
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

    public class PendingLeaveDto
    {
        public int LeaveId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Days { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class MonthlyLeaveStatDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalDays { get; set; }
        public int RequestCount { get; set; }
    }

    #endregion
}