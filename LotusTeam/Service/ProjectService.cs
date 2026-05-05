using LotusTeam.Data;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _context;

    public ProjectService(AppDbContext context)
    {
        _context = context;
    }

    // ==================================================
    // 1. QUẢN LÝ DỰ ÁN
    // ==================================================
    public async Task<List<Project>> GetAllProjectsAsync()
    {
        return await _context.Projects
            .Include(p => p.Manager)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task UpdateProjectStatusAsync(int projectId, short statusId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
            throw new Exception("Không tìm thấy dự án");

        project.StatusID = statusId;
        await _context.SaveChangesAsync();
    }

    // ==================================================
    // 2. PHÂN CÔNG NHÂN SỰ
    // ==================================================
    public async Task<ProjectAssignment> AssignEmployeeAsync(ProjectAssignment assignment)
    {
        assignment.AssignedDate = DateTime.Now;

        _context.ProjectAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<List<ProjectAssignment>> GetProjectAssignmentsAsync(int projectId)
    {
        return await _context.ProjectAssignments
            .Include(pa => pa.Employee)
            .Where(pa => pa.ProjectID == projectId)
            .AsNoTracking()
            .ToListAsync();
    }

    // ==================================================
    // 3. LỊCH SỬ THAM GIA DỰ ÁN
    // ==================================================
    public async Task<List<ProjectAssignment>> GetEmployeeProjectHistoryAsync(int employeeId)
    {
        return await _context.ProjectAssignments
            .Include(pa => pa.Project)
            .Where(pa => pa.EmployeeID == employeeId)
            .OrderByDescending(pa => pa.AssignedDate)
            .AsNoTracking()
            .ToListAsync();
    }
}
