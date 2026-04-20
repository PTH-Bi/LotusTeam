using LotusTeam.DTOs;

public interface IWorkReportService
{
    Task<WorkReportDto> UploadReportAsync(UploadWorkReportDto dto);
    Task<List<WorkReportDto>> GetReportsByProjectAsync(int projectId);
}