using System.ComponentModel.DataAnnotations;

namespace LotusTeam.DTOs
{
    /// <summary>
    /// DTO số dư ngày phép
    /// </summary>
    public class LeaveBalanceDto
    {
        /// <summary>Mã bản ghi số dư phép (LeaveBalanceID)</summary>
        public int LeaveBalanceID { get; set; }

        /// <summary>Mã nhân viên</summary>
        public int EmployeeID { get; set; }

        /// <summary>Tên nhân viên</summary>
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>Mã nhân viên (code)</summary>
        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>Tên phòng ban</summary>
        public string DepartmentName { get; set; } = string.Empty;

        /// <summary>Năm áp dụng</summary>
        public int Year { get; set; }

        /// <summary>Số ngày phép năm (AnnualQuota)</summary>
        public decimal AnnualQuota { get; set; }

        /// <summary>Số ngày đã sử dụng</summary>
        public decimal UsedDays { get; set; }

        /// <summary>Số ngày phép còn lại</summary>
        public decimal RemainingDays => AnnualQuota - UsedDays;

        /// <summary>Số ngày nghỉ không lương</summary>
        public decimal UnpaidDays { get; set; }

        /// <summary>Số ngày nghỉ liên tiếp</summary>
        public int ConsecutiveLeaveDays { get; set; }

        /// <summary>Ngày kết thúc kỳ nghỉ gần nhất</summary>
        public DateTime? LastLeaveEndDate { get; set; }

        /// <summary>Đã reset sang năm mới chưa</summary>
        public bool IsReset { get; set; }

        /// <summary>Ngày cập nhật</summary>
        public DateTime UpdatedDate { get; set; }
    }

    /// <summary>
    /// DTO tạo/cập nhật số dư ngày phép
    /// </summary>
    public class CreateLeaveBalanceDto
    {
        /// <summary>Mã nhân viên (bắt buộc)</summary>
        [Required(ErrorMessage = "Mã nhân viên là bắt buộc")]
        public int EmployeeID { get; set; }

        /// <summary>Năm áp dụng (bắt buộc)</summary>
        [Required(ErrorMessage = "Năm là bắt buộc")]
        [Range(2000, 2100, ErrorMessage = "Năm phải từ 2000 đến 2100")]
        public int Year { get; set; }

        /// <summary>Số ngày phép năm (mặc định 12)</summary>
        [Range(0, 30, ErrorMessage = "Số ngày phép năm từ 0 đến 30")]
        public decimal AnnualQuota { get; set; } = 12;

        /// <summary>Số ngày đã sử dụng</summary>
        [Range(0, 365, ErrorMessage = "Số ngày đã sử dụng không hợp lệ")]
        public decimal UsedDays { get; set; } = 0;

        /// <summary>Số ngày nghỉ không lương</summary>
        [Range(0, 365, ErrorMessage = "Số ngày nghỉ không lương không hợp lệ")]
        public decimal UnpaidDays { get; set; } = 0;

        /// <summary>Số ngày nghỉ liên tiếp</summary>
        [Range(0, 30, ErrorMessage = "Số ngày nghỉ liên tiếp không hợp lệ")]
        public int ConsecutiveLeaveDays { get; set; } = 0;

        /// <summary>Ngày kết thúc kỳ nghỉ gần nhất</summary>
        public DateTime? LastLeaveEndDate { get; set; }

        /// <summary>Đã reset sang năm mới chưa</summary>
        public bool IsReset { get; set; } = false;
    }

    /// <summary>
    /// DTO cập nhật số ngày đã sử dụng
    /// </summary>
    public class UpdateUsedDaysDto
    {
        /// <summary>Năm áp dụng</summary>
        [Required(ErrorMessage = "Năm là bắt buộc")]
        public int Year { get; set; }

        /// <summary>Số ngày cần cộng thêm (có thể âm nếu hủy đơn)</summary>
        [Required(ErrorMessage = "Số ngày cần cập nhật là bắt buộc")]
        public decimal DaysToAdd { get; set; }
    }

    /// <summary>
    /// DTO reset số dư phép cho năm mới
    /// </summary>
    public class ResetLeaveBalanceDto
    {
        /// <summary>Mã nhân viên</summary>
        [Required(ErrorMessage = "Mã nhân viên là bắt buộc")]
        public int EmployeeId { get; set; }

        /// <summary>Năm mới</summary>
        [Required(ErrorMessage = "Năm mới là bắt buộc")]
        [Range(2000, 2100, ErrorMessage = "Năm phải từ 2000 đến 2100")]
        public int NewYear { get; set; }

        /// <summary>Số ngày được chuyển sang năm mới (carry-over)</summary>
        [Range(0, 30, ErrorMessage = "Số ngày carry-over từ 0 đến 30")]
        public decimal CarryOverDays { get; set; } = 0;
    }
}