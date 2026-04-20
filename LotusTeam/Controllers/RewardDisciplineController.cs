using LotusTeam.DTOs;
using LotusTeam.Service;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/reward-discipline")]
    public class RewardDisciplineController : ControllerBase
    {
        private readonly IRewardDisciplineService _service;

        public RewardDisciplineController(IRewardDisciplineService service)
        {
            _service = service;
        }

        [HttpGet("reward/{employeeId}")]
        public async Task<IActionResult> Rewards(int employeeId)
            => Ok(await _service.GetRewardsAsync(employeeId));

        [HttpGet("discipline/{employeeId}")]
        public async Task<IActionResult> Disciplines(int employeeId)
            => Ok(await _service.GetDisciplinesAsync(employeeId));

        [HttpPost("reward")]
        public async Task<IActionResult> AddReward([FromBody] CreateRewardDisciplineDto dto)
        {
            await _service.AddRewardAsync(dto);
            return Ok(new { message = "Thêm khen thưởng thành công" });
        }

        [HttpPost("discipline")]
        public async Task<IActionResult> AddDiscipline([FromBody] CreateRewardDisciplineDto dto)
        {
            await _service.AddDisciplineAsync(dto);
            return Ok(new { message = "Thêm kỷ luật thành công" });
        }
    }
}