// Services/IFaceRecognitionProvider.cs
namespace LotusTeam.Service
{
    public interface IFaceRecognitionProvider
    {
        /// <summary>So sánh ảnh chấm công với ảnh đã đăng ký</summary>
        Task<FaceMatchResult> MatchFace(string capturedBase64, string registeredBase64);

        /// <summary>Kiểm tra chất lượng ảnh trước khi đăng ký</summary>
        Task<double> ValidateImageQuality(string imageBase64);
    }

    public class FaceMatchResult
    {
        public bool IsMatch { get; set; }
        public double Confidence { get; set; }
        public string? ErrorMessage { get; set; }
    }
}