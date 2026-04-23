using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;

namespace LotusTeam.Service
{
    public class HRQueryService
    {
        private readonly AppDbContext _context;

        public HRQueryService(AppDbContext context)
        {
            _context = context;
        }

        // ===== GET LEAVE =====
        public async Task<decimal> GetLeave(int userId)
        {
            return await _context.LeaveRequests
                .Where(x => x.EmployeeID == userId)
                .SumAsync(x => x.NumberOfDays);
        }

        // ===== GET SALARY =====
        public async Task<decimal> GetSalary(int userId)
        {
            var data = await _context.Payrolls
                .Where(x => x.EmployeeID == userId)
                .OrderByDescending(x => x.PayPeriod)
                .FirstOrDefaultAsync();

            return data?.NetSalary ?? 0;
        }

        // ===== GET ATTENDANCE =====
        public async Task<int> GetAttendance(int userId)
        {
            return await _context.Attendances
                .Where(x => x.EmployeeID == userId && x.IsConfirmed)
                .CountAsync();
        }

        // ===== GET TEAM LEAVE SUMMARY =====
        public async Task<TeamLeaveSummary> GetTeamLeaveSummary(int managerId)
        {
            // Lấy danh sách nhân viên dưới quyền manager
            // Sử dụng PositionID hoặc DepartmentID để xác định quản lý
            // Giả sử manager là người có PositionID = 1 (Manager)
            var teamEmployeeIds = await _context.Employees
                .Where(e => e.DepartmentID != null && e.PositionID != 1) // Không phải manager
                .Select(e => e.EmployeeID)
                .ToListAsync();

            var today = DateTime.Today;

            var used = await _context.LeaveRequests
                .Where(l => teamEmployeeIds.Contains(l.EmployeeID)
                            && l.StatusID == 3  // Approved
                            && l.StartDate <= today)
                .SumAsync(l => l.NumberOfDays);

            var pending = await _context.LeaveRequests
                .Where(l => teamEmployeeIds.Contains(l.EmployeeID)
                            && l.StatusID == 1)  // Pending
                .SumAsync(l => l.NumberOfDays);

            var upcoming = await _context.LeaveRequests
                .Where(l => teamEmployeeIds.Contains(l.EmployeeID)
                            && l.StatusID == 3  // Approved
                            && l.StartDate > today)
                .SumAsync(l => l.NumberOfDays);

            return new TeamLeaveSummary
            {
                Used = used,
                Pending = pending,
                Upcoming = upcoming
            };
        }

        // ===== SEARCH EMPLOYEE =====
        public async Task<EmployeeInfoDto?> SearchEmployee(string searchText)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.EmployeeCode.Contains(searchText)
                            || e.FullName.Contains(searchText))
                .Select(e => new EmployeeInfoDto
                {
                    EmployeeID = e.EmployeeID,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : "",
                    PositionName = e.Position != null ? e.Position.PositionName : "",
                    JoinDate = e.HireDate  // Sử dụng HireDate thay vì JoinDate
                })
                .FirstOrDefaultAsync();

            return employee;
        }
    }

    // DTO classes
    public class TeamLeaveSummary
    {
        public decimal Used { get; set; }
        public decimal Pending { get; set; }
        public decimal Upcoming { get; set; }
    }

    public class EmployeeInfoDto
    {
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
    }
}