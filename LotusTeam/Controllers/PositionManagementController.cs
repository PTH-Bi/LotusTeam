using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class PositionManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PositionManagementController> _logger;

        public PositionManagementController(AppDbContext context, ILogger<PositionManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =====================================================
        // QUẢN LÝ POSITION (JOB POSITION)
        // =====================================================

        /// <summary>
        /// Lấy danh sách tất cả vị trí công việc (Job Positions)
        /// </summary>
        /// <returns>Danh sách vị trí công việc</returns>
        [HttpGet("positions")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,RECRUITER,INTERVIEWER,MANAGER")]
        public async Task<IActionResult> GetAllPositions()
        {
            try
            {
                var positions = await _context.JobPositions
                    .Include(p => p.JobSkills)
                    .Where(p => p.IsActive)
                    .Select(p => new
                    {
                        p.JobPositionId,
                        p.PositionName,
                        p.Description,
                        p.MinScoreRequired,
                        p.IsActive,
                        p.CreatedAt,
                        Skills = p.JobSkills.Where(s => s.IsActive).Select(s => new
                        {
                            s.JobSkillId,
                            s.SkillName,
                            s.Weight,
                            s.IsRequired
                        })
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Lấy danh sách vị trí công việc thành công",
                    Data = positions,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách vị trí công việc");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách vị trí công việc",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một vị trí công việc theo ID
        /// </summary>
        /// <param name="id">ID vị trí công việc</param>
        /// <returns>Thông tin chi tiết vị trí</returns>
        [HttpGet("positions/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,RECRUITER,INTERVIEWER,MANAGER")]
        public async Task<IActionResult> GetPositionById(int id)
        {
            try
            {
                var position = await _context.JobPositions
                    .Include(p => p.JobSkills)
                    .FirstOrDefaultAsync(p => p.JobPositionId == id);

                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy vị trí công việc với ID {id}",
                        StatusCode = 404
                    });
                }

                return Ok(new ApiResponse<JobPosition>
                {
                    Success = true,
                    Message = "Lấy thông tin vị trí công việc thành công",
                    Data = position,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy vị trí công việc ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin vị trí",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Tạo mới vị trí công việc (Chỉ HR và Admin)
        /// </summary>
        /// <param name="request">Thông tin vị trí mới</param>
        /// <returns>Vị trí vừa tạo</returns>
        [HttpPost("positions")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<IActionResult> CreatePosition([FromBody] CreatePositionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    StatusCode = 400
                });
            }

            try
            {
                // Kiểm tra trùng tên
                var exists = await _context.JobPositions
                    .AnyAsync(p => p.PositionName == request.PositionName);

                if (exists)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Tên vị trí công việc đã tồn tại",
                        StatusCode = 400
                    });
                }

                var position = new JobPosition
                {
                    PositionName = request.PositionName,
                    Description = request.Description,
                    MinScoreRequired = request.MinScoreRequired,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.JobPositions.Add(position);
                await _context.SaveChangesAsync();

                // Thêm skills nếu có
                if (request.Skills != null && request.Skills.Any())
                {
                    foreach (var skillDto in request.Skills)
                    {
                        var skill = new JobSkill
                        {
                            JobPositionId = position.JobPositionId,
                            SkillName = skillDto.SkillName,
                            Weight = skillDto.Weight,
                            IsRequired = skillDto.IsRequired,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };
                        _context.JobSkills.Add(skill);
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Tạo vị trí công việc thành công",
                    Data = new { positionId = position.JobPositionId, position },
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo vị trí công việc");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo vị trí công việc",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin vị trí công việc (Chỉ HR và Admin)
        /// </summary>
        /// <param name="id">ID vị trí</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("positions/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<IActionResult> UpdatePosition(int id, [FromBody] UpdatePositionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    StatusCode = 400
                });
            }

            try
            {
                var position = await _context.JobPositions.FindAsync(id);
                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy vị trí công việc với ID {id}",
                        StatusCode = 404
                    });
                }

                // Kiểm tra trùng tên (trừ chính nó)
                var exists = await _context.JobPositions
                    .AnyAsync(p => p.PositionName == request.PositionName && p.JobPositionId != id);

                if (exists)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Tên vị trí công việc đã tồn tại",
                        StatusCode = 400
                    });
                }

                position.PositionName = request.PositionName;
                position.Description = request.Description;
                position.MinScoreRequired = request.MinScoreRequired;
                position.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Cập nhật vị trí công việc thành công",
                    Data = position,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật vị trí công việc ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật vị trí công việc",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Xóa vị trí công việc (Chỉ Admin cấp cao)
        /// </summary>
        /// <param name="id">ID vị trí</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("positions/{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<IActionResult> DeletePosition(int id)
        {
            try
            {
                var position = await _context.JobPositions
                    .Include(p => p.JobSkills)
                    .FirstOrDefaultAsync(p => p.JobPositionId == id);

                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy vị trí công việc với ID {id}",
                        StatusCode = 404
                    });
                }

                // Kiểm tra xem đã có candidate match chưa
                var hasMatches = await _context.CandidatePositionMatches
                    .AnyAsync(m => m.JobPositionId == id);

                if (hasMatches)
                {
                    // Soft delete - chỉ đánh dấu inactive
                    position.IsActive = false;
                    position.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Vị trí công việc đã được vô hiệu hóa (có dữ liệu match tồn tại)",
                        Data = position,
                        StatusCode = 200
                    });
                }

                // Hard delete - xóa hoàn toàn
                _context.JobPositions.Remove(position);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa vị trí công việc thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa vị trí công việc ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa vị trí công việc",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =====================================================
        // QUẢN LÝ SKILLS
        // =====================================================

        /// <summary>
        /// Lấy danh sách kỹ năng theo vị trí công việc
        /// </summary>
        /// <param name="positionId">ID vị trí</param>
        /// <returns>Danh sách kỹ năng</returns>
        [HttpGet("positions/{positionId}/skills")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,RECRUITER,INTERVIEWER")]
        public async Task<IActionResult> GetSkillsByPosition(int positionId)
        {
            try
            {
                var skills = await _context.JobSkills
                    .Where(s => s.JobPositionId == positionId && s.IsActive)
                    .ToListAsync();

                return Ok(new ApiResponse<List<JobSkill>>
                {
                    Success = true,
                    Message = "Lấy danh sách kỹ năng thành công",
                    Data = skills,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy kỹ năng cho vị trí {PositionId}", positionId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách kỹ năng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Thêm kỹ năng mới cho vị trí công việc (Chỉ HR)
        /// </summary>
        /// <param name="request">Thông tin kỹ năng</param>
        /// <returns>Kỹ năng vừa tạo</returns>
        [HttpPost("skills")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<IActionResult> AddSkill([FromBody] CreateSkillRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    StatusCode = 400
                });
            }

            try
            {
                var position = await _context.JobPositions.FindAsync(request.JobPositionId);
                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy vị trí công việc với ID {request.JobPositionId}",
                        StatusCode = 404
                    });
                }

                // Kiểm tra trùng skill
                var exists = await _context.JobSkills
                    .AnyAsync(s => s.JobPositionId == request.JobPositionId &&
                                   s.SkillName == request.SkillName &&
                                   s.IsActive);

                if (exists)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Kỹ năng này đã tồn tại cho vị trí công việc",
                        StatusCode = 400
                    });
                }

                var skill = new JobSkill
                {
                    JobPositionId = request.JobPositionId,
                    SkillName = request.SkillName,
                    Weight = request.Weight,
                    IsRequired = request.IsRequired,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.JobSkills.Add(skill);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<JobSkill>
                {
                    Success = true,
                    Message = "Thêm kỹ năng thành công",
                    Data = skill,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm kỹ năng");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi thêm kỹ năng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin kỹ năng (Chỉ HR)
        /// </summary>
        /// <param name="skillId">ID kỹ năng</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("skills/{skillId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<IActionResult> UpdateSkill(int skillId, [FromBody] UpdateSkillRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    StatusCode = 400
                });
            }

            try
            {
                var skill = await _context.JobSkills.FindAsync(skillId);
                if (skill == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy kỹ năng với ID {skillId}",
                        StatusCode = 404
                    });
                }

                skill.SkillName = request.SkillName;
                skill.Weight = request.Weight;
                skill.IsRequired = request.IsRequired;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<JobSkill>
                {
                    Success = true,
                    Message = "Cập nhật kỹ năng thành công",
                    Data = skill,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật kỹ năng ID {SkillId}", skillId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật kỹ năng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Xóa kỹ năng (soft delete) - Chỉ HR
        /// </summary>
        /// <param name="skillId">ID kỹ năng</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("skills/{skillId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<IActionResult> DeleteSkill(int skillId)
        {
            try
            {
                var skill = await _context.JobSkills.FindAsync(skillId);
                if (skill == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy kỹ năng với ID {skillId}",
                        StatusCode = 404
                    });
                }

                // Soft delete
                skill.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa kỹ năng thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa kỹ năng ID {SkillId}", skillId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa kỹ năng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // =====================================================
        // BULK IMPORT (Import nhiều position cùng lúc)
        // =====================================================

        /// <summary>
        /// Import hàng loạt vị trí công việc (Chỉ HR Manager)
        /// </summary>
        /// <param name="requests">Danh sách vị trí</param>
        /// <returns>Kết quả import</returns>
        [HttpPost("bulk-import")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<IActionResult> BulkImport([FromBody] List<CreatePositionRequest> requests)
        {
            if (requests == null || !requests.Any())
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Danh sách vị trí không được để trống",
                    StatusCode = 400
                });
            }

            var results = new List<object>();
            int successCount = 0;
            int failCount = 0;

            foreach (var request in requests)
            {
                try
                {
                    // Kiểm tra trùng tên
                    var exists = await _context.JobPositions
                        .AnyAsync(p => p.PositionName == request.PositionName);

                    if (exists)
                    {
                        results.Add(new { request.PositionName, status = "skipped", reason = "Đã tồn tại" });
                        failCount++;
                        continue;
                    }

                    var position = new JobPosition
                    {
                        PositionName = request.PositionName,
                        Description = request.Description,
                        MinScoreRequired = request.MinScoreRequired,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    _context.JobPositions.Add(position);
                    await _context.SaveChangesAsync();

                    // Thêm skills
                    if (request.Skills != null && request.Skills.Any())
                    {
                        foreach (var skillDto in request.Skills)
                        {
                            var skill = new JobSkill
                            {
                                JobPositionId = position.JobPositionId,
                                SkillName = skillDto.SkillName,
                                Weight = skillDto.Weight,
                                IsRequired = skillDto.IsRequired,
                                IsActive = true,
                                CreatedAt = DateTime.Now
                            };
                            _context.JobSkills.Add(skill);
                        }
                        await _context.SaveChangesAsync();
                    }

                    results.Add(new { request.PositionName, status = "success", positionId = position.JobPositionId });
                    successCount++;
                }
                catch (Exception ex)
                {
                    results.Add(new { request.PositionName, status = "failed", error = ex.Message });
                    failCount++;
                }
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = $"Import hoàn tất: {successCount} thành công, {failCount} thất bại",
                Data = new
                {
                    total = requests.Count,
                    success = successCount,
                    failed = failCount,
                    details = results
                },
                StatusCode = 200
            });
        }
    }

    // =====================================================
    // REQUEST MODELS (giữ nguyên)
    // =====================================================

    public class CreatePositionRequest
    {
        public string PositionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MinScoreRequired { get; set; } = 40;
        public List<SkillRequest>? Skills { get; set; }
    }

    public class UpdatePositionRequest
    {
        public string PositionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MinScoreRequired { get; set; } = 40;
    }

    public class SkillRequest
    {
        public string SkillName { get; set; } = string.Empty;
        public int Weight { get; set; } = 20;
        public bool IsRequired { get; set; } = false;
    }

    public class CreateSkillRequest
    {
        public int JobPositionId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public int Weight { get; set; } = 20;
        public bool IsRequired { get; set; } = false;
    }

    public class UpdateSkillRequest
    {
        public string SkillName { get; set; } = string.Empty;
        public int Weight { get; set; } = 20;
        public bool IsRequired { get; set; } = false;
    }
}