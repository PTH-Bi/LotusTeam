using LotusTeam.Models;

namespace LotusTeam.Services
{
    public interface ILeaveService
    {
        // ===== LOẠI NGHỈ =====
        Task<List<LeaveType>> GetLeaveTypesAsync();
        Task<LeaveType> CreateLeaveTypeAsync(LeaveType leaveType);

        // ===== ĐƠN NGHỈ =====
        Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest request);
        Task<List<LeaveRequest>> GetMyLeaveAsync(int employeeId);
        Task<List<LeaveRequest>> GetLeaveHistoryAsync(int employeeId);

        // ===== DUYỆT =====
        Task<bool> ApproveLeaveAsync(int leaveId, int approverId);
        Task<bool> RejectLeaveAsync(int leaveId, int approverId);

        // ===== SỐ DƯ =====
        Task<decimal> GetLeaveBalanceAsync(int employeeId, int leaveTypeId);

        // ===== LỊCH NGHỈ (🔥 BẮT BUỘC CÓ) =====
        Task<List<LeaveRequest>> GetLeaveCalendarAsync(DateTime from, DateTime to);
    }
}
