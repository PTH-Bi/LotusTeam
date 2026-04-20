using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Interfaces;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LotusTeam.Services
{
    public class PayrollBankTransferService : IPayrollBankTransferService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PayrollBankTransferService> _logger;

        public PayrollBankTransferService(AppDbContext context, ILogger<PayrollBankTransferService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<PayrollBankTransferDto>> GetAllAsync()
        {
            var transfers = await _context.PayrollBankTransfers
                .Include(t => t.Payroll)
                .Include(t => t.CompanyBankAccount)
                    .ThenInclude(c => c.BankPartner)
                .Include(t => t.TransferDetails)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            return transfers.Select(t => new PayrollBankTransferDto
            {
                TransferID = t.TransferID,
                PayrollID = t.PayrollID,
                CompanyBankAccountID = t.CompanyBankAccountID,
                TransferDate = t.TransferDate,
                TotalAmount = t.TotalAmount,
                Status = t.Status,
                BankBatchCode = t.BankBatchCode,
                CreatedDate = t.CreatedDate,
                PayrollPeriod = t.Payroll?.PayPeriod.ToString("MM/yyyy"),
                CompanyBankName = t.CompanyBankAccount?.BankPartner?.BankName,
                EmployeeCount = t.TransferDetails?.Count ?? 0
            });
        }

        public async Task<PayrollBankTransferDto?> GetByIdAsync(int id)
        {
            var transfer = await _context.PayrollBankTransfers
                .Include(t => t.Payroll)
                .Include(t => t.CompanyBankAccount)
                    .ThenInclude(c => c.BankPartner)
                .Include(t => t.TransferDetails)
                .FirstOrDefaultAsync(t => t.TransferID == id);

            if (transfer == null) return null;

            return new PayrollBankTransferDto
            {
                TransferID = transfer.TransferID,
                PayrollID = transfer.PayrollID,
                CompanyBankAccountID = transfer.CompanyBankAccountID,
                TransferDate = transfer.TransferDate,
                TotalAmount = transfer.TotalAmount,
                Status = transfer.Status,
                BankBatchCode = transfer.BankBatchCode,
                CreatedDate = transfer.CreatedDate,
                PayrollPeriod = transfer.Payroll?.PayPeriod.ToString("MM/yyyy"),
                CompanyBankName = transfer.CompanyBankAccount?.BankPartner?.BankName,
                EmployeeCount = transfer.TransferDetails?.Count ?? 0
            };
        }

        public async Task<PayrollBankTransferDto> CreateAsync(CreatePayrollBankTransferDto createDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var payroll = await _context.Payrolls
                    .Include(p => p.Employee)
                    .FirstOrDefaultAsync(p => p.PayrollID == createDto.PayrollID);

                if (payroll == null)
                    throw new Exception($"Không tìm thấy payroll với ID {createDto.PayrollID}");

                var companyBank = await _context.CompanyBankAccounts
                    .Include(c => c.BankPartner)
                    .FirstOrDefaultAsync(c => c.CompanyBankAccountID == createDto.CompanyBankAccountID);

                if (companyBank == null)
                    throw new Exception($"Không tìm thấy tài khoản ngân hàng công ty với ID {createDto.CompanyBankAccountID}");

                var existingTransfer = await _context.PayrollBankTransfers
                    .FirstOrDefaultAsync(t => t.PayrollID == createDto.PayrollID && t.Status != "CANCELLED");

                if (existingTransfer != null)
                    throw new Exception($"Payroll này đã có transfer (ID: {existingTransfer.TransferID})");

                var transfer = new PayrollBankTransfers
                {
                    PayrollID = createDto.PayrollID,
                    CompanyBankAccountID = createDto.CompanyBankAccountID,
                    TransferDate = createDto.TransferDate ?? DateTime.Now,
                    TotalAmount = payroll.NetSalary,
                    Status = "DRAFT",
                    CreatedDate = DateTime.Now
                };

                _context.PayrollBankTransfers.Add(transfer);
                await _context.SaveChangesAsync();

                // Sử dụng BankAccountName từ model Employees
                var employee = payroll.Employee;
                var bankAccount = employee.BankAccount ?? "";
                var bankName = employee.BankPartner?.BankName ?? "";
                var accountHolderName = employee.BankAccountName ?? employee.FullName ?? "";

                var transferDetail = new PayrollTransferDetails
                {
                    TransferID = transfer.TransferID,
                    EmployeeID = payroll.EmployeeID,
                    BankAccount = bankAccount,
                    BankName = bankName,
                    AccountName = accountHolderName,
                    Amount = payroll.NetSalary,
                    Status = "PENDING"
                };

                _context.PayrollTransferDetails.Add(transferDetail);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                var result = await GetByIdAsync(transfer.TransferID);
                return result ?? throw new Exception("Không thể tạo transfer");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi tạo transfer cho payroll {PayrollID}", createDto.PayrollID);
                throw;
            }
        }

        public async Task<PayrollBankTransferDto?> UpdateAsync(int id, UpdatePayrollBankTransferDto updateDto)
        {
            var transfer = await _context.PayrollBankTransfers.FindAsync(id);
            if (transfer == null) return null;

            if (updateDto.TransferDate.HasValue)
                transfer.TransferDate = updateDto.TransferDate.Value;

            if (!string.IsNullOrEmpty(updateDto.Status))
                transfer.Status = updateDto.Status;

            if (!string.IsNullOrEmpty(updateDto.BankBatchCode))
                transfer.BankBatchCode = updateDto.BankBatchCode;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var transfer = await _context.PayrollBankTransfers
                .Include(t => t.TransferDetails)
                .FirstOrDefaultAsync(t => t.TransferID == id);

            if (transfer == null) return false;

            if (transfer.Status != "DRAFT" && transfer.Status != "PENDING")
                throw new Exception($"Không thể xóa transfer ở trạng thái {transfer.Status}");

            if (transfer.TransferDetails != null && transfer.TransferDetails.Any())
                _context.PayrollTransferDetails.RemoveRange(transfer.TransferDetails);

            _context.PayrollBankTransfers.Remove(transfer);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<PayrollBankTransferDto?> ProcessTransferAsync(int transferId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var transfer = await _context.PayrollBankTransfers
                    .Include(t => t.TransferDetails)
                    .Include(t => t.Payroll)
                    .FirstOrDefaultAsync(t => t.TransferID == transferId);

                if (transfer == null)
                    throw new Exception($"Không tìm thấy transfer ID {transferId}");

                if (transfer.Status != "DRAFT" && transfer.Status != "PENDING")
                    throw new Exception($"Transfer ở trạng thái {transfer.Status} không thể xử lý");

                foreach (var detail in transfer.TransferDetails ?? new List<PayrollTransferDetails>())
                {
                    if (string.IsNullOrEmpty(detail.BankAccount))
                        throw new Exception($"Nhân viên ID {detail.EmployeeID} chưa có thông tin tài khoản ngân hàng");
                }

                transfer.Status = "PROCESSING";
                transfer.TransferDate = DateTime.Now;
                transfer.BankBatchCode = $"BATCH_{DateTime.Now:yyyyMMddHHmmss}_{transfer.TransferID}";

                await _context.SaveChangesAsync();

                // TODO: Gọi API ngân hàng ở đây

                transfer.Status = "COMPLETED";

                if (transfer.Payroll != null)
                {
                    transfer.Payroll.PaymentDate = DateTime.Now;
                    transfer.Payroll.StatusID = 4;
                }

                foreach (var detail in transfer.TransferDetails ?? new List<PayrollTransferDetails>())
                {
                    detail.Status = "COMPLETED";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Transfer {TransferId} đã được xử lý thành công", transferId);

                return await GetByIdAsync(transferId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xử lý transfer {TransferId}", transferId);
                throw;
            }
        }

        public async Task<IEnumerable<PayrollBankTransferDto>> GetByPayrollIdAsync(int payrollId)
        {
            var transfers = await _context.PayrollBankTransfers
                .Where(t => t.PayrollID == payrollId)
                .ToListAsync();

            return transfers.Select(t => new PayrollBankTransferDto
            {
                TransferID = t.TransferID,
                PayrollID = t.PayrollID,
                CompanyBankAccountID = t.CompanyBankAccountID,
                TransferDate = t.TransferDate,
                TotalAmount = t.TotalAmount,
                Status = t.Status,
                BankBatchCode = t.BankBatchCode,
                CreatedDate = t.CreatedDate
            });
        }

        public async Task<PayrollBankTransferDetailDto?> GetTransferDetailAsync(int transferId)
        {
            var transfer = await _context.PayrollBankTransfers
                .Include(t => t.Payroll)
                    .ThenInclude(p => p.Employee)
                        .ThenInclude(e => e.BankPartner)
                .Include(t => t.CompanyBankAccount)
                    .ThenInclude(c => c.BankPartner)
                .Include(t => t.TransferDetails)
                    .ThenInclude(d => d.Employee)
                        .ThenInclude(e => e.BankPartner)
                .FirstOrDefaultAsync(t => t.TransferID == transferId);

            if (transfer == null) return null;

            return new PayrollBankTransferDetailDto
            {
                Transfer = new PayrollBankTransferDto
                {
                    TransferID = transfer.TransferID,
                    PayrollID = transfer.PayrollID,
                    CompanyBankAccountID = transfer.CompanyBankAccountID,
                    TransferDate = transfer.TransferDate,
                    TotalAmount = transfer.TotalAmount,
                    Status = transfer.Status,
                    BankBatchCode = transfer.BankBatchCode,
                    CreatedDate = transfer.CreatedDate,
                    EmployeeCount = transfer.TransferDetails?.Count ?? 0
                },
                CompanyBankAccount = transfer.CompanyBankAccount != null ? new CompanyBankAccountViewDto
                {
                    Id = transfer.CompanyBankAccount.CompanyBankAccountID,
                    AccountNumber = transfer.CompanyBankAccount.AccountNumber,
                    AccountName = transfer.CompanyBankAccount.AccountName,
                    Branch = transfer.CompanyBankAccount.Branch,
                    BankPartnerName = transfer.CompanyBankAccount.BankPartner?.BankName ?? ""
                } : null!,
                PayrollSummary = new PayrollSummaryDto
                {
                    PayrollID = transfer.PayrollID,
                    PayPeriod = transfer.Payroll?.PayPeriod ?? DateTime.MinValue,
                    EmployeeCount = transfer.TransferDetails?.Count ?? 0,
                    TotalNetSalary = transfer.TotalAmount
                },
                TransferDetails = transfer.TransferDetails?.Select(d => new TransferDetailItemDto
                {
                    TransferDetailID = d.TransferDetailID,
                    EmployeeID = d.EmployeeID,
                    EmployeeCode = d.Employee?.EmployeeCode ?? "",
                    EmployeeName = d.Employee?.FullName ?? "",
                    BankAccount = d.BankAccount,
                    BankName = d.BankName,
                    AccountHolderName = d.AccountName,
                    Amount = d.Amount,
                    Status = d.Status
                }) ?? new List<TransferDetailItemDto>()
            };
        }

        public async Task<BankTransferSummaryDto> GetTransferSummaryAsync(int transferId)
        {
            var transfer = await _context.PayrollBankTransfers
                .Include(t => t.TransferDetails)
                .FirstOrDefaultAsync(t => t.TransferID == transferId);

            if (transfer == null)
                throw new Exception($"Không tìm thấy transfer ID {transferId}");

            var details = transfer.TransferDetails ?? new List<PayrollTransferDetails>();

            return new BankTransferSummaryDto
            {
                TransferID = transferId,
                TotalEmployees = details.Count,
                SuccessCount = details.Count(d => d.Status == "COMPLETED"),
                PendingCount = details.Count(d => d.Status == "PENDING"),
                FailedCount = details.Count(d => d.Status == "FAILED"),
                TotalAmount = details.Sum(d => d.Amount),
                SuccessAmount = details.Where(d => d.Status == "COMPLETED").Sum(d => d.Amount),
                PendingAmount = details.Where(d => d.Status == "PENDING").Sum(d => d.Amount),
                FailedAmount = details.Where(d => d.Status == "FAILED").Sum(d => d.Amount)
            };
        }
    }
}