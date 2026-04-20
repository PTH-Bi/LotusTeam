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
}