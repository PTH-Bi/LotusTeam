using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Service
{
    public class RewardDisciplineService : IRewardDisciplineService
    {
        private readonly AppDbContext _context;

        public RewardDisciplineService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RewardDisciplineDto>> GetRewardsAsync(int employeeId)
        {
            return await _context.RewardsDisciplines
                .Where(x => x.EmployeeID == employeeId && x.Type == 1)
                .Select(x => new RewardDisciplineDto
                {
                    RDID = x.RDID,
                    EmployeeID = x.EmployeeID,
                    Type = x.Type,
                    Title = x.Title,
                    Description = x.Description,
                    RDDate = x.RDDate,
                    DecisionNumber = x.DecisionNumber,
                    EffectiveDate = x.EffectiveDate,
                    StatusID = x.StatusID
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<RewardDisciplineDto>> GetDisciplinesAsync(int employeeId)
        {
            return await _context.RewardsDisciplines
                .Where(x => x.EmployeeID == employeeId && x.Type == 2)
                .Select(x => new RewardDisciplineDto
                {
                    RDID = x.RDID,
                    EmployeeID = x.EmployeeID,
                    Type = x.Type,
                    Title = x.Title,
                    Description = x.Description,
                    RDDate = x.RDDate,
                    DecisionNumber = x.DecisionNumber,
                    EffectiveDate = x.EffectiveDate,
                    StatusID = x.StatusID
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddRewardAsync(CreateRewardDisciplineDto dto)
        {
            var reward = new RewardsDisciplines
            {
                EmployeeID = dto.EmployeeID,
                Title = dto.Title,
                Description = dto.Description,
                DecisionNumber = dto.DecisionNumber,
                EffectiveDate = dto.EffectiveDate,
                StatusID = dto.StatusID,
                Type = 1,
                RDDate = DateTime.Now
            };

            _context.RewardsDisciplines.Add(reward);
            await _context.SaveChangesAsync();
        }

        public async Task AddDisciplineAsync(CreateRewardDisciplineDto dto)
        {
            var discipline = new RewardsDisciplines
            {
                EmployeeID = dto.EmployeeID,
                Title = dto.Title,
                Description = dto.Description,
                DecisionNumber = dto.DecisionNumber,
                EffectiveDate = dto.EffectiveDate,
                StatusID = dto.StatusID,
                Type = 2,
                RDDate = DateTime.Now
            };

            _context.RewardsDisciplines.Add(discipline);
            await _context.SaveChangesAsync();
        }
    }
}