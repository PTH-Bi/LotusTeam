using LotusTeam.Services;

namespace LotusTeam.Service
{
    public interface IRemoteAttendanceService
    {
        Task<bool> CanFaceScanAsync(int employeeId);
        Task<bool> RequestAsync(RemoteAttendanceRequestDto dto);
        Task<bool> ApproveAsync(int id, int approverId);
        Task<bool> RejectAsync(int id);
    }
}
