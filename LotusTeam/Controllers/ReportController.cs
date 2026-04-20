using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _service;

        public ReportController(IReportService service)
        {
            _service = service;
        }

        // ==================================================
        // 1. BÁO CÁO NHÂN SỰ
        // ==================================================
        /// <summary>
        /// Báo cáo tổng quan nhân sự
        /// </summary>
        [Authorize(Roles = "HR,Manager")]
        [HttpGet("employees")]
        public async Task<IActionResult> EmployeeReport()
        {
            return Ok(await _service.EmployeeReportAsync());
        }

        // ==================================================
        // 2. BÁO CÁO CHẤM CÔNG
        // ==================================================
        /// <summary>
        /// Báo cáo chấm công
        /// </summary>
        [Authorize(Roles = "HR,Manager")]
        [HttpGet("attendance")]
        public async Task<IActionResult> AttendanceReport()
        {
            return Ok(await _service.AttendanceReportAsync());
        }

        // ==================================================
        // 3. BÁO CÁO LƯƠNG
        // ==================================================
        /// <summary>
        /// Báo cáo lương
        /// </summary>
        [Authorize(Roles = "HR,Finance")]
        [HttpGet("payroll")]
        public async Task<IActionResult> PayrollReport()
        {
            return Ok(await _service.PayrollReportAsync());
        }

        // ==================================================
        // 4. BÁO CÁO NGHỈ PHÉP
        // ==================================================
        /// <summary>
        /// Báo cáo nghỉ phép
        /// </summary>
        [Authorize(Roles = "HR,Manager")]
        [HttpGet("leave")]
        public async Task<IActionResult> LeaveReport()
        {
            return Ok(await _service.LeaveReportAsync());
        }
    }
}
