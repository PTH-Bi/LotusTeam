using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Service;

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

        public async Task<LeaveBalanceDto?> GetByEmployeeAndYearAsync(int employeeId, int? year = null)
        {
            var targetYear = year ?? DateTime.Now.Year;

            var balance = await _context.LeaveBalances
                .Include(x => x.Employee)
                    .ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeID == employeeId &&
                    x.Year == targetYear);

            if (balance == null) return null;

            return new LeaveBalanceDto
            {
                LeaveBalanceID = balance.LeaveBalanceID,
                EmployeeID = balance.EmployeeID,
                EmployeeName = balance.Employee?.FullName ?? "",
                EmployeeCode = balance.Employee?.EmployeeCode ?? "",
                DepartmentName = balance.Employee?.Department?.DepartmentName ?? "",
                Year = balance.Year,
                AnnualQuota = balance.AnnualQuota,
                UsedDays = balance.UsedDays,
                UnpaidDays = balance.UnpaidDays,
                ConsecutiveLeaveDays = balance.ConsecutiveLeaveDays,
                LastLeaveEndDate = balance.LastLeaveEndDate,
                IsReset = balance.IsReset,
                UpdatedDate = balance.UpdatedDate
            };
        }

        public async Task<IEnumerable<LeaveBalanceDto>> GetAllAsync(int? year = null, int? departmentId = null)
        {
            var targetYear = year ?? DateTime.Now.Year;

            var query = _context.LeaveBalances
                .Include(x => x.Employee)
                    .ThenInclude(e => e.Department)
                .Where(x => x.Year == targetYear);

            if (departmentId.HasValue)
            {
                query = query.Where(x => x.Employee.DepartmentID == departmentId);
            }

            var balances = await query.ToListAsync();

            return balances.Select(b => new LeaveBalanceDto
            {
                LeaveBalanceID = b.LeaveBalanceID,
                EmployeeID = b.EmployeeID,
                EmployeeName = b.Employee?.FullName ?? "",
                EmployeeCode = b.Employee?.EmployeeCode ?? "",
                DepartmentName = b.Employee?.Department?.DepartmentName ?? "",
                Year = b.Year,
                AnnualQuota = b.AnnualQuota,
                UsedDays = b.UsedDays,
                UnpaidDays = b.UnpaidDays,
                ConsecutiveLeaveDays = b.ConsecutiveLeaveDays,
                LastLeaveEndDate = b.LastLeaveEndDate,
                IsReset = b.IsReset,
                UpdatedDate = b.UpdatedDate
            });
        }

        public async Task<ServiceResult<LeaveBalanceDto>> CreateOrUpdateAsync(LeaveBalances model)
        {
            var result = new ServiceResult<LeaveBalanceDto>();

            try
            {
                var existing = await _context.LeaveBalances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeID == model.EmployeeID &&
                        x.Year == model.Year);

                if (existing == null)
                {
                    model.UpdatedDate = DateTime.Now;
                    _context.LeaveBalances.Add(model);
                    await _context.SaveChangesAsync();

                    result.Success = true;
                    result.Message = "Tạo số dư phép thành công";
                    result.Data = await GetByEmployeeAndYearAsync(model.EmployeeID, model.Year);
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

                    await _context.SaveChangesAsync();

                    result.Success = true;
                    result.Message = "Cập nhật số dư phép thành công";
                    result.Data = await GetByEmployeeAndYearAsync(model.EmployeeID, model.Year);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Lỗi khi lưu số dư phép";
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        public async Task<ServiceResult<LeaveBalanceDto>> UpdateUsedDaysAsync(int employeeId, int year, decimal daysToAdd)
        {
            var result = new ServiceResult<LeaveBalanceDto>();

            try
            {
                var balance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeID == employeeId &&
                        x.Year == year);

                if (balance == null)
                {
                    result.Success = false;
                    result.Message = $"Không tìm thấy số dư phép cho nhân viên {employeeId} năm {year}";
                    return result;
                }

                var newUsedDays = balance.UsedDays + daysToAdd;

                if (newUsedDays < 0)
                {
                    result.Success = false;
                    result.Message = "Số ngày đã sử dụng không thể âm";
                    return result;
                }

                if (newUsedDays > balance.AnnualQuota)
                {
                    result.Success = false;
                    result.Message = $"Số ngày đã sử dụng ({newUsedDays}) vượt quá số ngày phép năm ({balance.AnnualQuota})";
                    return result;
                }

                balance.UsedDays = newUsedDays;
                balance.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = daysToAdd >= 0
                    ? $"Đã cộng {daysToAdd} ngày đã sử dụng"
                    : $"Đã trừ {Math.Abs(daysToAdd)} ngày đã sử dụng";
                result.Data = await GetByEmployeeAndYearAsync(employeeId, year);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Lỗi khi cập nhật số ngày đã sử dụng";
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        public async Task<ServiceResult<bool>> ResetLeaveBalanceAsync(int employeeId, int newYear, decimal carryOverDays = 0)
        {
            var result = new ServiceResult<bool>();

            try
            {
                // Check if already reset for new year
                var existing = await _context.LeaveBalances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeID == employeeId &&
                        x.Year == newYear);

                if (existing != null)
                {
                    result.Success = false;
                    result.Message = $"Đã có số dư phép cho năm {newYear} của nhân viên này";
                    result.Data = false;
                    return result;
                }

                // Get current year balance
                var currentYearBalance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeID == employeeId &&
                        x.Year == newYear - 1);

                // Calculate new annual quota
                decimal newAnnualQuota = 12; // Default 12 days per year

                if (currentYearBalance != null)
                {
                    var remainingDays = currentYearBalance.AnnualQuota - currentYearBalance.UsedDays;
                    newAnnualQuota = 12 + carryOverDays;

                    // Mark old balance as reset
                    currentYearBalance.IsReset = true;
                    currentYearBalance.UpdatedDate = DateTime.Now;
                }

                var newBalance = new LeaveBalances
                {
                    EmployeeID = employeeId,
                    Year = newYear,
                    AnnualQuota = newAnnualQuota,
                    UsedDays = 0,
                    UnpaidDays = 0,
                    ConsecutiveLeaveDays = 0,
                    LastLeaveEndDate = null,
                    IsReset = false,
                    UpdatedDate = DateTime.Now
                };

                _context.LeaveBalances.Add(newBalance);
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = $"Reset số dư phép cho năm {newYear} thành công. Số ngày phép năm: {newAnnualQuota}";
                result.Data = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Lỗi khi reset số dư phép";
                result.Errors.Add(ex.Message);
                result.Data = false;
            }

            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.LeaveBalances.FindAsync(id);
            if (entity == null) return false;

            _context.LeaveBalances.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsEmployeeInSameDepartment(int? managerEmployeeId, int targetEmployeeId)
        {
            if (!managerEmployeeId.HasValue) return false;

            var manager = await _context.Employees
                .FirstOrDefaultAsync(x => x.EmployeeID == managerEmployeeId.Value);

            var target = await _context.Employees
                .FirstOrDefaultAsync(x => x.EmployeeID == targetEmployeeId);

            if (manager == null || target == null) return false;

            return manager.DepartmentID == target.DepartmentID;
        }

        public async Task<EmployeeInfoDto?> GetEmployeeInfoAsync(int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

            if (employee == null) return null;

            return new EmployeeInfoDto
            {
                EmployeeID = employee.EmployeeID,
                FullName = employee.FullName,
                EmployeeCode = employee.EmployeeCode,
                DepartmentName = employee.Department?.DepartmentName ?? ""
            };
        }

        public async Task<List<LeaveBalanceDto>> GetByEmployeeWithInfoAsync(int employeeId)
        {
            var balances = await _context.LeaveBalances
                .Where(l => l.EmployeeID == employeeId)
                .OrderByDescending(l => l.Year)
                .ToListAsync();

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

            return balances.Select(b => new LeaveBalanceDto
            {
                LeaveBalanceID = b.LeaveBalanceID,
                EmployeeID = b.EmployeeID,
                EmployeeName = employee?.FullName ?? "",
                EmployeeCode = employee?.EmployeeCode ?? "",
                DepartmentName = employee?.Department?.DepartmentName ?? "",
                Year = b.Year,
                AnnualQuota = b.AnnualQuota,
                UsedDays = b.UsedDays,
                UnpaidDays = b.UnpaidDays,
                ConsecutiveLeaveDays = b.ConsecutiveLeaveDays,
                LastLeaveEndDate = b.LastLeaveEndDate,
                IsReset = b.IsReset,
                UpdatedDate = b.UpdatedDate
            }).ToList();
        }

        Task ILeaveBalanceService.GetEmployeeInfoAsync(int employeeID)
        {
            throw new NotImplementedException();
        }
    }
}