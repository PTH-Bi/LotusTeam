using LotusTeam.DTOs;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/work-reports")]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class WorkReportsController : ControllerBase
    {
        private readonly IWorkReportService _service;
        private readonly ILogger<WorkReportsController> _logger;

        public WorkReportsController(IWorkReportService service, ILogger<WorkReportsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Upload báo cáo công việc (Nhân viên, Manager, Team Leader, HR)
        /// </summary>
        /// <param name="dto">Thông tin file báo cáo</param>
        /// <returns>Kết quả upload</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] UploadWorkReportDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    StatusCode = 400
                });
            }

            // Kiểm tra file
            if (dto.File == null || dto.File.Length == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Vui lòng chọn file để upload",
                    StatusCode = 400
                });
            }

            // Kiểm tra kích thước file (max 10MB)
            if (dto.File.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Kích thước file không được vượt quá 10MB",
                    StatusCode = 400
                });
            }

            // Kiểm tra extension file
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip", ".rar", ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(dto.File.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Định dạng file không được hỗ trợ",
                    StatusCode = 400
                });
            }

            try
            {
                // Kiểm tra quyền: Employee chỉ upload báo cáo cho chính mình
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

                if (!allowedRoles.Contains(currentUserRole) && currentUserId != dto.EmployeeID)
                {
                    return Forbid();
                }

                var result = await _service.UploadReportAsync(dto);
                return Ok(new ApiResponse<WorkReportDto>
                {
                    Success = true,
                    Message = "Upload báo cáo thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload báo cáo cho nhân viên {EmployeeId}", dto.EmployeeID);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi upload báo cáo",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy danh sách báo cáo theo dự án
        /// </summary>
        /// <param name="projectId">ID dự án</param>
        /// <returns>Danh sách báo cáo</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER")]
        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetReportsByProject(int projectId)
        {
            if (projectId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "ProjectId không hợp lệ",
                    StatusCode = 400
                });
            }

            try
            {
                var result = await _service.GetReportsByProjectAsync(projectId);
                return Ok(new ApiResponse<List<WorkReportDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách báo cáo thành công",
                    Data = result,
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = result.Count,
                        Page = 1,
                        PageSize = result.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách báo cáo cho dự án {ProjectId}", projectId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách báo cáo",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy danh sách báo cáo theo nhân viên
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Danh sách báo cáo của nhân viên</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetReportsByEmployee(int employeeId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "EmployeeId không hợp lệ",
                    StatusCode = 400
                });
            }

            // Kiểm tra quyền: nhân viên chỉ xem được báo cáo của mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            try
            {
                var result = await _service.GetReportsByEmployeeAsync(employeeId);
                return Ok(new ApiResponse<List<WorkReportDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách báo cáo của nhân viên thành công",
                    Data = result,
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = result.Count,
                        Page = 1,
                        PageSize = result.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách báo cáo của nhân viên {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách báo cáo",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Tải xuống file báo cáo
        /// </summary>
        /// <param name="reportId">ID báo cáo</param>
        /// <returns>File báo cáo</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("download/{reportId}")]
        public async Task<IActionResult> DownloadReport(int reportId)
        {
            if (reportId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "ReportId không hợp lệ",
                    StatusCode = 400
                });
            }

            try
            {
                var report = await _service.GetReportByIdAsync(reportId);
                if (report == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy báo cáo với ID {reportId}",
                        StatusCode = 404
                    });
                }

                // Kiểm tra quyền tải file
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

                if (!allowedRoles.Contains(currentUserRole) && currentUserId != report.EmployeeID)
                {
                    return Forbid();
                }

                var fileBytes = await _service.DownloadReportAsync(reportId);
                if (fileBytes == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không tìm thấy file báo cáo",
                        StatusCode = 404
                    });
                }

                return File(fileBytes, "application/octet-stream", report.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải file báo cáo ID {ReportId}", reportId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tải file báo cáo",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Xóa báo cáo (Chỉ Manager và HR)
        /// </summary>
        /// <param name="reportId">ID báo cáo</param>
        /// <returns>Kết quả xóa</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        [HttpDelete("{reportId}")]
        public async Task<IActionResult> DeleteReport(int reportId)
        {
            if (reportId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "ReportId không hợp lệ",
                    StatusCode = 400
                });
            }

            try
            {
                var result = await _service.DeleteReportAsync(reportId);
                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy báo cáo với ID {reportId}",
                        StatusCode = 404
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa báo cáo thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa báo cáo ID {ReportId}", reportId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa báo cáo",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }
    }
}