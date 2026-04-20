using LotusTeam.DTOs;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/surveys")]
    public class SurveyController : ControllerBase
    {
        private readonly ISurveyService _service;

        public SurveyController(ISurveyService service)
        {
            _service = service;
        }

        // =========================
        // CREATE SURVEY
        // =========================
        [Authorize(Roles = "HR,Manager")]
        [HttpPost]
        public async Task<IActionResult> CreateSurvey([FromBody] CreateSurveyDto dto)
        {
            await _service.CreateSurveyAsync(dto);
            return Ok(new { message = "Tạo khảo sát thành công" });
        }

        // =========================
        // SUBMIT RESPONSE
        // =========================
        [Authorize]
        [HttpPost("response")]
        public async Task<IActionResult> SubmitResponse([FromBody] SubmitSurveyResponseDto dto)
        {
            await _service.SubmitResponseAsync(dto);
            return Ok(new { message = "Gửi phản hồi thành công" });
        }

        // =========================
        // RESULTS
        // =========================
        [Authorize(Roles = "HR,Manager")]
        [HttpGet("{surveyId:int}/results")]
        public async Task<IActionResult> Results(int surveyId)
        {
            return Ok(await _service.GetResultsAsync(surveyId));
        }
    }
}