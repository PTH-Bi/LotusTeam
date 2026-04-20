using Microsoft.AspNetCore.Mvc;
using LotusTeam.Service;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;

        public ChatbotController(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            int userId = int.Parse(User.FindFirst("id")?.Value ?? "1");

            var result = await _chatbotService.Handle(request.Message, userId);
            return Ok(result);
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}