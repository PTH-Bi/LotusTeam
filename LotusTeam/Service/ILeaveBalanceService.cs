using LotusTeam.Models;

namespace LotusTeam.Services
{
    public interface ILeaveBalanceService
    {
        Task<List<LeaveBalances>> GetByEmployeeAsync(int employeeId);
        Task<LeaveBalances?> GetByYearAsync(int employeeId, int year);
        Task<LeaveBalances> CreateOrUpdateAsync(LeaveBalances model);
        Task<bool> DeleteAsync(int id);
    }
}
