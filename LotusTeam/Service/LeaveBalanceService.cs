using LotusTeam.Data;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Services
{
    public class LeaveBalanceService : ILeaveBalanceService
    {
        private readonly AppDbContext _context;

        public LeaveBalanceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeaveBalances>> GetByEmployeeAsync(int employeeId)
        {
            return await _context.LeaveBalances
                .Include(x => x.Employee)
                .Where(x => x.EmployeeID == employeeId)
                .OrderByDescending(x => x.Year)
                .ToListAsync();
        }

        public async Task<LeaveBalances?> GetByYearAsync(int employeeId, int year)
        {
            return await _context.LeaveBalances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeID == employeeId &&
                    x.Year == year);
        }

        public async Task<LeaveBalances> CreateOrUpdateAsync(LeaveBalances model)
        {
            var existing = await _context.LeaveBalances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeID == model.EmployeeID &&
                    x.Year == model.Year);

            if (existing == null)
            {
                _context.LeaveBalances.Add(model);
            }
            else
            {
                existing.AnnualQuota = model.AnnualQuota;
                existing.UsedDays = model.UsedDays;
                existing.UnpaidDays = model.UnpaidDays;
                existing.ConsecutiveLeaveDays = model.ConsecutiveLeaveDays;
                existing.LastLeaveEndDate = model.LastLeaveEndDate;
                existing.IsReset = model.IsReset;
                existing.UpdatedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.LeaveBalances.FindAsync(id);
            if (entity == null) return false;

            _context.LeaveBalances.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
