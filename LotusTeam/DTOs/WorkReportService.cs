using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Service
{
    public class WorkReportService : IWorkReportService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public WorkReportService(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<WorkReportDto> UploadReportAsync(UploadWorkReportDto dto)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "reports");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(dto.File.FileName);

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            var report = new WorkReport
            {
                EmployeeID = dto.EmployeeID,
                ProjectID = dto.ProjectID,
                FileName = dto.File.FileName,
                FilePath = "/reports/" + fileName,
                Description = dto.Description
            };

            _context.WorkReports.Add(report);
            await _context.SaveChangesAsync();

            return new WorkReportDto
            {
                ReportID = report.ReportID,
                ProjectID = report.ProjectID,
                EmployeeID = report.EmployeeID,
                FileName = report.FileName,
                FileUrl = report.FilePath,
                UploadDate = report.UploadDate
            };
        }

        public async Task<List<WorkReportDto>> GetReportsByProjectAsync(int projectId)
        {
            return await _context.WorkReports
                .Where(r => r.ProjectID == projectId)
                .Select(r => new WorkReportDto
                {
                    ReportID = r.ReportID,
                    ProjectID = r.ProjectID,
                    EmployeeID = r.EmployeeID,
                    FileName = r.FileName,
                    FileUrl = r.FilePath,
                    UploadDate = r.UploadDate
                }).ToListAsync();
        }
    }
}