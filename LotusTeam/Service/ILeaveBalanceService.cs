using LotusTeam.Models;
using LotusTeam.DTOs;

namespace LotusTeam.Services
{
    /// <summary>
    /// Interface service quản lý số dư ngày phép của nhân viên
    /// </summary>
    public interface ILeaveBalanceService
    {
        /// <summary>
        /// Lấy danh sách số dư ngày phép của nhân viên (tất cả các năm) - trả về Entity
        /// </summary>
        Task<List<LeaveBalanceDto>> GetByEmployeeWithInfoAsync(int employeeId);

        /// <summary>
        /// Lấy số dư ngày phép của nhân viên theo năm cụ thể (trả về Entity)
        /// </summary>
        Task<LeaveBalances?> GetByYearAsync(int employeeId, int year);

        /// <summary>
        /// Lấy số dư ngày phép của nhân viên theo năm (trả về DTO)
        /// </summary>
        Task<LeaveBalanceDto?> GetByEmployeeAndYearAsync(int employeeId, int? year = null);

        /// <summary>
        /// Lấy danh sách số dư ngày phép của tất cả nhân viên
        /// </summary>
        Task<IEnumerable<LeaveBalanceDto>> GetAllAsync(int? year = null, int? departmentId = null);

        /// <summary>
        /// Tạo mới hoặc cập nhật số dư ngày phép
        /// </summary>
        Task<ServiceResult<LeaveBalanceDto>> CreateOrUpdateAsync(LeaveBalances model);

        /// <summary>
        /// Cập nhật số ngày đã sử dụng
        /// </summary>
        Task<ServiceResult<LeaveBalanceDto>> UpdateUsedDaysAsync(int employeeId, int year, decimal daysToAdd);

        /// <summary>
        /// Reset số dư ngày phép cho năm mới
        /// </summary>
        Task<ServiceResult<bool>> ResetLeaveBalanceAsync(int employeeId, int newYear, decimal carryOverDays = 0);

        /// <summary>
        /// Xóa số dư ngày phép
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Kiểm tra nhân viên có cùng phòng ban không (dùng cho phân quyền Manager)
        /// </summary>
        Task<bool> IsEmployeeInSameDepartment(int? managerEmployeeId, int targetEmployeeId);
        Task GetEmployeeInfoAsync(int employeeID);
    }

    /// <summary>
    /// Kết quả trả về từ service (chuẩn hóa)
    /// </summary>
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}