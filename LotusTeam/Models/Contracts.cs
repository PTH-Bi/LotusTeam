namespace LotusTeam.Models
{
    public class Contract
    {
        public int ContractID { get; set; }
        public int EmployeeID { get; set; }
        public string ContractCode { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int ContractTypeID { get; set; }
        public decimal Salary { get; set; }
        public DateTime SignedDate { get; set; }

        public Employees Employee { get; set; } = null!;
        public ContractType ContractType { get; set; } = null!;
    }

}
