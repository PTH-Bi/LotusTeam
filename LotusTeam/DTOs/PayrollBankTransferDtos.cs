namespace LotusTeam.DTOs
{
    public class PayrollBankTransferDto
    {
        public int TransferID { get; set; }
        public int PayrollID { get; set; }
        public int CompanyBankAccountID { get; set; }
        public DateTime? TransferDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "PENDING";
        public string? BankBatchCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? PayrollPeriod { get; set; }
        public string? CompanyBankName { get; set; }
        public int EmployeeCount { get; set; }
    }

    public class CreatePayrollBankTransferDto
    {
        public int PayrollID { get; set; }
        public int CompanyBankAccountID { get; set; }
        public DateTime? TransferDate { get; set; }
    }

    public class UpdatePayrollBankTransferDto
    {
        public DateTime? TransferDate { get; set; }
        public string? Status { get; set; }
        public string? BankBatchCode { get; set; }
    }

    public class PayrollBankTransferDetailDto
    {
        public PayrollBankTransferDto Transfer { get; set; } = null!;
        public CompanyBankAccountViewDto CompanyBankAccount { get; set; } = null!;
        public PayrollSummaryDto PayrollSummary { get; set; } = null!;
        public IEnumerable<TransferDetailItemDto> TransferDetails { get; set; } = new List<TransferDetailItemDto>();
    }

    public class TransferDetailItemDto
    {
        public int TransferDetailID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string EmployeeName { get; set; } = null!;
        public string BankAccount { get; set; } = null!;
        public string BankName { get; set; } = null!;
        public string AccountHolderName { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
    }

    public class BankTransferSummaryDto
    {
        public int TransferID { get; set; }
        public int TotalEmployees { get; set; }
        public int SuccessCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SuccessAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal FailedAmount { get; set; }
    }

    public class CompanyBankAccountViewDto
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = null!;
        public string AccountName { get; set; } = null!;
        public string? Branch { get; set; }
        public string BankPartnerName { get; set; } = null!;
    }

    public class PayrollSummaryDto
    {
        public int PayrollID { get; set; }
        public DateTime PayPeriod { get; set; }
        public int EmployeeCount { get; set; }
        public decimal TotalNetSalary { get; set; }
    }
}