using LotusTeam.DTOs;
using LotusTeam.Models;

public interface IPerformanceService
{
    Task<PerformanceReview> CreatePerformanceReviewAsync(PerformanceReview review);

    Task<List<PerformanceReviewDto>> GetReviewHistoryAsync(int employeeId);

    Task<List<EmployeeSkillDto>> GetEmployeeSkillsAsync(int employeeId);

    Task UpdateEmployeeSkillAsync(EmployeeSkill skill);

    Task<List<Training>> RecommendTrainingAsync(int employeeId);

    Task<CapabilityDto> GetEmployeeCapabilityAsync(int employeeId);
}