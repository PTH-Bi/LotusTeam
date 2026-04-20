using LotusTeam.Models;
using LotusTeam.Services;
using LotusTeam.DTOs;
using Microsoft.AspNetCore.Mvc;
using LotusTeam.Service;


namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/leave-balance")]
    public class LeaveBalanceController : ControllerBase
    {
        private readonly ILeaveBalanceService _service;

        public LeaveBalanceController(ILeaveBalanceService service)
        {
            _service = service;
        }

        // ================= LẤY THEO NHÂN VIÊN =================

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetByEmployee(int employeeId)
        {
            var result = await _service.GetByEmployeeAsync(employeeId);
            return Ok(result);
        }

        // ================= TẠO / UPDATE =================

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate([FromBody] LeaveBalanceDto dto)
        {
            var model = new LeaveBalances
            {
                EmployeeID = dto.EmployeeID,
                Year = dto.Year,
                AnnualQuota = dto.AnnualQuota,
                UsedDays = dto.UsedDays,
                UnpaidDays = dto.UnpaidDays,
                ConsecutiveLeaveDays = dto.ConsecutiveLeaveDays,
                LastLeaveEndDate = dto.LastLeaveEndDate,
                IsReset = dto.IsReset
            };

            var result = await _service.CreateOrUpdateAsync(model);

            return Ok(result);
        }

        // ================= XÓA =================

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok();
        }
    }
}