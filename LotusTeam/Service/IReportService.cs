namespace LotusTeam.Service
{
    public interface IReportService
    {
        Task<object> EmployeeReportAsync();
        Task<object> AttendanceReportAsync();
        Task<object> PayrollReportAsync();
        Task<object> LeaveReportAsync();
    }

}
