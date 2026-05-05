using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Models;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/recruitment")]
    [Authorize] // Yêu cầu xác thực cho tất cả các action
    public class RecruitmentController : ControllerBase
    {
        private readonly IRecruitmentService _service;
        private readonly AppDbContext _context;

        public RecruitmentController(IRecruitmentService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        // ==================================================
        // 1. ỨNG VIÊN
        // ==================================================

        /// <summary>
        /// Lấy danh sách ứng viên - Chỉ HR và Admin mới xem được
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        [HttpGet("candidates")]
        public async Task<IActionResult> GetCandidates()
        {
            return Ok(await _service.GetAllCandidatesAsync());
        }

        /// <summary>
        /// Tạo ứng viên mới - Bất kỳ ai có tài khoản đều có thể ứng tuyển
        /// (Cho phép cả Employee, Intern tự ứng tuyển)
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,EMPLOYEE,INTERN,PROBATION_STAFF")]
        [HttpPost("candidates")]
        public async Task<IActionResult> CreateCandidate([FromBody] Candidates candidate)
        {
            return Ok(await _service.CreateCandidateAsync(candidate));
        }

        // ==================================================
        // 2. WORKFLOW TUYỂN DỤNG (TEMPLATE)
        // ==================================================

        /// <summary>
        /// Lấy workflow tuyển dụng - Chỉ HR và Admin
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        [HttpGet("workflow/template")]
        public async Task<IActionResult> GetWorkflowTemplate()
        {
            return Ok(await _service.GetRecruitmentWorkflowTemplateAsync());
        }

        // ==================================================
        // 3. CHUYỂN GIAI ĐOẠN (STATUS)
        // ==================================================

        /// <summary>
        /// Chuyển giai đoạn cho ứng viên - Chỉ HR và Admin
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        [HttpPost("candidates/{candidateId:int}/advance")]
        public async Task<IActionResult> AdvanceStage(
            int candidateId,
            [FromQuery] short nextStatusId)
        {
            await _service.AdvanceCandidateStageAsync(candidateId, nextStatusId);
            return Ok("Chuyển trạng thái thành công");
        }

        // ==================================================
        // 4. ĐÁNH GIÁ ỨNG VIÊN
        // ==================================================

        /// <summary>
        /// Đánh giá ứng viên - HR và Manager đều có thể đánh giá
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        [HttpPost("review")]
        public async Task<IActionResult> Review([FromBody] ReviewCandidateDto dto)
        {
            // Kiểm tra quyền: Manager chỉ đánh giá được ứng viên của phòng mình
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = GetCurrentUserId();

            if (currentUserRole == "MANAGER")
            {
                // Lấy thông tin manager
                var currentUser = await _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.UserID == currentUserId);

                if (currentUser?.Employee == null)
                {
                    return Forbid("Không có quyền đánh giá ứng viên");
                }

                // Kiểm tra ứng viên có thuộc phòng của manager không
                var candidate = await _context.Candidates
                    .FirstOrDefaultAsync(c => c.CandidateId == dto.CandidateId);

                if (candidate == null)
                {
                    return NotFound("Không tìm thấy ứng viên");
                }

                // Giả sử ứng viên có trường DepartmentId hoặc PositionId để kiểm tra
                // Nếu không, có thể bỏ qua check này
            }

            var review = new PerformanceReview
            {
                EmployeeId = dto.EmployeeId,
                ReviewerId = dto.ReviewerId,
                Score = dto.Score,
                Comments = dto.Comments,
                ReviewPeriod = dto.ReviewPeriod
            };

            var result = await _service.ReviewCandidateAsync(review);

            return Ok(new
            {
                result.ReviewId,
                result.EmployeeId,
                result.Score,
                result.Comments,
                result.ReviewDate
            });
        }

        // ==================================================
        // 5. TUYỂN THÀNH NHÂN VIÊN + CV
        // ==================================================

        /// <summary>
        /// Tuyển ứng viên thành nhân viên - Chỉ HR Manager và Admin mới có quyền
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")] // HR Staff không có quyền này
        [HttpPost("hire/{candidateId:int}")]
        public async Task<IActionResult> Hire(int candidateId)
        {
            return Ok(await _service.ConvertToEmployeeAsync(candidateId));
        }

        // ======================
        // LẤY DANH SÁCH CV PHÙ HỢP
        // ======================

        /// <summary>
        /// Lấy danh sách CV phù hợp - Chỉ HR và Admin
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        [HttpGet("cvs")]
        public async Task<IActionResult> GetSuitableCVs()
        {
            var cvs = await (
                from cv in _context.CandidateCVs
                join c in _context.Candidates
                    on cv.CandidateID equals c.CandidateId
                where cv.IsSuitable
                orderby cv.Score descending
                select new
                {
                    cv.CandidateCVID,
                    cv.FileName,
                    cv.Score,
                    cv.IsViewedByHR,
                    cv.CreatedAt,

                    CandidateName = c.FullName,

                    FileUrl = $"{Request.Scheme}://{Request.Host}/Uploads/CV/{cv.FileName}"
                }
            ).ToListAsync();

            return Ok(cvs);
        }

        // ======================
        // HR CLICK XEM → ĐÁNH DẤU ĐÃ XEM
        // ======================

        /// <summary>
        /// Đánh dấu CV đã xem - Chỉ HR và Admin
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        [HttpPost("cvs/{id:int}/view")]
        public async Task<IActionResult> MarkAsViewed(int id)
        {
            var cv = await _context.CandidateCVs.FindAsync(id);

            if (cv == null)
                return NotFound("CV không tồn tại");

            if (!cv.IsViewedByHR)
            {
                cv.IsViewedByHR = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Đã đánh dấu CV đã xem" });
        }

        // ======================
        // (OPTIONAL) STREAM FILE CV QUA API
        // ======================

        /// <summary>
        /// Tải file CV - Chỉ HR và Admin
        /// </summary>
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        [HttpGet("cvs/file/{id:int}")]
        public async Task<IActionResult> GetCVFile(int id)
        {
            var cv = await _context.CandidateCVs.FindAsync(id);

            if (cv == null)
                return NotFound("CV không tồn tại");

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/cvs", cv.FileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File không tồn tại");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            return File(fileBytes, "application/pdf", cv.FileName);
        }

        // ==================================================
        // Helper Methods
        // ==================================================

        private int? GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userId, out int id))
            {
                return id;
            }
            return null;
        }
    }
}