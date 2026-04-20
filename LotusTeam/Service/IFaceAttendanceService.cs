namespace LotusTeam.Service
{
    public interface IFaceAttendanceService
    {
        Task<(bool Success, string Message, double Confidence)> CheckIn(int employeeId, string imageBase64);

        Task<(bool Success, string Message, double Confidence)> CheckOut(int employeeId, string imageBase64);

        Task<object> GetHistory(int employeeId);
    }
}
