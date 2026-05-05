using LotusTeam.Models;

public interface IProjectService
{
    // Dự án
    Task<List<Project>> GetAllProjectsAsync();
    Task<Project> CreateProjectAsync(Project project);
    Task UpdateProjectStatusAsync(int projectId, short statusId);

    // Phân công
    Task<ProjectAssignment> AssignEmployeeAsync(ProjectAssignment assignment);
    Task<List<ProjectAssignment>> GetProjectAssignmentsAsync(int projectId);

    // Lịch sử
    Task<List<ProjectAssignment>> GetEmployeeProjectHistoryAsync(int employeeId);
}
