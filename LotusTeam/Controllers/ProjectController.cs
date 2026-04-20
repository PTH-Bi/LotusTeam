using LotusTeam.Models;
using LotusTeam.Service;
using LotusTeam.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/projects")]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _service;

        public ProjectController(IProjectService service)
        {
            _service = service;
        }

        // =========================================
        // GET ALL PROJECTS
        // =========================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var projects = await _service.GetAllProjectsAsync();

            var result = projects.Select(p => new ProjectDto
            {
                ProjectID = p.ProjectID,
                ProjectCode = p.ProjectCode,
                ProjectName = p.ProjectName,
                Description = p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                StatusID = p.StatusID,
                Budget = p.Budget,
                ManagerID = p.ManagerID
            });

            return Ok(result);
        }

        // =========================================
        // CREATE PROJECT
        // =========================================
        [Authorize(Roles = "Manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            var project = new Project
            {
                ProjectCode = dto.ProjectCode,
                ProjectName = dto.ProjectName,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                StatusID = dto.StatusID,
                Budget = dto.Budget,
                ManagerID = dto.ManagerID
            };

            var result = await _service.CreateProjectAsync(project);

            return Ok(new ProjectDto
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
            });
        }

        // =========================================
        // UPDATE STATUS
        // =========================================
        [Authorize(Roles = "Manager")]
        [HttpPost("{projectId:int}/status")]
        public async Task<IActionResult> UpdateStatus(int projectId, [FromQuery] short statusId)
        {
            await _service.UpdateProjectStatusAsync(projectId, statusId);
            return Ok("Cập nhật trạng thái thành công");
        }

        // =========================================
        // ASSIGN EMPLOYEE
        // =========================================
        [Authorize(Roles = "Manager")]
        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignProjectDto dto)
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

            return Ok(result);
        }

        // =========================================
        // PROJECT ASSIGNMENTS
        // =========================================
        [Authorize]
        [HttpGet("{projectId:int}/assignments")]
        public async Task<IActionResult> Assignments(int projectId)
        {
            return Ok(await _service.GetProjectAssignmentsAsync(projectId));
        }

        // =========================================
        // EMPLOYEE PROJECT HISTORY
        // =========================================
        [Authorize]
        [HttpGet("history/{employeeId:int}")]
        public async Task<IActionResult> History(int employeeId)
        {
            return Ok(await _service.GetEmployeeProjectHistoryAsync(employeeId));
        }
    }
}