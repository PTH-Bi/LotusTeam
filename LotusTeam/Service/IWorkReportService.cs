using LotusTeam.DTOs;
using LotusTeam.Models;

namespace LotusTeam.Service
{
    public interface IWorkReportService
    {
        Task<WorkReportDto> UploadReportAsync(UploadWorkReportDto dto);
        Task<List<WorkReportDto>> GetReportsByProjectAsync(int projectId);
        Task<List<WorkReportDto>> GetReportsByEmployeeAsync(int employeeId);
        Task<WorkReport?> GetReportByIdAsync(int reportId);
        Task<byte[]?> DownloadReportAsync(int reportId);
        Task<bool> DeleteReportAsync(int reportId);
    }
}