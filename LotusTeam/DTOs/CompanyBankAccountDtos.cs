namespace LotusTeam.DTOs
{
    public class CompanyBankAccountDto
    {
        public int CompanyBankAccountID { get; set; }
        public int CompanyID { get; set; }
        public int BankPartnerID { get; set; }

        public string AccountNumber { get; set; } = "";
        public string AccountName { get; set; } = "";

        public string? Branch { get; set; }

        public bool IsDefault { get; set; }
    }

    public class CreateCompanyBankAccountDto
    {
        public int CompanyID { get; set; }
        public int BankPartnerID { get; set; }

        public string AccountNumber { get; set; } = "";
        public string AccountName { get; set; } = "";

        public string? Branch { get; set; }
    }
}