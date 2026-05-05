using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

namespace LotusTeam.Models
{
    public class Employees
    {
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public byte? GenderID { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int? DepartmentID { get; set; }
        public int? PositionID { get; set; }
        public DateTime HireDate { get; set; }
        public decimal? BaseSalary { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public short Status { get; set; }
        public string? AvatarPath { get; set; } // Thêm dòng này
        public DateTime CreatedDate { get; set; }
        public string? MaritalStatus { get; set; }
        public string? IdentityNumber { get; set; }
        public string? BankAccount { get; set; }
        public string? TaxCode { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public int? BankPartnerID { get; set; }
        [StringLength(200)]
        public string? BankAccountName { get; set; }

        public Department? Department { get; set; }
        public Position? Position { get; set; }
        public Gender? Gender { get; set; }
        public BankPartner? BankPartner { get; set; }

        public ICollection<Contract>? Contracts { get; set; }
        public ICollection<LeaveRequest>? LeaveRequests { get; set; }
        public ICollection<Attendances>? Attendances { get; set; }
        public ICollection<FaceAttendances>? FaceAttendances { get; set; }
    }

}
