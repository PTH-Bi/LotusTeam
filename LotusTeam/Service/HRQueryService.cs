using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;

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
    }
}