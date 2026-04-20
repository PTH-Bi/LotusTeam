using LotusTeam.Services;
using LotusTeam.Models;
using LotusTeam.DTOs;
using Microsoft.AspNetCore.Mvc;
using LotusTeam.Service;


namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/leave")]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveService _service;

        public LeaveController(ILeaveService service)
        {
            _service = service;
        }

        // ================= LOẠI NGHỈ =================

        [HttpGet("types")]
        public async Task<IActionResult> GetLeaveTypes()
            => Ok(await _service.GetLeaveTypesAsync());


        [HttpPost("types")]
        public async Task<IActionResult> CreateLeaveType([FromBody] CreateLeaveTypeDto dto)
        {
            var leaveType = new LeaveType
            {
                LeaveTypeCode = dto.LeaveTypeCode,
                LeaveTypeName = dto.LeaveTypeName,
                IsPaid = dto.IsPaid,
                Description = dto.Description,
                IsActive = dto.IsActive
            };

            var result = await _service.CreateLeaveTypeAsync(leaveType);
            return Ok(result);
        }


        // ================= ĐƠN NGHỈ =================

        [HttpPost("request")]
        public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestDto dto)
        {
            var request = new LeaveRequest
            {
                EmployeeID = dto.EmployeeID,
                LeaveTypeID = dto.LeaveTypeID,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                NumberOfDays = dto.NumberOfDays,
                Reason = dto.Reason
            };

            var result = await _service.CreateLeaveRequestAsync(request);

            return Ok(result);
        }


        // ================= CÁ NHÂN =================

        [HttpGet("my/{employeeId}")]
        public async Task<IActionResult> MyLeave(int employeeId)
            => Ok(await _service.GetMyLeaveAsync(employeeId));


        [HttpGet("history/{employeeId}")]
        public async Task<IActionResult> LeaveHistory(int employeeId)
            => Ok(await _service.GetLeaveHistoryAsync(employeeId));


        // ================= DUYỆT =================

        [HttpPost("approve/{leaveId}")]
        public async Task<IActionResult> ApproveLeave(int leaveId, int approverId)
            => await _service.ApproveLeaveAsync(leaveId, approverId)
                ? Ok()
                : NotFound();


        [HttpPost("reject/{leaveId}")]
        public async Task<IActionResult> RejectLeave(int leaveId, int approverId)
            => await _service.RejectLeaveAsync(leaveId, approverId)
                ? Ok()
                : NotFound();


        // ================= SỐ DƯ =================

        [HttpGet("balance")]
        public async Task<IActionResult> LeaveBalance(int employeeId, int leaveTypeId)
            => Ok(await _service.GetLeaveBalanceAsync(employeeId, leaveTypeId));


        // ================= LỊCH NGHỈ =================

        [HttpGet("calendar")]
        public async Task<IActionResult> LeaveCalendar(DateTime from, DateTime to)
            => Ok(await _service.GetLeaveCalendarAsync(from, to));
    }
}