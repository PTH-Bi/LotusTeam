using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.Service;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Services
{
    public class PayrollService : IPayrollService
    {
        private readonly AppDbContext _context;

        public PayrollService(AppDbContext context)
        {
            _context = context;
        }

        private DateTime NormalizePeriod(DateTime date)
            => new DateTime(date.Year, date.Month, 1);

        //====================================================
        // TÍNH LƯƠNG CHO 1 NHÂN VIÊN (ĐẦY ĐỦ)
        //====================================================
        private async Task<Payrolls> CalculateSinglePayrollAsync(Employees emp, DateTime period, short statusDraftId)
        {
            try
            {
                Console.WriteLine($"=== Bắt đầu tính lương cho nhân viên {emp.EmployeeID} - {emp.FullName} ===");

                //---------------------------------
                // 1. LƯƠNG CƠ BẢN
                //---------------------------------
                decimal baseSalary = emp.BaseSalary ?? 0;
                Console.WriteLine($"Lương cơ bản: {baseSalary:N0} VND");

                //---------------------------------
                // 2. PHỤ CẤP (từ bảng Allowances)
                //---------------------------------
                var allowances = await _context.Allowances
                    .Where(a => a.EmployeeID == emp.EmployeeID &&
                               a.Month.Year == period.Year &&
                               a.Month.Month == period.Month)
                    .ToListAsync();

                decimal allowanceTotal = allowances.Sum(a => a.Amount);
                Console.WriteLine($"Phụ cấp: {allowanceTotal:N0} VND ({allowances.Count} khoản)");

                //---------------------------------
                // 3. THƯỞNG (từ bảng Bonuses)
                //---------------------------------
                var bonuses = await _context.Bonuses
                    .Where(b => b.EmployeeID == emp.EmployeeID &&
                               b.Month.Year == period.Year &&
                               b.Month.Month == period.Month)
                    .ToListAsync();

                decimal bonusTotal = bonuses.Sum(b => b.Amount);
                Console.WriteLine($"Thưởng: {bonusTotal:N0} VND ({bonuses.Count} khoản)");

                //---------------------------------
                // 4. PHỤ CẤP THÂN NHÂN
                //---------------------------------
                var dependentAllowance = await _context.DependentAllowances
                    .FirstOrDefaultAsync(d => d.EmployeeID == emp.EmployeeID &&
                                              d.Month.Year == period.Year &&
                                              d.Month.Month == period.Month);

                decimal dependentAllowanceAmount = dependentAllowance?.TotalAmount ?? 0;

                if (dependentAllowance == null)
                {
                    dependentAllowance = await CalculateDependentAllowanceAsync(emp.EmployeeID, period, 500000);
                    dependentAllowanceAmount = dependentAllowance?.TotalAmount ?? 0;
                }

                Console.WriteLine($"Phụ cấp thân nhân: {dependentAllowanceAmount:N0} VND");

                // Gộp phụ cấp thân nhân vào tổng phụ cấp
                allowanceTotal += dependentAllowanceAmount;

                //---------------------------------
                // 5. CHẤM CÔNG
                //---------------------------------
                var attendances = await _context.Attendances
                    .Include(a => a.AttendanceOvertimes)
                        .ThenInclude(o => o.OvertimeRule)
                    .Where(a => a.EmployeeID == emp.EmployeeID &&
                               a.WorkDate.Month == period.Month &&
                               a.WorkDate.Year == period.Year)
                    .ToListAsync();

                Console.WriteLine($"Số ngày công: {attendances.Count}");

                decimal totalHours = attendances
                    .Where(a => a.WorkingHours != null)
                    .Sum(a => a.WorkingHours!.Value);

                decimal hourlyRate = baseSalary / 160m;
                decimal salaryByHour = totalHours * hourlyRate;
                Console.WriteLine($"Lương giờ công: {salaryByHour:N0} VND");

                //---------------------------------
                // 6. LƯƠNG TĂNG CA
                //---------------------------------
                decimal overtimeAmount = 0;
                foreach (var att in attendances)
                {
                    foreach (var ot in att.AttendanceOvertimes)
                    {
                        decimal hours = ot.OvertimeHours;
                        decimal rate = ot.OvertimeRule?.Rate ?? 1.5m;
                        overtimeAmount += hours * hourlyRate * rate;
                    }
                }
                Console.WriteLine($"Lương tăng ca: {overtimeAmount:N0} VND");

                //---------------------------------
                // 7. TỔNG THU NHẬP (GROSS)
                //---------------------------------
                decimal gross = baseSalary + allowanceTotal + bonusTotal + salaryByHour + overtimeAmount;
                Console.WriteLine($"Tổng thu nhập (Gross): {gross:N0} VND");

                //---------------------------------
                // 8. CÁC KHOẢN KHẤU TRỪ KHÁC
                //---------------------------------
                var otherDeductionsList = await _context.Deductions
                    .Where(d => d.EmployeeID == emp.EmployeeID &&
                               d.Month.Year == period.Year &&
                               d.Month.Month == period.Month)
                    .ToListAsync();

                decimal otherDeductions = otherDeductionsList.Sum(d => d.Amount);
                Console.WriteLine($"Khấu trừ khác: {otherDeductions:N0} VND");

                //---------------------------------
                // 9. BHXH (8% trên gross)
                //---------------------------------
                decimal bhxh = gross * 0.08m;
                Console.WriteLine($"BHXH (8%): {bhxh:N0} VND");

                //---------------------------------
                // 10. THUẾ TNCN (10% trên thu nhập sau BHXH)
                //---------------------------------
                decimal taxableIncome = gross - bhxh;
                decimal tax = taxableIncome * 0.1m;
                Console.WriteLine($"Thuế TNCN: {tax:N0} VND");

                //---------------------------------
                // 11. TỔNG KHẤU TRỪ
                //---------------------------------
                decimal totalDeductions = bhxh + tax + otherDeductions;

                //---------------------------------
                // 12. THỰC LĨNH (NET)
                //---------------------------------
                decimal net = gross - totalDeductions;
                Console.WriteLine($"Thực lĩnh (Net): {net:N0} VND");

                //---------------------------------
                // 13. TẠO BẢNG LƯƠNG
                //---------------------------------
                var payroll = new Payrolls
                {
                    EmployeeID = emp.EmployeeID,
                    PayPeriod = period,
                    GrossSalary = gross,
                    TotalDeductions = totalDeductions,
                    NetSalary = net,
                    StatusID = statusDraftId,
                    AllowanceTotal = allowanceTotal,
                    BonusTotal = bonusTotal,
                    OtherDeductions = otherDeductions
                };

                _context.Payrolls.Add(payroll);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Đã tạo bảng lương ID: {payroll.PayrollID}");

                //---------------------------------
                // 14. CẬP NHẬT PAYROLLID CHO CÁC BẢNG LIÊN QUAN
                //---------------------------------
                foreach (var allowance in allowances)
                {
                    allowance.PayrollID = payroll.PayrollID;
                }
                foreach (var bonus in bonuses)
                {
                    bonus.PayrollID = payroll.PayrollID;
                }
                foreach (var deduction in otherDeductionsList)
                {
                    deduction.PayrollID = payroll.PayrollID;
                }
                if (dependentAllowance != null)
                {
                    dependentAllowance.PayrollID = payroll.PayrollID;
                    _context.DependentAllowances.Update(dependentAllowance);
                }
                await _context.SaveChangesAsync();

                //---------------------------------
                // 15. LƯU CHI TIẾT BẢNG LƯƠNG
                //---------------------------------
                await SavePayrollDetailsAsync(payroll, baseSalary, salaryByHour, overtimeAmount,
                                              allowanceTotal, bonusTotal, bhxh, tax, otherDeductions);

                //---------------------------------
                // 16. LƯU SNAPSHOT THUẾ
                //---------------------------------
                var snapshot = new PayrollTaxSnapshot
                {
                    PayrollID = payroll.PayrollID,
                    TaxableIncome = taxableIncome,
                    TaxAmount = tax,
                    TaxRate = 10m,
                    CreatedDate = DateTime.Now
                };
                _context.PayrollTaxSnapshots.Add(snapshot);
                await _context.SaveChangesAsync();

                Console.WriteLine($"=== Hoàn thành tính lương cho nhân viên {emp.EmployeeID} ===");
                return payroll;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi: {ex.Message}");
                throw;
            }
        }

        //====================================================
        // LƯU CHI TIẾT BẢNG LƯƠNG
        //====================================================
        private async Task SavePayrollDetailsAsync(Payrolls payroll, decimal baseSalary, decimal salaryByHour,
                                                   decimal overtimeAmount, decimal allowanceTotal, decimal bonusTotal,
                                                   decimal bhxh, decimal tax, decimal otherDeductions)
        {
            var details = new List<PayrollDetails>();

            // Các khoản CỘNG
            if (baseSalary > 0)
            {
                details.Add(new PayrollDetails
                {
                    PayrollID = payroll.PayrollID,
                    ComponentID = 1,
                    ComponentName = "Lương cơ bản",
                    Amount = baseSalary,
                    IsAddition = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                });
            }

            if (allowanceTotal > 0)
            {
                details.Add(new PayrollDetails
                {
                    PayrollID = payroll.PayrollID,
                    ComponentID = 2,
                    ComponentName = "Phụ cấp",
                    Amount = allowanceTotal,
                    IsAddition = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                });
            }

            if (bonusTotal > 0)
            {
                details.Add(new PayrollDetails
                {
                    PayrollID = payroll.PayrollID,
                    ComponentID = 3,
                    ComponentName = "Thưởng",
                    Amount = bonusTotal,
                    IsAddition = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                });
            }

            if (salaryByHour > 0)
            {
                details.Add(new PayrollDetails
                {
                    PayrollID = payroll.PayrollID,
                    ComponentID = 4,
                    ComponentName = "Lương giờ công",
                    Amount = salaryByHour,
                    IsAddition = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                });
            }

            if (overtimeAmount > 0)
            {
                details.Add(new PayrollDetails
                {
                    PayrollID = payroll.PayrollID,
                    ComponentID = 5,
                    ComponentName = "Lương tăng ca",
                    Amount = overtimeAmount,
                    IsAddition = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                });
            }

            // Các khoản TRỪ
            if (bhxh > 0)
            {
                details.Add(new PayrollDetails
                {
                    PayrollID = payroll.PayrollID,
                    ComponentID = 6,
                    ComponentName = "Bảo hiểm xã hội",
                    Amount = bhxh,
                    IsAddition = false,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                });
            }

            if (tax > 0)
            {
                details.Add(new PayrollDetails
                {
                    PayrollID = payroll.PayrollID,
                    ComponentID = 7,
                    ComponentName = "Thuế TNCN",
                    Amount = tax,
                    IsAddition = false,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                });
            }

            if (otherDeductions > 0)
            {
                details.Add(new PayrollDetails
                {
                    PayrollID = payroll.PayrollID,
                    ComponentID = 8,
                    ComponentName = "Khấu trừ khác",
                    Amount = otherDeductions,
                    IsAddition = false,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                });
            }

            if (details.Any())
            {
                _context.PayrollDetails.AddRange(details);
                await _context.SaveChangesAsync();
            }
        }

        //====================================================
        // 1. TÍNH LƯƠNG THEO KỲ (TẤT CẢ NHÂN VIÊN)
        //====================================================
        public async Task<List<Payrolls>> CalculatePayrollAsync(DateTime payPeriod)
        {
            var period = NormalizePeriod(payPeriod);

            if (await _context.Payrolls.AnyAsync(p => p.PayPeriod == period))
                throw new Exception("Payroll already calculated for this period");

            var statusDraft = await _context.StatusMasters
                .FirstOrDefaultAsync(s => s.StatusCode == "DRAFT" && s.Module == "PAYROLL");

            if (statusDraft == null)
                throw new Exception("Status DRAFT not found for PAYROLL module");

            short statusDraftId = (short)statusDraft.StatusID;

            var employees = await _context.Employees
                .Where(e => e.Status == 1)
                .ToListAsync();

            var payrolls = new List<Payrolls>();

            foreach (var emp in employees)
            {
                try
                {
                    var payroll = await CalculateSinglePayrollAsync(emp, period, statusDraftId);
                    payrolls.Add(payroll);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating payroll for employee {emp.EmployeeID}: {ex.Message}");
                }
            }

            return payrolls;
        }

        //====================================================
        // 2. TÍNH LƯƠNG HÀNG LOẠT
        //====================================================
        public async Task<List<Payrolls>> CalculatePayrollBulkAsync(DateTime payPeriod, List<int> employeeIds)
        {
            var period = NormalizePeriod(payPeriod);

            var statusDraft = await _context.StatusMasters
                .FirstOrDefaultAsync(s => s.StatusCode == "DRAFT" && s.Module == "PAYROLL");

            if (statusDraft == null)
                throw new Exception("Status DRAFT not found for PAYROLL module");

            short statusDraftId = (short)statusDraft.StatusID;

            var employees = await _context.Employees
                .Where(e => employeeIds.Contains(e.EmployeeID))
                .ToListAsync();

            if (!employees.Any())
                throw new Exception($"Không tìm thấy nhân viên nào với IDs: {string.Join(", ", employeeIds)}");

            var payrolls = new List<Payrolls>();

            foreach (var emp in employees)
            {
                try
                {
                    var existingPayroll = await _context.Payrolls
                        .FirstOrDefaultAsync(p => p.EmployeeID == emp.EmployeeID && p.PayPeriod == period);

                    if (existingPayroll != null)
                    {
                        Console.WriteLine($"Payroll already exists for employee {emp.EmployeeID}");
                        payrolls.Add(existingPayroll);
                        continue;
                    }

                    var payroll = await CalculateSinglePayrollAsync(emp, period, statusDraftId);
                    payrolls.Add(payroll);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating payroll for employee {emp.EmployeeID}: {ex.Message}");
                    throw new Exception($"Lỗi tính lương cho nhân viên {emp.EmployeeID}: {ex.Message}");
                }
            }

            return payrolls;
        }

        //====================================================
        // 3. XEM BẢNG LƯƠNG THEO NHÂN VIÊN
        //====================================================
        public async Task<List<Payrolls>> GetPayrollByEmployeeAsync(int employeeId)
        {
            return await _context.Payrolls
                .Include(p => p.Status)
                .Where(p => p.EmployeeID == employeeId)
                .OrderByDescending(p => p.PayPeriod)
                .ToListAsync();
        }

        //====================================================
        // 4. XEM CHI TIẾT BẢNG LƯƠNG
        //====================================================
        public async Task<Payrolls?> GetPayrollDetailAsync(int payrollId)
        {
            return await _context.Payrolls
                .Include(p => p.Employee)
                .Include(p => p.PayrollDetails)
                .Include(p => p.Status)
                .FirstOrDefaultAsync(p => p.PayrollID == payrollId);
        }

        //====================================================
        // 5. LẤY TẤT CẢ BẢNG LƯƠNG (PHÂN TRANG)
        //====================================================
        public async Task<object> GetAllPayrollsAsync(int? month, int? year, int page, int pageSize)
        {
            try
            {
                var query = _context.Payrolls
                    .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                    .AsQueryable();

                if (month.HasValue && year.HasValue)
                {
                    query = query.Where(p => p.PayPeriod.Month == month.Value && p.PayPeriod.Year == year.Value);
                }
                else if (year.HasValue)
                {
                    query = query.Where(p => p.PayPeriod.Year == year.Value);
                }
                else if (month.HasValue)
                {
                    query = query.Where(p => p.PayPeriod.Month == month.Value);
                }

                var totalCount = await query.CountAsync();

                var payrolls = await query
                    .OrderByDescending(p => p.PayPeriod)
                    .ThenBy(p => p.Employee.FullName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.PayrollID,
                        p.EmployeeID,
                        EmployeeName = p.Employee.FullName,
                        DepartmentName = p.Employee.Department != null ? p.Employee.Department.DepartmentName : null,
                        p.PayPeriod,
                        p.GrossSalary,
                        p.TotalDeductions,
                        p.NetSalary,
                        p.AllowanceTotal,
                        p.BonusTotal,
                        p.OtherDeductions,
                        p.PaymentDate,
                        Status = p.StatusID == 1 ? "APPROVED" : "PENDING",
                        StatusID = p.StatusID
                    })
                    .ToListAsync();

                return new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    Data = payrolls
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách bảng lương: {ex.Message}", ex);
            }
        }

        //====================================================
        // 5b. XEM BẢNG LƯƠNG DẠNG FLAT (NHƯ TRONG ẢNH)
        //====================================================
        public async Task<object> GetPayrollFlatAsync(DateTime payPeriod)
        {
            var period = NormalizePeriod(payPeriod);

            var payrolls = await _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e.Department)
                .Where(p => p.PayPeriod == period)
                .OrderBy(p => p.Employee.FullName)
                .Select(p => new
                {
                    EmployeeID = p.EmployeeID,
                    NhanVien = p.Employee.FullName,
                    PhongBan = p.Employee.Department != null ? p.Employee.Department.DepartmentName : "",
                    LuongCoBan = p.Employee.BaseSalary ?? 0,
                    PhuCap = p.AllowanceTotal ?? 0,
                    Thuong = p.BonusTotal ?? 0,
                    KhauTru = p.OtherDeductions ?? 0,
                    ThucLinh = p.NetSalary,
                    TrangThai = p.StatusID == 1 ? "Đã duyệt" : "Nháp"
                })
                .ToListAsync();

            // Đánh số STT
            var result = payrolls.Select((item, index) => new
            {
                STT = index + 1,
                item.NhanVien,
                item.PhongBan,
                item.LuongCoBan,
                item.PhuCap,
                item.Thuong,
                item.KhauTru,
                item.ThucLinh,
                item.TrangThai
            });

            return new
            {
                PayPeriod = period.ToString("MM/yyyy"),
                TotalEmployees = payrolls.Count,
                Data = result
            };
        }

        //====================================================
        // 6. PHÊ DUYỆT BẢNG LƯƠNG
        //====================================================
        public async Task<bool> ApprovePayrollAsync(DateTime payPeriod)
        {
            var period = NormalizePeriod(payPeriod);

            var approvedStatus = await _context.StatusMasters
                .FirstOrDefaultAsync(s => s.StatusCode == "PROCESSED" && s.Module == "PAYROLL");

            if (approvedStatus == null)
                throw new Exception("Status PROCESSED not found for PAYROLL module");

            short approvedStatusId = (short)approvedStatus.StatusID;

            var payrolls = await _context.Payrolls
                .Where(p => p.PayPeriod == period)
                .ToListAsync();

            if (!payrolls.Any())
                return false;

            foreach (var payroll in payrolls)
            {
                payroll.StatusID = approvedStatusId;
                payroll.PaymentDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        //====================================================
        // 7. TẠO SNAPSHOT THUẾ
        //====================================================
        public async Task<PayrollTaxSnapshot> CreateTaxSnapshotAsync(int payrollId)
        {
            var payroll = await _context.Payrolls
                .FirstOrDefaultAsync(p => p.PayrollID == payrollId);

            if (payroll == null)
                throw new Exception($"Không tìm thấy bảng lương với ID {payrollId}");

            decimal taxableIncome = payroll.GrossSalary - payroll.TotalDeductions;
            decimal tax = taxableIncome * 0.1m;

            var snapshot = new PayrollTaxSnapshot
            {
                PayrollID = payrollId,
                TaxableIncome = taxableIncome,
                TaxAmount = tax,
                TaxRate = 10m,
                CreatedDate = DateTime.Now
            };

            _context.PayrollTaxSnapshots.Add(snapshot);
            await _context.SaveChangesAsync();

            return snapshot;
        }

        //====================================================
        // 8. LỊCH SỬ LƯƠNG NHÂN VIÊN
        //====================================================
        public async Task<List<Payrolls>> GetPayrollHistoryAsync(int employeeId)
        {
            return await _context.Payrolls
                .Where(p => p.EmployeeID == employeeId && p.PaymentDate != null)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        //====================================================
        // QUẢN LÝ PHỤ CẤP
        //====================================================
        public async Task<Allowances> AddAllowanceAsync(int employeeId, DateTime month, string allowanceName, decimal amount, string? note = null)
        {
            var allowance = new Allowances
            {
                EmployeeID = employeeId,
                Month = new DateTime(month.Year, month.Month, 1),
                AllowanceName = allowanceName,
                Amount = amount,
                Note = note,
                CreatedDate = DateTime.Now,
                IsDependentAllowance = false
            };

            _context.Allowances.Add(allowance);
            await _context.SaveChangesAsync();
            return allowance;
        }

        public async Task<List<Allowances>> GetAllowancesByMonthAsync(int employeeId, DateTime month)
        {
            return await _context.Allowances
                .Where(a => a.EmployeeID == employeeId &&
                           a.Month.Year == month.Year &&
                           a.Month.Month == month.Month)
                .ToListAsync();
        }

        //====================================================
        // QUẢN LÝ THƯỞNG
        //====================================================
        public async Task<Bonus> AddBonusAsync(int employeeId, DateTime month, string bonusName, decimal amount, string? reason = null)
        {
            var bonus = new Bonus
            {
                EmployeeID = employeeId,
                Month = new DateTime(month.Year, month.Month, 1),
                BonusName = bonusName,
                Amount = amount,
                Reason = reason,
                CreatedDate = DateTime.Now
            };

            _context.Bonuses.Add(bonus);
            await _context.SaveChangesAsync();
            return bonus;
        }

        public async Task<List<Bonus>> GetBonusesByMonthAsync(int employeeId, DateTime month)
        {
            return await _context.Bonuses
                .Where(b => b.EmployeeID == employeeId &&
                           b.Month.Year == month.Year &&
                           b.Month.Month == month.Month)
                .ToListAsync();
        }

        //====================================================
        // QUẢN LÝ KHẤU TRỪ
        //====================================================
        public async Task<Deductions> AddDeductionAsync(int employeeId, DateTime month, string deductionName, decimal amount, string? note = null)
        {
            var deduction = new Deductions
            {
                EmployeeID = employeeId,
                Month = new DateTime(month.Year, month.Month, 1),
                DeductionName = deductionName,
                Amount = amount,
                Note = note,
                CreatedDate = DateTime.Now
            };

            _context.Deductions.Add(deduction);
            await _context.SaveChangesAsync();
            return deduction;
        }

        public async Task<List<Deductions>> GetDeductionsByMonthAsync(int employeeId, DateTime month)
        {
            return await _context.Deductions
                .Where(d => d.EmployeeID == employeeId &&
                           d.Month.Year == month.Year &&
                           d.Month.Month == month.Month)
                .ToListAsync();
        }

        //====================================================
        // QUẢN LÝ NGƯỜI PHỤ THUỘC
        //====================================================
        public async Task<Dependent> AddDependentAsync(int employeeId, string fullName, string relationship, DateTime birthDate, string? identityNumber = null)
        {
            var dependent = new Dependent
            {
                EmployeeID = employeeId,
                FullName = fullName,
                Relationship = relationship,
                BirthDate = birthDate,
                IdentityNumber = identityNumber,
                IsActive = true,
                StartDate = DateTime.Now,
                CreatedDate = DateTime.Now
            };

            _context.Dependents.Add(dependent);
            await _context.SaveChangesAsync();
            return dependent;
        }

        public async Task<bool> DeactivateDependentAsync(int dependentId, DateTime? endDate = null)
        {
            var dependent = await _context.Dependents.FindAsync(dependentId);
            if (dependent == null) return false;

            dependent.IsActive = false;
            dependent.EndDate = endDate ?? DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Dependent>> GetDependentsByEmployeeAsync(int employeeId, bool onlyActive = true)
        {
            var query = _context.Dependents.Where(d => d.EmployeeID == employeeId);

            if (onlyActive)
                query = query.Where(d => d.IsActive);

            return await query.OrderBy(d => d.Relationship).ThenBy(d => d.FullName).ToListAsync();
        }

        public async Task<int> GetActiveDependentCountAsync(int employeeId, DateTime month)
        {
            var startOfMonth = new DateTime(month.Year, month.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var dependents = await _context.Dependents
                .Where(d => d.EmployeeID == employeeId && d.IsActive)
                .ToListAsync();

            return dependents.Count(d =>
                (!d.StartDate.HasValue || d.StartDate.Value <= endOfMonth) &&
                (!d.EndDate.HasValue || d.EndDate.Value >= startOfMonth));
        }

        //====================================================
        // PHỤ CẤP THÂN NHÂN
        //====================================================
        public async Task<DependentAllowances?> CalculateDependentAllowanceAsync(int employeeId, DateTime month, decimal amountPerDependent = 500000)
        {
            var dependentCount = await GetActiveDependentCountAsync(employeeId, month);

            if (dependentCount == 0)
                return null;

            var startOfMonth = new DateTime(month.Year, month.Month, 1);

            var existing = await _context.DependentAllowances
                .FirstOrDefaultAsync(d => d.EmployeeID == employeeId && d.Month == startOfMonth);

            if (existing != null)
            {
                existing.TotalDependents = dependentCount;
                existing.AmountPerDependent = amountPerDependent;
                existing.TotalAmount = dependentCount * amountPerDependent;
                existing.Note = $"Phụ cấp thân nhân cho {dependentCount} người phụ thuộc";
                existing.CreatedDate = DateTime.Now;
                _context.DependentAllowances.Update(existing);
                await _context.SaveChangesAsync();
                return existing;
            }

            var allowance = new DependentAllowances
            {
                EmployeeID = employeeId,
                Month = startOfMonth,
                TotalDependents = dependentCount,
                AmountPerDependent = amountPerDependent,
                TotalAmount = dependentCount * amountPerDependent,
                CreatedDate = DateTime.Now,
                Note = $"Phụ cấp thân nhân cho {dependentCount} người phụ thuộc"
            };

            _context.DependentAllowances.Add(allowance);
            await _context.SaveChangesAsync();

            return allowance;
        }

        public async Task<List<DependentAllowances>> CreateDependentAllowancesForAllAsync(DateTime month, decimal amountPerDependent = 500000)
        {
            var employees = await _context.Employees.Where(e => e.Status == 1).ToListAsync();
            var allowances = new List<DependentAllowances>();

            foreach (var emp in employees)
            {
                var allowance = await CalculateDependentAllowanceAsync(emp.EmployeeID, month, amountPerDependent);
                if (allowance != null)
                    allowances.Add(allowance);
            }

            return allowances;
        }

        public async Task<DependentAllowances?> GetDependentAllowanceByMonthAsync(int employeeId, DateTime month)
        {
            var startOfMonth = new DateTime(month.Year, month.Month, 1);
            return await _context.DependentAllowances
                .FirstOrDefaultAsync(d => d.EmployeeID == employeeId && d.Month == startOfMonth);
        }
    }
}