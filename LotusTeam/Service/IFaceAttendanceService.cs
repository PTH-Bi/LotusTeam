using LotusTeam.Models;
using LotusTeam.Controllers;

namespace LotusTeam.Service
{
    /// <summary>
    /// Interface cho service chấm công bằng nhận diện khuôn mặt
    /// </summary>
    public interface IFaceAttendanceService
    {
        /// <summary>
        /// Chấm công check-in bằng nhận diện khuôn mặt
        /// </summary>
        /// <param name="employeeId">Mã nhân viên</param>
        /// <param name="imageBase64">Ảnh khuôn mặt dạng Base64</param>
        /// <returns>Kết quả chấm công (Success, Message, Confidence)</returns>
        Task<FaceCheckResult> CheckIn(int employeeId, string imageBase64);

        /// <summary>
        /// Chấm công check-out bằng nhận diện khuôn mặt
        /// </summary>
        /// <param name="employeeId">Mã nhân viên</param>
        /// <param name="imageBase64">Ảnh khuôn mặt dạng Base64</param>
        /// <returns>Kết quả chấm công (Success, Message, Confidence)</returns>
        Task<FaceCheckResult> CheckOut(int employeeId, string imageBase64);

        /// <summary>
        /// Lấy lịch sử chấm công khuôn mặt của nhân viên
        /// </summary>
        /// <param name="employeeId">Mã nhân viên</param>
        /// <param name="fromDate">Ngày bắt đầu (tùy chọn)</param>
        /// <param name="toDate">Ngày kết thúc (tùy chọn)</param>
        /// <returns>Danh sách lịch sử chấm công</returns>
        Task<IEnumerable<FaceAttendanceHistoryDto>> GetHistory(int employeeId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Đăng ký khuôn mặt cho nhân viên
        /// </summary>
        /// <param name="employeeId">Mã nhân viên</param>
        /// <param name="imageBase64">Ảnh khuôn mặt dạng Base64</param>
        /// <returns>Kết quả đăng ký</returns>
        Task<FaceRegisterResult> RegisterFace(int employeeId, string imageBase64);

        /// <summary>
        /// Lấy thông tin chấm công hôm nay của nhân viên
        /// </summary>
        /// <param name="employeeId">Mã nhân viên</param>
        /// <returns>Thông tin chấm công hôm nay</returns>
        Task<TodayAttendanceDto> GetTodayAttendance(int employeeId);

        /// <summary>
        /// Kiểm tra nhân viên có cùng phòng ban không
        /// </summary>
        /// <param name="managerEmployeeId">Mã nhân viên quản lý</param>
        /// <param name="targetEmployeeId">Mã nhân viên cần kiểm tra</param>
        /// <returns>True nếu cùng phòng ban</returns>
        Task<bool> IsEmployeeInSameDepartment(int? managerEmployeeId, int targetEmployeeId);

        /// <summary>
        /// Xóa dữ liệu khuôn mặt của nhân viên
        /// </summary>
        /// <param name="employeeId">Mã nhân viên</param>
        /// <returns>Kết quả xóa</returns>
        Task<FaceDeleteResult> DeleteFaceData(int employeeId);

        /// <summary>
        /// Cập nhật ảnh khuôn mặt cho nhân viên
        /// </summary>
        /// <param name="employeeId">Mã nhân viên</param>
        /// <param name="imageBase64">Ảnh khuôn mặt mới dạng Base64</param>
        /// <returns>Kết quả cập nhật</returns>
        Task<FaceUpdateResult> UpdateFaceData(int employeeId, string imageBase64);

        /// <summary>
        /// Lấy thống kê chấm công khuôn mặt theo tháng
        /// </summary>
        /// <param name="employeeId">Mã nhân viên</param>
        /// <param name="month">Tháng (1-12)</param>
        /// <param name="year">Năm</param>
        /// <returns>Thống kê chấm công</returns>
        Task<FaceAttendanceStatisticsDto> GetAttendanceStatistics(int employeeId, int month, int year);

        /// <summary>
        /// Kiểm tra trạng thái đăng ký khuôn mặt của nhân viên
        /// </summary>
        /// <param name="employeeId">Mã nhân viên</param>
        /// <returns>Trạng thái đăng ký</returns>
        Task<FaceRegistrationStatusDto> GetFaceRegistrationStatus(int employeeId);

        /// <summary>
        /// So sánh hai ảnh khuôn mặt
        /// </summary>
        /// <param name="imageBase64_1">Ảnh thứ nhất</param>
        /// <param name="imageBase64_2">Ảnh thứ hai</param>
        /// <returns>Độ tương đồng</returns>
        Task<double> CompareFaces(string imageBase64_1, string imageBase64_2);

        /// <summary>
        /// Lấy danh sách nhân viên chưa đăng ký khuôn mặt
        /// </summary>
        /// <param name="departmentId">Lọc theo phòng ban (tùy chọn)</param>
        /// <returns>Danh sách nhân viên</returns>
        Task<IEnumerable<UnregisteredFaceEmployeeDto>> GetUnregisteredEmployees(int? departmentId = null);
    }

