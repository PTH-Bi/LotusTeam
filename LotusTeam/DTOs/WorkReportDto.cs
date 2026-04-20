public class WorkReportDto
{
    public int ReportID { get; set; }
    public int ProjectID { get; set; }
    public int EmployeeID { get; set; }
    public string FileName { get; set; }
    public string FileUrl { get; set; }
    public DateTime UploadDate { get; set; }
}