using LotusTeam.DTOs;

public interface IRewardDisciplineService
{
    Task<List<RewardDisciplineDto>> GetRewardsAsync(int employeeId);
    Task<List<RewardDisciplineDto>> GetDisciplinesAsync(int employeeId);

    Task AddRewardAsync(CreateRewardDisciplineDto dto);
    Task AddDisciplineAsync(CreateRewardDisciplineDto dto);
}