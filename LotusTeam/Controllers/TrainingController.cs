using LotusTeam.DTOs;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/training")]
    [Authorize]
    public class TrainingController : ControllerBase
    {
        private readonly ITrainingService _service;

        public TrainingController(ITrainingService service)
        {
            _service = service;
        }

        // ======================
        // COURSE LIST
        // ======================

        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses()
        {
            var result = await _service.GetAllCoursesAsync();
            return Ok(result);
        }

        // ======================
        // CREATE COURSE
        // ======================

        [Authorize(Roles = "HR")]
        [HttpPost("courses")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
        {
            var result = await _service.CreateCourseAsync(dto);
            return Ok(result);
        }

        // ======================
        // ENROLL EMPLOYEE
        // ======================

        [Authorize(Roles = "HR")]
        [HttpPost("enroll")]
        public async Task<IActionResult> EnrollEmployee([FromBody] EnrollEmployeeDto dto)
        {
            await _service.EnrollEmployeeAsync(dto);
            return Ok("Employee enrolled successfully");
        }

        // ======================
        // EMPLOYEE COURSES
        // ======================

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetEmployeeCourses(int employeeId)
        {
            var result = await _service.GetEmployeeCoursesAsync(employeeId);
            return Ok(result);
        }

        // ======================
        // UPDATE RESULT
        // ======================

        [Authorize(Roles = "HR")]
        [HttpPut("result/{enrollmentId}")]
        public async Task<IActionResult> UpdateResult(
            int enrollmentId,
            short statusId,
            DateOnly? completionDate,
            string? certificatePath)
        {
            await _service.UpdateTrainingResultAsync(
                enrollmentId,
                statusId,
                completionDate,
                certificatePath);

            return Ok("Training result updated");
        }

        // ======================
        // CERTIFICATES
        // ======================

        [HttpGet("certificates/{employeeId}")]
        public async Task<IActionResult> GetCertificates(int employeeId)
        {
            var result = await _service.GetEmployeeCertificatesAsync(employeeId);
            return Ok(result);
        }
    }
}