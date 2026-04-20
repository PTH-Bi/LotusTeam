using System.Diagnostics.Contracts;

namespace LotusTeam.Models
{
    public class ContractType
    {
        public int ContractTypeID { get; set; }
        public string ContractTypeCode { get; set; } = null!;
        public string ContractTypeName { get; set; } = null!;
        public bool IsIndefinite { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        public ICollection<Contract>? Contracts { get; set; }
    }

}
