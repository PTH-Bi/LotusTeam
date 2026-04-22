namespace LotusTeam.DTOs
{
    public class BankPartnerDto
    {
        public int BankPartnerID { get; set; }
        public string BankCode { get; set; } = "";
        public string BankName { get; set; } = "";
        public string? ShortName { get; set; }
        public string? SwiftCode { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateBankPartnerDto
    {
        public string BankCode { get; set; } = "";
        public string BankName { get; set; } = "";
        public string? ShortName { get; set; }
        public string? SwiftCode { get; set; }
    }

    public class UpdateBankPartnerDto
    {
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string? SwiftCode { get; set; }
        public string? ShortName { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
    }
}