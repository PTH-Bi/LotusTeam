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
    }
}