using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Service
{
    public class SurveyService : ISurveyService
    {
        private readonly AppDbContext _context;

        public SurveyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateSurveyAsync(CreateSurveyDto dto)
        {
            var survey = new Survey
            {
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsAnonymous = dto.IsAnonymous,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now
            };

            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();
        }

        public async Task SubmitResponseAsync(SubmitSurveyResponseDto dto)
        {
            var response = new SurveyResponse
            {
                SurveyID = dto.SurveyID,
                EmployeeID = dto.EmployeeID,
                ResponseData = dto.ResponseData,
                SubmittedDate = DateTime.Now
            };

            _context.SurveyResponses.Add(response);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SurveyResponseDto>> GetResultsAsync(int surveyId)
        {
            return await _context.SurveyResponses
                .Where(r => r.SurveyID == surveyId)
                .Select(r => new SurveyResponseDto
                {
                    ResponseID = r.ResponseID,
                    SurveyID = r.SurveyID,
                    EmployeeID = r.EmployeeID,
                    SubmittedDate = r.SubmittedDate,
                    ResponseData = r.ResponseData
                })
                .AsNoTracking()
                .ToListAsync();
        }

        // ========== PHƯƠNG THỨC BỔ SUNG ==========

        public async Task<List<SurveyDto>> GetAllSurveysAsync()
        {
            var surveys = await _context.Surveys
                .OrderByDescending(s => s.CreatedDate)
                .AsNoTracking()
                .ToListAsync();

            var result = new List<SurveyDto>();
            foreach (var survey in surveys)
            {
                var responseCount = await _context.SurveyResponses
                    .CountAsync(r => r.SurveyID == survey.SurveyID);

                result.Add(new SurveyDto
                {
                    SurveyID = survey.SurveyID,
                    Title = survey.Title,
                    Description = survey.Description,
                    StartDate = survey.StartDate,
                    EndDate = survey.EndDate,
                    IsAnonymous = survey.IsAnonymous,
                    CreatedBy = survey.CreatedBy,
                    CreatedDate = survey.CreatedDate,
                    ResponseCount = responseCount
                });
            }

            return result;
        }

        public async Task<SurveyDto?> GetSurveyByIdAsync(int surveyId)
        {
            var survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.SurveyID == surveyId);

            if (survey == null) return null;

            var responseCount = await _context.SurveyResponses
                .CountAsync(r => r.SurveyID == surveyId);

            return new SurveyDto
            {
                SurveyID = survey.SurveyID,
                Title = survey.Title,
                Description = survey.Description,
                StartDate = survey.StartDate,
                EndDate = survey.EndDate,
                IsAnonymous = survey.IsAnonymous,
                CreatedBy = survey.CreatedBy,
                CreatedDate = survey.CreatedDate,
                ResponseCount = responseCount
            };
        }

        public async Task<bool> HasEmployeeRespondedAsync(int surveyId, int employeeId)
        {
            return await _context.SurveyResponses
                .AnyAsync(r => r.SurveyID == surveyId && r.EmployeeID == employeeId);
        }
    }
}