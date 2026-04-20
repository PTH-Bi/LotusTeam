using LotusTeam.DTOs;
using LotusTeam.Models;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/performance")]
    public class PerformanceController : ControllerBase
    {
        private readonly IPerformanceService _service;

        public PerformanceController(IPerformanceService service)
        {
            _service = service;
        }

        // =====================================
        // CREATE REVIEW
        // =====================================

        [Authorize(Roles = "Manager,HR")]
        [HttpPost("review")]
        public async Task<IActionResult> CreateReview([FromBody] CreatePerformanceReviewDto dto)
        {
            var model = new PerformanceReview
            {
                EmployeeId = dto.EmployeeID,
                ReviewerId = dto.ReviewerId,
                Score = dto.Score,
                Comments = dto.Comments,
                ReviewPeriod = dto.ReviewPeriod
            };

            var result = await _service.CreatePerformanceReviewAsync(model);

            return Ok(new PerformanceReviewDto
            {
                ReviewId = result.ReviewId,
                EmployeeID = result.EmployeeId,
                ReviewDate = result.ReviewDate,
                ReviewerId = result.ReviewerId,
                Score = result.Score,
                Comments = result.Comments,
                ReviewPeriod = result.ReviewPeriod
            });
        }

        // =====================================
        // REVIEW HISTORY
        // =====================================

        [Authorize]
        [HttpGet("review-history/{employeeId:int}")]
        public async Task<IActionResult> ReviewHistory(int employeeId)
        {
            return Ok(await _service.GetReviewHistoryAsync(employeeId));
        }

        // =====================================
        // SKILLS
        // =====================================

        [Authorize]
        [HttpGet("skills/{employeeId:int}")]
        public async Task<IActionResult> GetSkills(int employeeId)
        {
            return Ok(await _service.GetEmployeeSkillsAsync(employeeId));
        }

        [Authorize(Roles = "HR,Manager")]
        [HttpPost("skills")]
        public async Task<IActionResult> UpdateSkill([FromBody] UpdateEmployeeSkillDto dto)
        {
            var skill = new EmployeeSkill
            {
                EmployeeID = dto.EmployeeID,
                SkillID = dto.SkillID,
                ProficiencyLevel = dto.ProficiencyLevel,
                VerifiedBy = dto.VerifiedBy,
                Certification = dto.Certification
            };

            await _service.UpdateEmployeeSkillAsync(skill);

            return Ok(new
            {
                message = "Cập nhật kỹ năng thành công"
            });
        }

        // =====================================
        // TRAINING RECOMMEND
        // =====================================

        [Authorize(Roles = "HR")]
        [HttpGet("training-recommend/{employeeId:int}")]
        public async Task<IActionResult> RecommendTraining(int employeeId)
        {
            return Ok(await _service.RecommendTrainingAsync(employeeId));
        }

        // =====================================
        // CAPABILITY
        // =====================================

        [Authorize]
        [HttpGet("capability/{employeeId:int}")]
        public async Task<IActionResult> Capability(int employeeId)
        {
            return Ok(await _service.GetEmployeeCapabilityAsync(employeeId));
        }
    }
}