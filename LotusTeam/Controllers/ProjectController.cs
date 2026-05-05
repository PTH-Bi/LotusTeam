using LotusTeam.Models;
using LotusTeam.Service;
using LotusTeam.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/projects")]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _service;
        private readonly ILogger<ProjectController> _logger;

        public ProjectController(IProjectService service, ILogger<ProjectController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tất cả dự án
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE,DIRECTOR")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var projects = await _service.GetAllProjectsAsync();

                var result = projects.Select(p => NewMethod(p));

                return Ok(new ApiResponse<IEnumerable<ProjectDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách dự án thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách dự án");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách dự án",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }

            static ProjectDto NewMethod(Project p)
            {
                return new ProjectDto
                {
                    ProjectID = p.ProjectID,
                    ProjectCode = p.ProjectCode,
                    ProjectName = p.ProjectName,
                    Description = p.Description,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    StatusID = p.StatusID,
                    Budget = p.Budget,
                    ManagerID = p.ManagerID,
                    ManagerName = p.Manager?.FullName
                };
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một dự án
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var projects = await _service.GetAllProjectsAsync();
                var project = projects.FirstOrDefault(p => p.ProjectID == id);

                if (project == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy dự án với ID {id}",
                        StatusCode = 404
                    });
                }

                var result = new ProjectDto
                {
                    ProjectID = project.ProjectID,
                    ProjectCode = project.ProjectCode,
                    ProjectName = project.ProjectName,
                    Description = project.Description,
                    StartDate = project.StartDate,
                    EndDate = project.EndDate,
                    StatusID = project.StatusID,
                    Budget = project.Budget,
                    ManagerID = project.ManagerID,
                    ManagerName = project.Manager?.FullName
                };

                return Ok(new ApiResponse<ProjectDto>
                {
                    Success = true,
                    Message = "Lấy thông tin dự án thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin dự án ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin dự án",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Tạo mới dự án
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,MANAGER")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
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

            try
            {
                var project = new Project
                {
                    ProjectCode = dto.ProjectCode,
                    ProjectName = dto.ProjectName,
                    Description = dto.Description,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    StatusID = dto.StatusID ?? 1,
                    Budget = dto.Budget,
                    ManagerID = dto.ManagerID
                };

                var result = await _service.CreateProjectAsync(project);

                var projectDto = new ProjectDto
                {
                    ProjectID = result.ProjectID,
                    ProjectCode = result.ProjectCode,
                    ProjectName = result.ProjectName,
                    Description = result.Description,
                    StartDate = result.StartDate,
                    EndDate = result.EndDate,
                    StatusID = result.StatusID,
                    Budget = result.Budget,
                    ManagerID = result.ManagerID
                };

                return Ok(new ApiResponse<ProjectDto>
                {
                    Success = true,
                    Message = "Tạo dự án thành công",
                    Data = projectDto,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo dự án");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo dự án",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái dự án
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,MANAGER")]
        [HttpPost("{projectId:int}/status")]
        public async Task<IActionResult> UpdateStatus(int projectId, [FromQuery] short statusId)
        {
            try
            {
                await _service.UpdateProjectStatusAsync(projectId, statusId);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Cập nhật trạng thái dự án thành công",
                    Data = new { ProjectId = projectId, NewStatusId = statusId },
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái dự án ID {ProjectId}", projectId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật trạng thái dự án",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Phân công nhân viên vào dự án
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,MANAGER")]
        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignProjectDto dto)
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

            try
            {
                var assignment = new ProjectAssignment
                {
                    EmployeeID = dto.EmployeeID,
                    ProjectID = dto.ProjectID,
                    Role = dto.Role,
                    AssignedDate = dto.AssignedDate,
                    ReleasedDate = dto.ReleasedDate,
                    ContributionDescription = dto.ContributionDescription
                };

                var result = await _service.AssignEmployeeAsync(assignment);

                return Ok(new ApiResponse<ProjectAssignment>
                {
                    Success = true,
                    Message = "Phân công nhân viên vào dự án thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân công nhân viên vào dự án");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi phân công nhân viên",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy danh sách nhân viên được phân công trong dự án
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER")]
        [HttpGet("{projectId:int}/assignments")]
        public async Task<IActionResult> GetAssignments(int projectId)
        {
            try
            {
                var assignments = await _service.GetProjectAssignmentsAsync(projectId);
                return Ok(new ApiResponse<List<ProjectAssignment>>
                {
                    Success = true,
                    Message = "Lấy danh sách phân công thành công",
                    Data = assignments,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách phân công dự án ID {ProjectId}", projectId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách phân công",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy lịch sử tham gia dự án của nhân viên
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("history/{employeeId:int}")]
        public async Task<IActionResult> GetEmployeeHistory(int employeeId)
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

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            try
            {
                var history = await _service.GetEmployeeProjectHistoryAsync(employeeId);
                return Ok(new ApiResponse<List<ProjectAssignment>>
                {
                    Success = true,
                    Message = "Lấy lịch sử tham gia dự án thành công",
                    Data = history,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch sử dự án của nhân viên ID {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy lịch sử dự án",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }
    }
}