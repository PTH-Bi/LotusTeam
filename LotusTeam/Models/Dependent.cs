namespace LotusTeam.Models
{
    public class Dependent
    {
        public int DependentID { get; set; }
        public int EmployeeID { get; set; } // Nhân viên là người bảo lãnh
        public string FullName { get; set; } // Tên người phụ thuộc
        public string Relationship { get; set; } // Quan hệ (Con, Vợ/Chồng, Bố/Mẹ)
        public DateTime BirthDate { get; set; } // Ngày sinh
        public string? IdentityNumber { get; set; } // CMND/CCCD (nếu có)
        public bool IsActive { get; set; } = true; // Còn được hưởng phụ cấp không
        public DateTime? StartDate { get; set; } // Ngày bắt đầu phụ thuộc
        public DateTime? EndDate { get; set; } // Ngày kết thúc (nếu có)
        public DateTime CreatedDate { get; set; }

        // Navigation
        public virtual Employees Employee { get; set; }
    }
}
