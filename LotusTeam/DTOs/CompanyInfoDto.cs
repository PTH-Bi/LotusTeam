namespace LotusTeam.DTOs
{
    public class CompanyInfoDto
    {
        public int CompanyID { get; set; }
        public string CompanyCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string? TaxCode { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? Representative { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCompanyInfoDto
    {
        public string CompanyCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string? TaxCode { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? Representative { get; set; }
    }

    public class UpdateCompanyInfoDto
    {
        public string CompanyCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string? TaxCode { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? Representative { get; set; }
        public bool IsActive { get; set; }
    }
}
