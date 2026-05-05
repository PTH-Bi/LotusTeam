// Services/MockFaceRecognitionProvider.cs
namespace LotusTeam.Service
{
    public class MockFaceRecognitionProvider : IFaceRecognitionProvider
    {
        public Task<FaceMatchResult> MatchFace(string capturedBase64, string registeredBase64)
        {
            var rng = new Random();
            var confidence = 0.75 + rng.NextDouble() * 0.25;
            return Task.FromResult(new FaceMatchResult
            {
                IsMatch = true,
                Confidence = confidence
            });
        }

        public Task<double> ValidateImageQuality(string imageBase64)
        {
            var rng = new Random();
            return Task.FromResult(0.6 + rng.NextDouble() * 0.4);
        }
    }
}