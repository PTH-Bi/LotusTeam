using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;

public class PerformanceService : IPerformanceService
{
    private readonly AppDbContext _context;

    public PerformanceService(AppDbContext context)
    {
        _context = context;
    }

    // =============================================
    // CREATE REVIEW
    // =============================================

    public async Task<PerformanceReview> CreatePerformanceReviewAsync(PerformanceReview review)
    {
        review.ReviewDate = DateTime.Now;

        _context.PerformanceReviews.Add(review);

        await _context.SaveChangesAsync();

        return review;
    }

    // =============================================
    // REVIEW HISTORY
    // =============================================

    public async Task<List<PerformanceReviewDto>> GetReviewHistoryAsync(int employeeId)
    {
        return await _context.PerformanceReviews
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.ReviewDate)
            .Select(r => new PerformanceReviewDto
            {
                ReviewId = r.ReviewId,
                EmployeeID = r.EmployeeId,
                ReviewDate = r.ReviewDate,
                ReviewerId = r.ReviewerId,
                Score = r.Score,
                Comments = r.Comments,
                ReviewPeriod = r.ReviewPeriod
            })
            .AsNoTracking()
            .ToListAsync();
    }

    // =============================================
    // EMPLOYEE SKILLS
    // =============================================

    public async Task<List<EmployeeSkillDto>> GetEmployeeSkillsAsync(int employeeId)
    {
        return await _context.EmployeeSkills
            .Include(es => es.Skill)
            .Where(es => es.EmployeeID == employeeId)
            .Select(es => new EmployeeSkillDto
            {
                SkillID = es.SkillID,
                SkillName = es.Skill.SkillName,
                ProficiencyLevel = es.ProficiencyLevel
            })
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateEmployeeSkillAsync(EmployeeSkill skill)
    {
        var existing = await _context.EmployeeSkills
            .FirstOrDefaultAsync(es =>
                es.EmployeeID == skill.EmployeeID &&
                es.SkillID == skill.SkillID);

        if (existing == null)
        {
            skill.VerifiedDate = DateTime.Now;
            _context.EmployeeSkills.Add(skill);
        }
        else
        {
            existing.ProficiencyLevel = skill.ProficiencyLevel;
            existing.VerifiedBy = skill.VerifiedBy;
            existing.Certification = skill.Certification;
            existing.VerifiedDate = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    // =============================================
    // TRAINING RECOMMEND
    // =============================================

    public async Task<List<Training>> RecommendTrainingAsync(int employeeId)
    {
        return await _context.Trainings
            .Where(t => !_context.EmployeeTrainings
                .Any(et =>
                    et.EmployeeID == employeeId &&
                    et.TrainingID == t.TrainingID))
            .AsNoTracking()
            .ToListAsync();
    }

    // =============================================
    // CAPABILITY
    // =============================================

    public async Task<CapabilityDto> GetEmployeeCapabilityAsync(int employeeId)
    {
        var skills = await GetEmployeeSkillsAsync(employeeId);

        var reviews = await _context.PerformanceReviews
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.ReviewDate)
            .Take(5)
            .Select(r => new PerformanceReviewDto
            {
                ReviewId = r.ReviewId,
                EmployeeID = r.EmployeeId,
                ReviewDate = r.ReviewDate,
                ReviewerId = r.ReviewerId,
                Score = r.Score,
                Comments = r.Comments,
                ReviewPeriod = r.ReviewPeriod
            })
            .AsNoTracking()
            .ToListAsync();

        return new CapabilityDto
        {
            EmployeeID = employeeId,
            Skills = skills,
            RecentReviews = reviews
        };
    }
}