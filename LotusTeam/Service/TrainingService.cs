using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Service
{
    public class TrainingService : ITrainingService
    {
        private readonly AppDbContext _context;

        public TrainingService(AppDbContext context)
        {
            _context = context;
        }

        // =============================
        // GET ALL COURSES
        // =============================
        public async Task<List<CourseDto>> GetAllCoursesAsync()
        {
            return await _context.Courses
                .Select(c => new CourseDto
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    Description = c.Description
                })
                .AsNoTracking()
                .ToListAsync();
        }

        // =============================
        // CREATE COURSE
        // =============================
        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto)
        {
            var course = new Courses
            {
                CourseName = dto.CourseName,
                Description = dto.Description
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return new CourseDto
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                Description = course.Description
            };
        }

        // =============================
        // ENROLL EMPLOYEE
        // =============================
        public async Task EnrollEmployeeAsync(EnrollEmployeeDto dto)
        {
            var enrollment = new EmployeeCourses
            {
                EmployeeId = dto.EmployeeId,
                CourseId = dto.CourseId,
                StatusId = 1
            };

            _context.EmployeeCourses.Add(enrollment);
            await _context.SaveChangesAsync();
        }

        // =============================
        // EMPLOYEE COURSES
        // =============================
        public async Task<List<CourseDto>> GetEmployeeCoursesAsync(int employeeId)
        {
            return await _context.EmployeeCourses
                .Where(x => x.EmployeeId == employeeId)
                .Include(x => x.Course)
                .Select(x => new CourseDto
                {
                    CourseId = x.Course.CourseId,
                    CourseName = x.Course.CourseName,
                    Description = x.Course.Description
                })
                .AsNoTracking()
                .ToListAsync();
        }

        // =============================
        // UPDATE RESULT
        // =============================
        public async Task UpdateTrainingResultAsync(
            int enrollmentId,
            short statusId,
            DateOnly? completionDate,
            string? certificatePath)
        {
            var enrollment = await _context.EmployeeCourses
                .FirstOrDefaultAsync(x => x.EnrollmentId == enrollmentId);

            if (enrollment == null)
                throw new Exception("Enrollment not found");

            enrollment.StatusId = statusId;
            enrollment.CompletionDate = completionDate;
            enrollment.CertificatePath = certificatePath;

            await _context.SaveChangesAsync();
        }

        // =============================
        // GET CERTIFICATES
        // =============================
        public async Task<List<CourseDto>> GetEmployeeCertificatesAsync(int employeeId)
        {
            return await _context.EmployeeCourses
                .Where(x => x.EmployeeId == employeeId && x.CertificatePath != null)
                .Include(x => x.Course)
                .Select(x => new CourseDto
                {
                    CourseId = x.Course.CourseId,
                    CourseName = x.Course.CourseName,
                    Description = x.Course.Description
                })
                .AsNoTracking()
                .ToListAsync();
        }
    }
}