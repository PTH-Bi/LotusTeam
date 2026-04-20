using LotusTeam.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LotusTeam.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaceAttendanceController : ControllerBase
    {
        private readonly IFaceAttendanceService _service;
        private readonly ILogger<FaceAttendanceController> _logger;

        public FaceAttendanceController(
            IFaceAttendanceService service,
            ILogger<FaceAttendanceController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =============================
        // CHECK IN
        // =============================
        [HttpPost("check-in")]
        public async Task<IActionResult> CheckIn(FaceCheckDto dto)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                // Validate
                if (dto.EmployeeId <= 0 || string.IsNullOrEmpty(dto.ImageBase64))
                {
                    _logger.LogWarning("Invalid input at CheckIn | TraceId: {traceId}", traceId);

                    return BadRequest(new
                    {
                        message = "Dữ liệu không hợp lệ",
                        traceId
                    });
                }

                var result = await _service.CheckIn(dto.EmployeeId, dto.ImageBase64);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        "CheckIn failed | EmployeeId: {empId} | Reason: {msg} | TraceId: {traceId}",
                        dto.EmployeeId, result.Message, traceId);

                    return BadRequest(new
                    {
                        message = result.Message,
                        traceId
                    });
                }

                _logger.LogInformation(
                    "CheckIn success | EmployeeId: {empId} | Confidence: {conf} | TraceId: {traceId}",
                    dto.EmployeeId, result.Confidence, traceId);

                return Ok(new
                {
                    message = result.Message,
                    confidence = result.Confidence,
                    traceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception at CheckIn | EmployeeId: {empId} | TraceId: {traceId}",
                    dto.EmployeeId, traceId);

                return StatusCode(500, new
                {
                    message = "Lỗi hệ thống",
                    detail = ex.Message, // dev thấy
                    traceId
                });
            }
        }

        // =============================
        // CHECK OUT
        // =============================
        [HttpPost("check-out")]
        public async Task<IActionResult> CheckOut(FaceCheckDto dto)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                if (dto.EmployeeId <= 0 || string.IsNullOrEmpty(dto.ImageBase64))
                {
                    _logger.LogWarning("Invalid input at CheckOut | TraceId: {traceId}", traceId);

                    return BadRequest(new
                    {
                        message = "Dữ liệu không hợp lệ",
                        traceId
                    });
                }

                var result = await _service.CheckOut(dto.EmployeeId, dto.ImageBase64);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        "CheckOut failed | EmployeeId: {empId} | Reason: {msg} | TraceId: {traceId}",
                        dto.EmployeeId, result.Message, traceId);

                    return BadRequest(new
                    {
                        message = result.Message,
                        traceId
                    });
                }

                _logger.LogInformation(
                    "CheckOut success | EmployeeId: {empId} | Confidence: {conf} | TraceId: {traceId}",
                    dto.EmployeeId, result.Confidence, traceId);

                return Ok(new
                {
                    message = result.Message,
                    confidence = result.Confidence,
                    traceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception at CheckOut | EmployeeId: {empId} | TraceId: {traceId}",
                    dto.EmployeeId, traceId);

                return StatusCode(500, new
                {
                    message = "Lỗi hệ thống",
                    detail = ex.Message,
                    traceId
                });
            }
        }

        // =============================
        // HISTORY
        // =============================
        [HttpGet("history/{employeeId}")]
        public async Task<IActionResult> GetHistory(int employeeId)
        {
            var traceId = HttpContext.TraceIdentifier;

            try
            {
                if (employeeId <= 0)
                {
                    _logger.LogWarning("Invalid employeeId at History | TraceId: {traceId}", traceId);

                    return BadRequest(new
                    {
                        message = "EmployeeId không hợp lệ",
                        traceId
                    });
                }

                var data = await _service.GetHistory(employeeId);

                _logger.LogInformation(
                    "GetHistory success | EmployeeId: {empId} | TraceId: {traceId}",
                    employeeId, traceId);

                return Ok(new
                {
                    data,
                    traceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception at GetHistory | EmployeeId: {empId} | TraceId: {traceId}",
                    employeeId, traceId);

                return StatusCode(500, new
                {
                    message = "Lỗi hệ thống",
                    detail = ex.Message,
                    traceId
                });
            }
        }
    }

    // DTO
    public class FaceCheckDto
    {
        public int EmployeeId { get; set; }
        public string ImageBase64 { get; set; } = null!;
    }
}