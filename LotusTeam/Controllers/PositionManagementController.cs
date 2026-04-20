// Controllers/PositionManagementController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        // QUẢN LÝ POSITION
        // =====================================================

        // GET: api/PositionManagement/positions
        [HttpGet("positions")]
        public async Task<IActionResult> GetAllPositions()
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

            return Ok(positions);
        }

        // GET: api/PositionManagement/positions/{id}
        [HttpGet("positions/{id}")]
        public async Task<IActionResult> GetPositionById(int id)
        {
            var position = await _context.JobPositions
                .Include(p => p.JobSkills)
                .FirstOrDefaultAsync(p => p.JobPositionId == id);

            if (position == null)
                return NotFound(new { message = "Position not found" });

            return Ok(position);
        }

        // POST: api/PositionManagement/positions
        [HttpPost("positions")]
        public async Task<IActionResult> CreatePosition([FromBody] CreatePositionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Kiểm tra trùng tên
            var exists = await _context.JobPositions
                .AnyAsync(p => p.PositionName == request.PositionName);

            if (exists)
                return BadRequest(new { message = "Position name already exists" });

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

            return Ok(new
            {
                message = "Position created successfully",
                positionId = position.JobPositionId,
                position
            });
        }

        // PUT: api/PositionManagement/positions/{id}
        [HttpPut("positions/{id}")]
        public async Task<IActionResult> UpdatePosition(int id, [FromBody] UpdatePositionRequest request)
        {
            var position = await _context.JobPositions.FindAsync(id);
            if (position == null)
                return NotFound(new { message = "Position not found" });

            // Kiểm tra trùng tên (trừ chính nó)
            var exists = await _context.JobPositions
                .AnyAsync(p => p.PositionName == request.PositionName && p.JobPositionId != id);

            if (exists)
                return BadRequest(new { message = "Position name already exists" });

            position.PositionName = request.PositionName;
            position.Description = request.Description;
            position.MinScoreRequired = request.MinScoreRequired;
            position.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Position updated successfully", position });
        }

        // DELETE: api/PositionManagement/positions/{id}
        [HttpDelete("positions/{id}")]
        public async Task<IActionResult> DeletePosition(int id)
        {
            var position = await _context.JobPositions
                .Include(p => p.JobSkills)
                .FirstOrDefaultAsync(p => p.JobPositionId == id);

            if (position == null)
                return NotFound(new { message = "Position not found" });

            // Kiểm tra xem đã có candidate match chưa
            var hasMatches = await _context.CandidatePositionMatches
                .AnyAsync(m => m.JobPositionId == id);

            if (hasMatches)
            {
                // Soft delete - chỉ đánh dấu inactive
                position.IsActive = false;
                position.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Position deactivated (has existing matches)", position });
            }

            // Hard delete - xóa hoàn toàn
            _context.JobPositions.Remove(position);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Position deleted successfully" });
        }

        // =====================================================
        // QUẢN LÝ SKILLS
        // =====================================================

        // GET: api/PositionManagement/positions/{positionId}/skills
        [HttpGet("positions/{positionId}/skills")]
        public async Task<IActionResult> GetSkillsByPosition(int positionId)
        {
            var skills = await _context.JobSkills
                .Where(s => s.JobPositionId == positionId && s.IsActive)
                .ToListAsync();

            return Ok(skills);
        }

        // POST: api/PositionManagement/skills
        [HttpPost("skills")]
        public async Task<IActionResult> AddSkill([FromBody] CreateSkillRequest request)
        {
            var position = await _context.JobPositions.FindAsync(request.JobPositionId);
            if (position == null)
                return NotFound(new { message = "Position not found" });

            // Kiểm tra trùng skill
            var exists = await _context.JobSkills
                .AnyAsync(s => s.JobPositionId == request.JobPositionId && s.SkillName == request.SkillName);

            if (exists)
                return BadRequest(new { message = "Skill already exists for this position" });

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

            return Ok(new { message = "Skill added successfully", skill });
        }

        // PUT: api/PositionManagement/skills/{skillId}
        [HttpPut("skills/{skillId}")]
        public async Task<IActionResult> UpdateSkill(int skillId, [FromBody] UpdateSkillRequest request)
        {
            var skill = await _context.JobSkills.FindAsync(skillId);
            if (skill == null)
                return NotFound(new { message = "Skill not found" });

            skill.SkillName = request.SkillName;
            skill.Weight = request.Weight;
            skill.IsRequired = request.IsRequired;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Skill updated successfully", skill });
        }

        // DELETE: api/PositionManagement/skills/{skillId}
        [HttpDelete("skills/{skillId}")]
        public async Task<IActionResult> DeleteSkill(int skillId)
        {
            var skill = await _context.JobSkills.FindAsync(skillId);
            if (skill == null)
                return NotFound(new { message = "Skill not found" });

            // Soft delete
            skill.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Skill deleted successfully" });
        }

        // =====================================================
        // BULK IMPORT (Import nhiều position cùng lúc)
        // =====================================================

        // POST: api/PositionManagement/bulk-import
        [HttpPost("bulk-import")]
        public async Task<IActionResult> BulkImport([FromBody] List<CreatePositionRequest> requests)
        {
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
                        results.Add(new { request.PositionName, status = "skipped", reason = "Already exists" });
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

            return Ok(new
            {
                total = requests.Count,
                success = successCount,
                failed = failCount,
                details = results
            });
        }
    }

    // Request Models
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