using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace LotusTeam.Service
{
    public class AIService
    {
        private readonly HttpClient _http;
        private const string API_KEY = "YOUR_OPENAI_API_KEY";

        public AIService(HttpClient http)
        {
            _http = http;
        }

        public async Task<AIResult> CallFunction(string message)
        {
            try
            {
                var body = new
                {
                    model = "gpt-4o",
                    messages = new object[]
                    {
                new { role = "system", content = "Bạn là trợ lý HR." },
                new { role = "user", content = message }
                    },
                    tools = new object[]
                    {
                new {
                    type = "function",
                    function = new {
                        name = "get_leave",
                        description = "Lấy ngày phép",
                        parameters = new {
                            type = "object",
                            properties = new { employeeId = new { type = "integer" } }
                        }
                    }
                }
                    },
                    tool_choice = "auto"
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);
                request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                var response = await _http.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var msg = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

                if (msg.TryGetProperty("tool_calls", out var tools))
                {
                    var func = tools[0].GetProperty("function");

                    return new AIResult
                    {
                        ToolName = func.GetProperty("name").GetString()
                    };
                }

                return new AIResult
                {
                    Content = msg.GetProperty("content").GetString()
                };
            }
            catch (Exception ex)
            {
                return new AIResult
                {
                    Content = "AI lỗi: " + ex.Message
                };
            }
        }
    }
}

public class AIResult
{
    public string? ToolName { get; set; }
    public string? Content { get; set; }
}