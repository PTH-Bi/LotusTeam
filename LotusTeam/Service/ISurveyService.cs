using LotusTeam.DTOs;

public interface ISurveyService
{
    Task CreateSurveyAsync(CreateSurveyDto dto);

    Task SubmitResponseAsync(SubmitSurveyResponseDto dto);

    Task<List<SurveyResponseDto>> GetResultsAsync(int surveyId);
}