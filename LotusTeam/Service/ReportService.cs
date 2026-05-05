using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Service
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> EmployeeReportAsync()
        {
            return new
            {
                Total = await _context.Employees.CountAsync(),
                Active = await _context.Employees.CountAsync(e => e.Status == 1)
            };
        }

        public async Task<object> AttendanceReportAsync()
        {
            return await _context.Attendances.CountAsync();
        }

        public async Task<object> PayrollReportAsync()
        {
            return await _context.Payrolls.CountAsync();
        }

        public async Task<object> LeaveReportAsync()
        {
            return await _context.LeaveRequests.CountAsync();
        }
    }
}
