/*// Services/AzureFaceRecognitionProvider.cs
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LotusTeam.Service
{
    public class AzureFaceRecognitionProvider : IFaceRecognitionProvider
    {
        private readonly HttpClient _http;
        private readonly string _endpoint;
        private readonly string _subscriptionKey;
        private readonly ILogger<AzureFaceRecognitionProvider> _logger;

        public AzureFaceRecognitionProvider(
            HttpClient http,
            IConfiguration config,
            ILogger<AzureFaceRecognitionProvider> logger)
        {
            _http = http;
            _endpoint = config["Azure:FaceApiEndpoint"]!; // e.g. https://xxx.cognitiveservices.azure.com
            _subscriptionKey = config["Azure:FaceApiKey"]!;
            _logger = logger;
        }

        public async Task<FaceMatchResult> MatchFace(string capturedBase64, string registeredBase64)
        {
            try
            {
                var faceId1 = await DetectFaceId(capturedBase64);
                var faceId2 = await DetectFaceId(registeredBase64);

                if (faceId1 == null)
                    return new FaceMatchResult { IsMatch = false, ErrorMessage = "Không phát hiện khuôn mặt trong ảnh chấm công" };

                if (faceId2 == null)
                    return new FaceMatchResult { IsMatch = false, ErrorMessage = "Không phát hiện khuôn mặt trong ảnh đã đăng ký" };

                var body = JsonSerializer.Serialize(new { faceId1, faceId2 });
                var req = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/face/v1.0/verify")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                req.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

                var res = await _http.SendAsync(req);
                var json = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogError("Azure Face verify failed: {json}", json);
                    return new FaceMatchResult { IsMatch = false, ErrorMessage = "Lỗi khi xác minh khuôn mặt" };
                }

                var obj = JsonSerializer.Deserialize<JsonElement>(json);
                return new FaceMatchResult
                {
                    IsMatch = obj.GetProperty("isIdentical").GetBoolean(),
                    Confidence = obj.GetProperty("confidence").GetDouble()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AzureFaceRecognitionProvider.MatchFace");
                return new FaceMatchResult { IsMatch = false, ErrorMessage = "Lỗi kết nối dịch vụ nhận diện" };
            }
        }

        public async Task<double> ValidateImageQuality(string imageBase64)
        {
            try
            {
                var faceId = await DetectFaceId(imageBase64, returnQuality: true);
                // Azure trả faceId = null nếu ảnh không có khuôn mặt rõ ràng
                return faceId != null ? 0.85 : 0.2;
            }
            catch
            {
                return 0.0;
            }
        }

        private async Task<string?> DetectFaceId(string base64, bool returnQuality = false)
        {
            // Bỏ prefix "data:image/jpeg;base64," nếu có
            var pureBase64 = base64.Contains(',') ? base64.Split(',')[1] : base64;
            var bytes = Convert.FromBase64String(pureBase64);

            var url = $"{_endpoint}/face/v1.0/detect?returnFaceId=true&detectionModel=detection_03";
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new ByteArrayContent(bytes)
            };
            req.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Azure Face detect failed: {json}", json);
                return null;
            }

            var arr = JsonSerializer.Deserialize<JsonElement[]>(json);
            if (arr == null || arr.Length == 0)
                return null;

            return arr[0].GetProperty("faceId").GetString();
        }
    }
}*/