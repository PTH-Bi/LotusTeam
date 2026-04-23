using Microsoft.AspNetCore.Mvc;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using LotusTeam.DTOs;

// Aliases để tránh conflict tên class
using ServiceChatResponse = LotusTeam.Service.ChatResponse;
using ServiceFaqItem = LotusTeam.Service.FaqItem;

namespace LotusTeam.Controllers
{
    /// <summary>
    /// API Chatbot hỗ trợ nhân viên và quản lý
    /// </summary>
    [ApiController]
    [Route("api/chatbot")]
    [Authorize]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            ChatbotService chatbotService,
            ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        /// <summary>
        /// Gửi câu hỏi đến chatbot và nhận phản hồi
        /// </summary>
        [HttpPost("ask")]
        [ProducesResponseType(typeof(ApiResponse<ServiceChatResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<ServiceChatResponse>>> Ask([FromBody] ChatRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nội dung câu hỏi không được để trống"
                    });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("id")?.Value
                                  ?? User.FindFirst("userId")?.Value;

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token, using default user ID 1");
                    userId = 1;
                }

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value
                               ?? User.FindFirst("role")?.Value
                               ?? "EMPLOYEE";

                _logger.LogInformation("User {UserId} with role {UserRole} asked: {Message}",
                    userId, userRole, request.Message);

                var result = await _chatbotService.Handle(request.Message, userId, userRole);

                return Ok(new ApiResponse<ServiceChatResponse>
                {
                    Success = true,
                    Data = result,
                    Message = "Phản hồi từ chatbot"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to chatbot by user");
                return StatusCode(403, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Bạn không có quyền truy cập tính năng này"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chatbot request");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xử lý câu hỏi. Vui lòng thử lại sau."
                });
            }
        }

        /// <summary>
        /// Lấy danh sách câu hỏi thường gặp (FAQ)
        /// </summary>
        [HttpGet("faq")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ServiceFaqItem>>), 200)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ServiceFaqItem>>>> GetFaqs()
        {
            try
            {
                var faqs = await _chatbotService.GetFaqsAsync();

                return Ok(new ApiResponse<IEnumerable<ServiceFaqItem>>
                {
                    Success = true,
                    Data = faqs,
                    Message = "Danh sách câu hỏi thường gặp"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FAQs");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách câu hỏi thường gặp"
                });
            }
        }

        /// <summary>
        /// Gửi phản hồi đánh giá cho câu trả lời của chatbot
        /// </summary>
        [HttpPost("feedback")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<ActionResult<ApiResponse<object>>> SubmitFeedback([FromBody] ChatFeedbackRequest feedback)
        {
            try
            {
                if (feedback == null || string.IsNullOrWhiteSpace(feedback.MessageId))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Thông tin phản hồi không hợp lệ"
                    });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("id")?.Value;

                int.TryParse(userIdClaim, out int userId);

                await _chatbotService.SaveFeedbackAsync(
                    feedback.MessageId,
                    userId,
                    feedback.IsHelpful,
                    feedback.Comment);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Cảm ơn bạn đã đánh giá!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chatbot feedback");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lưu đánh giá"
                });
            }
        }
    }

    #region Request/Response DTOs

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatFeedbackRequest
    {
        public string MessageId { get; set; } = string.Empty;
        public bool IsHelpful { get; set; }
        public string? Comment { get; set; }
    }

    #endregion
}