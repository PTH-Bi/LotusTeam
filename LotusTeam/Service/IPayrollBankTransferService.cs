// Interfaces/IPayrollBankTransferService.cs
using LotusTeam.DTOs;

namespace LotusTeam.Interfaces
{
    public interface IPayrollBankTransferService
    {
        Task<IEnumerable<PayrollBankTransferDto>> GetAllAsync();
        Task<PayrollBankTransferDto?> GetByIdAsync(int id);
        Task<PayrollBankTransferDto> CreateAsync(CreatePayrollBankTransferDto createDto);
        Task<PayrollBankTransferDto?> UpdateAsync(int id, UpdatePayrollBankTransferDto updateDto);
        Task<bool> DeleteAsync(int id);
        Task<PayrollBankTransferDto?> ProcessTransferAsync(int transferId);
        Task<IEnumerable<PayrollBankTransferDto>> GetByPayrollIdAsync(int payrollId);
        Task<PayrollBankTransferDetailDto?> GetTransferDetailAsync(int transferId);
        Task<BankTransferSummaryDto> GetTransferSummaryAsync(int transferId);
    }
}