    #region Result Classes

    /// <summary>
    /// Kết quả chấm công khuôn mặt
    /// </summary>
    public class FaceCheckResult
    {
        /// <summary>Thành công hay không</summary>
        public bool Success { get; set; }

        /// <summary>Thông báo</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Độ tin cậy nhận diện (0-100)</summary>
        public double Confidence { get; set; }

        /// <summary>Thời gian chấm công</summary>
        public DateTime CheckTime { get; set; }

        /// <summary>ID bản ghi chấm công</summary>
        public int AttendanceId { get; set; }
    }

    /// <summary>
    /// Kết quả đăng ký khuôn mặt
    /// </summary>
    public class FaceRegisterResult
    {
        /// <summary>Thành công hay không</summary>
        public bool Success { get; set; }

        /// <summary>Thông báo</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>ID bản ghi khuôn mặt</summary>
        public int FaceDataId { get; set; }

        /// <summary>Chất lượng ảnh (0-100)</summary>
        public double ImageQuality { get; set; }
    }

    /// <summary>
    /// Kết quả xóa dữ liệu khuôn mặt
    /// </summary>
    public class FaceDeleteResult
    {
        /// <summary>Thành công hay không</summary>
        public bool Success { get; set; }

        /// <summary>Thông báo</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Số bản ghi đã xóa</summary>
        public int DeletedCount { get; set; }
    }

    /// <summary>
    /// Kết quả cập nhật khuôn mặt
    /// </summary>
    public class FaceUpdateResult
    {
        /// <summary>Thành công hay không</summary>
        public bool Success { get; set; }

        /// <summary>Thông báo</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Thời gian cập nhật</summary>
        public DateTime UpdatedAt { get; set; }
    }

    #endregion

    #region DTO Classes

    /// <summary>
    /// Lịch sử chấm công khuôn mặt
    /// </summary>
    public class FaceAttendanceHistoryDto
    {
        /// <summary>ID bản ghi</summary>
        public int Id { get; set; }

        /// <summary>Mã nhân viên</summary>
        public int EmployeeId { get; set; }

        /// <summary>Tên nhân viên</summary>
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>Mã nhân viên (code)</summary>
        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>Tên phòng ban</summary>
        public string DepartmentName { get; set; } = string.Empty;

        /// <summary>Ngày chấm công</summary>
        public DateTime Date { get; set; }

        /// <summary>Thời gian check-in</summary>
        public DateTime? CheckInTime { get; set; }

        /// <summary>Thời gian check-out</summary>
        public DateTime? CheckOutTime { get; set; }

        /// <summary>Số giờ làm việc</summary>
        public double? WorkingHours { get; set; }

        /// <summary>Độ tin cậy check-in</summary>
        public double? CheckInConfidence { get; set; }

        /// <summary>Độ tin cậy check-out</summary>
        public double? CheckOutConfidence { get; set; }

        /// <summary>Số phút đi muộn</summary>
        public int? LateMinutes { get; set; }

        /// <summary>Số phút về sớm</summary>
        public int? EarlyLeaveMinutes { get; set; }

        /// <summary>Trạng thái</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Ghi chú</summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// Thông tin chấm công hôm nay
    /// </summary>
    public class TodayAttendanceDto
    {
        /// <summary>Đã check-in chưa</summary>
        public bool HasCheckedIn { get; set; }

        /// <summary>Thời gian check-in</summary>
        public DateTime? CheckInTime { get; set; }

