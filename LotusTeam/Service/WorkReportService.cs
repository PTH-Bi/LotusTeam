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
        private readonly ILogger<WorkReportService> _logger;

        public WorkReportService(AppDbContext context, IWebHostEnvironment env, ILogger<WorkReportService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        public async Task<WorkReportDto> UploadReportAsync(UploadWorkReportDto dto)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "reports");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.File.FileName);
            var relativePath = Path.Combine("reports", fileName);
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
                FilePath = relativePath,
                Description = dto.Description,
                UploadDate = DateTime.UtcNow
            };

            _context.WorkReports.Add(report);
            await _context.SaveChangesAsync();

            return new WorkReportDto
            {
                ReportID = report.ReportID,
                ProjectID = report.ProjectID,
                EmployeeID = report.EmployeeID,
                FileName = report.FileName,
                FileUrl = "/api/work-reports/download/" + report.ReportID,
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
                    FileUrl = "/api/work-reports/download/" + r.ReportID,
                    UploadDate = r.UploadDate
                })
                .OrderByDescending(r => r.UploadDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<WorkReportDto>> GetReportsByEmployeeAsync(int employeeId)
        {
            return await _context.WorkReports
                .Where(r => r.EmployeeID == employeeId)
                .Select(r => new WorkReportDto
                {
                    ReportID = r.ReportID,
                    ProjectID = r.ProjectID,
                    EmployeeID = r.EmployeeID,
                    FileName = r.FileName,
                    FileUrl = "/api/work-reports/download/" + r.ReportID,
                    UploadDate = r.UploadDate
                })
                .OrderByDescending(r => r.UploadDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<WorkReport?> GetReportByIdAsync(int reportId)
        {
            return await _context.WorkReports
                .FirstOrDefaultAsync(r => r.ReportID == reportId);
        }

        public async Task<byte[]?> DownloadReportAsync(int reportId)
        {
            var report = await _context.WorkReports.FindAsync(reportId);
            if (report == null) return null;

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "reports");
            var filePath = Path.Combine(uploadsFolder, Path.GetFileName(report.FilePath));

            if (!File.Exists(filePath)) return null;

            return await File.ReadAllBytesAsync(filePath);
        }

        public async Task<bool> DeleteReportAsync(int reportId)
        {
            var report = await _context.WorkReports.FindAsync(reportId);
            if (report == null) return false;

            // Delete physical file
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "reports");
            var filePath = Path.Combine(uploadsFolder, Path.GetFileName(report.FilePath));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _context.WorkReports.Remove(report);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}