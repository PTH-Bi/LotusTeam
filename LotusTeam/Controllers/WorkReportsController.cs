using LotusTeam.DTOs;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/work-reports")]
    public class WorkReportsController : ControllerBase
    {
        private readonly IWorkReportService _service;

        public WorkReportsController(IWorkReportService service)
        {
            _service = service;
        }

        // Upload report
        [Authorize]
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] UploadWorkReportDto dto)
        {
            var result = await _service.UploadReportAsync(dto);
            return Ok(result);
        }

        // Get reports by project
        [Authorize]
        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetReports(int projectId)
        {
            return Ok(await _service.GetReportsByProjectAsync(projectId));
        }
    }
}