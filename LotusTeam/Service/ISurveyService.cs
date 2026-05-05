using LotusTeam.DTOs;

namespace LotusTeam.Service
{
    public interface ISurveyService
    {
        // Phương thức hiện có
        Task CreateSurveyAsync(CreateSurveyDto dto);
        Task SubmitResponseAsync(SubmitSurveyResponseDto dto);
        Task<List<SurveyResponseDto>> GetResultsAsync(int surveyId);

        // Phương thức bổ sung
        Task<List<SurveyDto>> GetAllSurveysAsync();
        Task<SurveyDto?> GetSurveyByIdAsync(int surveyId);
        Task<bool> HasEmployeeRespondedAsync(int surveyId, int employeeId);
    }
}