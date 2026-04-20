using UglyToad.PdfPig;

namespace LotusTeam.Services
{
    public class PdfParserService
    {
        private readonly ILogger<PdfParserService> _logger;

        public PdfParserService(ILogger<PdfParserService> logger)
        {
            _logger = logger;
        }

        public string ExtractText(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File not found: {File}", filePath);
                    return string.Empty;
                }

                using var document = PdfDocument.Open(filePath);

                var textBuilder = new System.Text.StringBuilder();

                foreach (var page in document.GetPages())
                {
                    textBuilder.AppendLine(page.Text);
                }

                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF parsing failed for {File}", filePath);
                return string.Empty;
            }
        }
    }
}