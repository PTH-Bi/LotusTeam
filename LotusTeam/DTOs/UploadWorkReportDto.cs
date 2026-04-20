using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LotusTeam.DTOs
{
    public class UploadWorkReportDto
    {
        [Required]
        public int ProjectID { get; set; }

        [Required]
        public int EmployeeID { get; set; }

        public string? Description { get; set; }

        [Required]
        public IFormFile File { get; set; }
    }
}