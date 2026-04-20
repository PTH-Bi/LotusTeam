using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.Service;
using Microsoft.EntityFrameworkCore;

namespace LotusTeam.Services
{
    public class RecruitmentService : IRecruitmentService
    {
        private readonly AppDbContext _context;

        public RecruitmentService(AppDbContext context)
        {
            _context = context;
        }

        // ==================================================
        // 1. ỨNG VIÊN
        // ==================================================
        public async Task<List<Candidates>> GetAllCandidatesAsync()
        {
            return await _context.Candidates
                .Include(c => c.Status)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Candidates> CreateCandidateAsync(Candidates candidate)
        {
            // StatusId = 1 giả sử là "New"
            candidate.StatusId = candidate.StatusId ?? 1;
            candidate.AppliedDate = DateTime.Now;

            _context.Candidates.Add(candidate);
            await _context.SaveChangesAsync();

            return candidate;
        }

        // ==================================================
        // 2. WORKFLOW TUYỂN DỤNG (TEMPLATE)
        // ==================================================
        public async Task<Workflows?> GetRecruitmentWorkflowTemplateAsync()
        {
            return await _context.Workflows
                .Include(w => w.WorkflowSteps.OrderBy(s => s.StepOrder))
                .AsNoTracking()
                .FirstOrDefaultAsync(w =>
                    w.Module == "Recruitment" &&
                    w.IsActive);
        }

        // ==================================================
        // 3. CẬP NHẬT TRẠNG THÁI ỨNG VIÊN
        // ==================================================
        public async Task AdvanceCandidateStageAsync(int candidateId, short nextStatusId)
        {
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.CandidateId == candidateId);

            if (candidate == null)
                throw new Exception("Ứng viên không tồn tại");

            candidate.StatusId = nextStatusId;
            await _context.SaveChangesAsync();
        }

        // ==================================================
        // 4. ĐÁNH GIÁ ỨNG VIÊN
        // ==================================================
        public async Task<PerformanceReview> ReviewCandidateAsync(PerformanceReview review)
        {
            review.ReviewDate = DateTime.Now;

            _context.PerformanceReviews.Add(review);
            await _context.SaveChangesAsync();

            return review;
        }

        // ==================================================
        // 5. TUYỂN THÀNH NHÂN VIÊN
        // ==================================================
        public async Task<Employees> ConvertToEmployeeAsync(int candidateId)
        {
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.CandidateId == candidateId);

            if (candidate == null)
                throw new Exception("Ứng viên không tồn tại");

            // StatusId = 5 giả sử là "Hired"
            candidate.StatusId = 5;

            var employee = new Employees
            {
                FullName = candidate.FullName,
                Email = candidate.Email,
                Phone = candidate.Phone,
                HireDate = DateTime.Now,
                Status = 1
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return employee;
        }
    }
}
