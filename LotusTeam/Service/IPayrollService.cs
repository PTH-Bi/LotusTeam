using LotusTeam.Models;

namespace LotusTeam.Service
{
    public interface IPayrollService
    {
        // Tính lương
        Task<List<Payrolls>> CalculatePayrollAsync(DateTime payPeriod);
        Task<List<Payrolls>> CalculatePayrollBulkAsync(DateTime payPeriod, List<int> employeeIds);

        // Lấy dữ liệu
        Task<List<Payrolls>> GetPayrollByEmployeeAsync(int employeeId);
        Task<Payrolls?> GetPayrollDetailAsync(int payrollId);
        Task<object> GetAllPayrollsAsync(int? month, int? year, int page, int pageSize);
        Task<object> GetPayrollFlatAsync(DateTime payPeriod); // Trả về object thay vì ViewModel

        // Phê duyệt
        Task<bool> ApprovePayrollAsync(DateTime payPeriod);

        // Thuế
        Task<PayrollTaxSnapshot> CreateTaxSnapshotAsync(int payrollId);
        Task<List<Payrolls>> GetPayrollHistoryAsync(int employeeId);

        // Quản lý phụ cấp, thưởng, khấu trừ
        Task<Allowances> AddAllowanceAsync(int employeeId, DateTime month, string allowanceName, decimal amount, string? note = null);
        Task<Bonus> AddBonusAsync(int employeeId, DateTime month, string bonusName, decimal amount, string? reason = null);
        Task<Deduction> AddDeductionAsync(int employeeId, DateTime month, string deductionName, decimal amount, string? note = null);

        // Lấy danh sách
        Task<List<Allowances>> GetAllowancesByMonthAsync(int employeeId, DateTime month);
        Task<List<Bonus>> GetBonusesByMonthAsync(int employeeId, DateTime month);
        Task<List<Deduction>> GetDeductionsByMonthAsync(int employeeId, DateTime month);

        // Quản lý người phụ thuộc
        Task<Dependent> AddDependentAsync(int employeeId, string fullName, string relationship, DateTime birthDate, string? identityNumber = null);
        Task<bool> DeactivateDependentAsync(int dependentId, DateTime? endDate = null);
        Task<List<Dependent>> GetDependentsByEmployeeAsync(int employeeId, bool onlyActive = true);
        Task<int> GetActiveDependentCountAsync(int employeeId, DateTime month);

        // Phụ cấp thân nhân
        Task<DependentAllowance?> CalculateDependentAllowanceAsync(int employeeId, DateTime month, decimal amountPerDependent = 500000);
        Task<List<DependentAllowance>> CreateDependentAllowancesForAllAsync(DateTime month, decimal amountPerDependent = 500000);
        Task<DependentAllowance?> GetDependentAllowanceByMonthAsync(int employeeId, DateTime month);
    }
}