        /// <summary>Độ tin cậy check-in</summary>
        public double? CheckInConfidence { get; set; }

        /// <summary>Đã check-out chưa</summary>
        public bool HasCheckedOut { get; set; }

        /// <summary>Thời gian check-out</summary>
        public DateTime? CheckOutTime { get; set; }

        /// <summary>Độ tin cậy check-out</summary>
        public double? CheckOutConfidence { get; set; }

        /// <summary>Số giờ làm việc</summary>
        public double? WorkingHours { get; set; }

        /// <summary>Số phút đi muộn</summary>
        public int? LateMinutes { get; set; }

        /// <summary>Trạng thái</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Ghi chú</summary>
        public string? Note { get; set; }

        /// <summary>Có thể check-out chưa</summary>
        public bool CanCheckOut { get; set; }
    }

    /// <summary>
    /// Thống kê chấm công khuôn mặt
    /// </summary>
    public class FaceAttendanceStatisticsDto
    {
        /// <summary>Tháng</summary>
        public int Month { get; set; }

        /// <summary>Năm</summary>
        public int Year { get; set; }

        /// <summary>Tổng số ngày làm việc</summary>
        public int TotalWorkingDays { get; set; }

        /// <summary>Số ngày đi làm</summary>
        public int DaysPresent { get; set; }

        /// <summary>Số ngày vắng mặt</summary>
        public int DaysAbsent { get; set; }

        /// <summary>Số ngày đi muộn</summary>
        public int DaysLate { get; set; }

        /// <summary>Số ngày về sớm</summary>
        public int DaysEarlyLeave { get; set; }

        /// <summary>Tổng số giờ làm việc</summary>
        public double TotalWorkingHours { get; set; }

        /// <summary>Số giờ làm thêm</summary>
        public double OvertimeHours { get; set; }

        /// <summary>Tỷ lệ chuyên cần (%)</summary>
        public double AttendanceRate { get; set; }

        /// <summary>Chi tiết theo ngày</summary>
        public List<DailyAttendanceStatDto> DailyDetails { get; set; } = new();
    }

    /// <summary>
    /// Thống kê chấm công theo ngày
    /// </summary>
    public class DailyAttendanceStatDto
    {
        /// <summary>Ngày</summary>
        public DateTime Date { get; set; }

        /// <summary>Thứ trong tuần</summary>
        public string DayOfWeek { get; set; } = string.Empty;

        /// <summary>Thời gian check-in</summary>
        public DateTime? CheckInTime { get; set; }

        /// <summary>Thời gian check-out</summary>
        public DateTime? CheckOutTime { get; set; }

        /// <summary>Số giờ làm việc</summary>
        public double? WorkingHours { get; set; }

        /// <summary>Trạng thái</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Màu sắc hiển thị</summary>
        public string StatusColor { get; set; } = string.Empty;
    }

    /// <summary>
    /// Trạng thái đăng ký khuôn mặt
    /// </summary>
    public class FaceRegistrationStatusDto
    {
        /// <summary>Đã đăng ký chưa</summary>
        public bool IsRegistered { get; set; }

        /// <summary>Mã nhân viên</summary>
        public int EmployeeId { get; set; }

        /// <summary>Tên nhân viên</summary>
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>Thời gian đăng ký</summary>
        public DateTime? RegisteredAt { get; set; }

        /// <summary>Số lượng ảnh đã đăng ký</summary>
        public int ImageCount { get; set; }

        /// <summary>Chất lượng ảnh trung bình</summary>
        public double AverageImageQuality { get; set; }

        /// <summary>Lần cập nhật cuối</summary>
        public DateTime? LastUpdatedAt { get; set; }
    }

    /// <summary>
    /// Nhân viên chưa đăng ký khuôn mặt
    /// </summary>
    public class UnregisteredFaceEmployeeDto
    {
        /// <summary>Mã nhân viên</summary>
        public int EmployeeId { get; set; }

        /// <summary>Mã nhân viên (code)</summary>
        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>Tên nhân viên</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Tên phòng ban</summary>
        public string DepartmentName { get; set; } = string.Empty;

        /// <summary>Email</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Ngày vào làm</summary>
        public DateTime? HireDate { get; set; }
    }

    #endregion
}