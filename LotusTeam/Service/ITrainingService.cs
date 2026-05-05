using LotusTeam.DTOs;

public interface ITrainingService
{
    Task<List<CourseDto>> GetAllCoursesAsync();

    Task<CourseDto> CreateCourseAsync(CreateCourseDto dto);

    Task EnrollEmployeeAsync(EnrollEmployeeDto dto);

    Task<List<CourseDto>> GetEmployeeCoursesAsync(int employeeId);

    Task UpdateTrainingResultAsync(
        int enrollmentId,
        short statusId,
        DateOnly? completionDate,
        string? certificatePath);

    Task<List<CourseDto>> GetEmployeeCertificatesAsync(int employeeId);
}