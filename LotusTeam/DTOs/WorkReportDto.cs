namespace LotusTeam.DTOs
{
    public class WorkReportDto
    {
        public int ReportID { get; set; }
        public int ProjectID { get; set; }
        public int EmployeeID { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
    }

    public class UploadWorkReportDto
    {
        public int ProjectID { get; set; }
        public int EmployeeID { get; set; }
        public IFormFile File { get; set; } = null!;
        public string? Description { get; set; }
    }
}