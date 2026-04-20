using LotusTeam.Data;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Services
{
    public class LeaveService : ILeaveService
    {
        private readonly AppDbContext _context;

        public LeaveService(AppDbContext context)
        {
            _context = context;
        }

        // ================= LOẠI NGHỈ =================
        public async Task<List<LeaveType>> GetLeaveTypesAsync()
        {
            return await _context.LeaveTypes
                .Where(x => x.IsActive)
                .ToListAsync();
        }

        public async Task<LeaveType> CreateLeaveTypeAsync(LeaveType leaveType)
        {
            _context.LeaveTypes.Add(leaveType);
            await _context.SaveChangesAsync();
            return leaveType;
        }

        // ================= ĐĂNG KÝ NGHỈ =================
        public async Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest request)
        {
            request.StatusID = 1; // Pending
            _context.LeaveRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        // ================= CÁ NHÂN =================
        public async Task<List<LeaveRequest>> GetMyLeaveAsync(int employeeId)
        {
            return await _context.LeaveRequests
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeID == employeeId)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();
        }

        // ================= LỊCH SỬ =================
        public async Task<List<LeaveRequest>> GetLeaveHistoryAsync(int employeeId)
        {
            return await _context.LeaveRequests
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeID == employeeId && x.StatusID != 1)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();
        }

        // ================= DUYỆT =================
        public async Task<bool> ApproveLeaveAsync(int leaveId, int approverId)
        {
            var leave = await _context.LeaveRequests.FindAsync(leaveId);
            if (leave == null) return false;

            leave.StatusID = 2; // Approved
            leave.ApprovedBy = approverId;
            leave.ApprovedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectLeaveAsync(int leaveId, int approverId)
        {
            var leave = await _context.LeaveRequests.FindAsync(leaveId);
            if (leave == null) return false;

            leave.StatusID = 3; // Rejected
            leave.ApprovedBy = approverId;
            leave.ApprovedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        // ================= SỐ DƯ =================
        public async Task<decimal> GetLeaveBalanceAsync(int employeeId, int leaveTypeId)
        {
            var used = await _context.LeaveRequests
                .Where(x =>
                    x.EmployeeID == employeeId &&
                    x.LeaveTypeID == leaveTypeId &&
                    x.StatusID == 2)
                .SumAsync(x => x.NumberOfDays);

            decimal policyDays = 12; // tạm thời
            return policyDays - used;
        }

        // ================= LỊCH NGHỈ (🔥 FIX LỖI CỦA BẠN) =================
        public async Task<List<LeaveRequest>> GetLeaveCalendarAsync(DateTime from, DateTime to)
        {
            return await _context.LeaveRequests
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.StatusID == 2 &&
                    x.StartDate <= to &&
                    x.EndDate >= from)
                .OrderBy(x => x.StartDate)
                .ToListAsync();
        }
    }
}
