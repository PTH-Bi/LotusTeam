using LotusTeam.DTOs;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/surveys")]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class SurveyController : ControllerBase
    {
        private readonly ISurveyService _service;
        private readonly ILogger<SurveyController> _logger;

        public SurveyController(ISurveyService service, ILogger<SurveyController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // =========================
        // CREATE SURVEY
        // =========================

        /// <summary>
        /// Tạo mới khảo sát (Chỉ HR Manager và Manager)
        /// </summary>
        /// <param name="dto">Thông tin khảo sát</param>
        /// <returns>Kết quả tạo khảo sát</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        [HttpPost]
        public async Task<IActionResult> CreateSurvey([FromBody] CreateSurveyDto dto)
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
                // Gán người tạo là người dùng hiện tại nếu không được cung cấp
                if (dto.CreatedBy == null || dto.CreatedBy == 0)
                {
                    var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    dto.CreatedBy = currentUserId;
                }

                await _service.CreateSurveyAsync(dto);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Tạo khảo sát thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo khảo sát");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo khảo sát",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =========================
        // SUBMIT RESPONSE
        // =========================

        /// <summary>
        /// Gửi phản hồi khảo sát
        /// </summary>
        /// <param name="dto">Thông tin phản hồi</param>
        /// <returns>Kết quả gửi phản hồi</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE,INTERN,PROBATION_STAFF")]
        [HttpPost("response")]
        public async Task<IActionResult> SubmitResponse([FromBody] SubmitSurveyResponseDto dto)
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
                // Gán EmployeeID là người dùng hiện tại nếu không được cung cấp
                if (dto.EmployeeID == null || dto.EmployeeID == 0)
                {
                    var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    dto.EmployeeID = currentUserId;
                }

                // Kiểm tra người dùng chỉ được gửi phản hồi cho chính mình
                var currentUserIdCheck = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "TEAM_LEADER" };

                if (!allowedRoles.Contains(currentUserRole) && currentUserIdCheck != dto.EmployeeID)
                {
                    return Forbid();
                }

                await _service.SubmitResponseAsync(dto);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Gửi phản hồi thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi phản hồi khảo sát");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi gửi phản hồi",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =========================
        // GET ALL SURVEYS
        // =========================

        /// <summary>
        /// Lấy danh sách tất cả khảo sát
        /// </summary>
        /// <returns>Danh sách khảo sát</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet]
        public async Task<IActionResult> GetAllSurveys()
        {
            try
            {
                var surveys = await _service.GetAllSurveysAsync();
                return Ok(new ApiResponse<List<SurveyDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách khảo sát thành công",
                    Data = surveys,
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = surveys.Count,
                        Page = 1,
                        PageSize = surveys.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách khảo sát");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách khảo sát",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =========================
        // GET SURVEY BY ID
        // =========================

        /// <summary>
        /// Lấy thông tin chi tiết khảo sát theo ID
        /// </summary>
        /// <param name="surveyId">ID khảo sát</param>
        /// <returns>Thông tin khảo sát</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("{surveyId:int}")]
        public async Task<IActionResult> GetSurveyById(int surveyId)
        {
            if (surveyId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "SurveyId không hợp lệ",
                    StatusCode = 400
                });
            }

            try
            {
                var survey = await _service.GetSurveyByIdAsync(surveyId);
                if (survey == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy khảo sát với ID {surveyId}",
                        StatusCode = 404
                    });
                }

                return Ok(new ApiResponse<SurveyDto>
                {
                    Success = true,
                    Message = "Lấy thông tin khảo sát thành công",
                    Data = survey,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin khảo sát ID {SurveyId}", surveyId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin khảo sát",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =========================
        // RESULTS
        // =========================

        /// <summary>
        /// Lấy kết quả khảo sát (Chỉ HR và Manager)
        /// </summary>
        /// <param name="surveyId">ID khảo sát</param>
        /// <returns>Kết quả khảo sát</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        [HttpGet("{surveyId:int}/results")]
        public async Task<IActionResult> GetResults(int surveyId)
        {
            if (surveyId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "SurveyId không hợp lệ",
                    StatusCode = 400
                });
            }

            try
            {
                var results = await _service.GetResultsAsync(surveyId);
                return Ok(new ApiResponse<List<SurveyResponseDto>>
                {
                    Success = true,
                    Message = "Lấy kết quả khảo sát thành công",
                    Data = results,
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = results.Count,
                        Page = 1,
                        PageSize = results.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy kết quả khảo sát ID {SurveyId}", surveyId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy kết quả khảo sát",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =========================
        // CHECK IF EMPLOYEE RESPONDED
        // =========================

        /// <summary>
        /// Kiểm tra nhân viên đã trả lời khảo sát chưa
        /// </summary>
        /// <param name="surveyId">ID khảo sát</param>
        /// <returns>Kết quả kiểm tra</returns>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,TEAM_LEADER,EMPLOYEE")]
        [HttpGet("{surveyId:int}/has-responded")]
        public async Task<IActionResult> HasResponded(int surveyId)
        {
            if (surveyId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "SurveyId không hợp lệ",
                    StatusCode = 400
                });
            }

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var hasResponded = await _service.HasEmployeeRespondedAsync(surveyId, currentUserId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = hasResponded ? "Nhân viên đã trả lời khảo sát" : "Nhân viên chưa trả lời khảo sát",
                    Data = hasResponded,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái trả lời khảo sát ID {SurveyId}", surveyId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi kiểm tra trạng thái",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }
    }
}