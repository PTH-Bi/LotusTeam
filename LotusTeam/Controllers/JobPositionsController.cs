using Microsoft.AspNetCore.Mvc;
using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace LotusTeam.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ADMIN,HR")]
    public class JobPositionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<JobPositionsController> _logger;

        public JobPositionsController(AppDbContext context, ILogger<JobPositionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/JobPositions
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<JobPositionDto>>>> GetJobPositions(
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                var query = _context.JobPositions
                    .Include(jp => jp.JobSkills)
                    .AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(jp => jp.IsActive);
                }

                var positions = await query
                    .OrderBy(jp => jp.PositionName)
                    .ToListAsync();

                var positionDtos = positions.Select(p => new JobPositionDto
                {
                    JobPositionId = p.JobPositionId,
                    PositionName = p.PositionName,
                    Description = p.Description,
                    MinScoreRequired = p.MinScoreRequired,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    SkillCount = p.JobSkills.Count(s => s.IsActive),
                    Skills = p.JobSkills.Where(s => s.IsActive).Select(s => new JobSkillDto
                    {
                        JobSkillId = s.JobSkillId,
                        SkillName = s.SkillName,
                        Weight = s.Weight,
                        IsRequired = s.IsRequired
                    }).ToList()
                }).ToList();

                return Ok(new ApiResponse<IEnumerable<JobPositionDto>>
                {
                    Success = true,
                    Data = positionDtos,
                    Message = "Danh sách vị trí tuyển dụng"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job positions");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách vị trí tuyển dụng"
                });
            }
        }

        // GET: api/JobPositions/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<JobPositionDetailDto>>> GetJobPosition(int id)
        {
            try
            {
                var position = await _context.JobPositions
                    .Include(jp => jp.JobSkills.Where(s => s.IsActive))
                    .Include(jp => jp.CandidatePositionMatches)
                        .ThenInclude(cpm => cpm.Candidate)
                    .FirstOrDefaultAsync(jp => jp.JobPositionId == id);

                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vị trí tuyển dụng với ID {id} không tồn tại"
                    });
                }

                // Thống kê candidates cho vị trí này
                var candidateStats = new PositionCandidateStats
                {
                    TotalCandidates = position.CandidatePositionMatches.Count,
                    SuitableCandidates = position.CandidatePositionMatches.Count(cpm => cpm.IsSuitable),
                    AverageScore = position.CandidatePositionMatches.Any()
                        ? position.CandidatePositionMatches.Average(cpm => cpm.TotalScore)
                        : 0,
                    TopCandidates = position.CandidatePositionMatches
                        .Where(cpm => cpm.IsSuitable)
                        .OrderByDescending(cpm => cpm.TotalScore)
                        .Take(5)
                        .Select(cpm => new TopCandidateDto
                        {
                            CandidateId = cpm.CandidateId,
                            FullName = cpm.Candidate.FullName,
                            Email = cpm.Candidate.Email,
                            Score = cpm.TotalScore,
                            MatchedAt = cpm.MatchedAt
                        }).ToList()
                };

                var positionDetail = new JobPositionDetailDto
                {
                    JobPositionId = position.JobPositionId,
                    PositionName = position.PositionName,
                    Description = position.Description,
                    MinScoreRequired = position.MinScoreRequired,
                    IsActive = position.IsActive,
                    CreatedAt = position.CreatedAt,
                    UpdatedAt = position.UpdatedAt,
                    Skills = position.JobSkills.Select(s => new JobSkillDto
                    {
                        JobSkillId = s.JobSkillId,
                        SkillName = s.SkillName,
                        Weight = s.Weight,
                        IsRequired = s.IsRequired
                    }).ToList(),
                    Statistics = candidateStats
                };

                return Ok(new ApiResponse<JobPositionDetailDto>
                {
                    Success = true,
                    Data = positionDetail,
                    Message = "Thông tin vị trí tuyển dụng"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job position with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin vị trí tuyển dụng"
                });
            }
        }

        // POST: api/JobPositions
        [HttpPost]
        public async Task<ActionResult<ApiResponse<JobPositionDto>>> CreateJobPosition(CreateJobPositionDto createDto)
        {
            try
            {
                // Check if position name exists
                var existingPosition = await _context.JobPositions
                    .FirstOrDefaultAsync(jp => jp.PositionName == createDto.PositionName);

                if (existingPosition != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vị trí '{createDto.PositionName}' đã tồn tại"
                    });
                }

                var position = new JobPosition
                {
                    PositionName = createDto.PositionName,
                    Description = createDto.Description,
                    MinScoreRequired = createDto.MinScoreRequired,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.JobPositions.Add(position);
                await _context.SaveChangesAsync();

                // Thêm skills nếu có
                if (createDto.Skills != null && createDto.Skills.Any())
                {
                    foreach (var skillDto in createDto.Skills)
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

                var positionDto = new JobPositionDto
                {
                    JobPositionId = position.JobPositionId,
                    PositionName = position.PositionName,
                    Description = position.Description,
                    MinScoreRequired = position.MinScoreRequired,
                    IsActive = position.IsActive,
                    CreatedAt = position.CreatedAt,
                    SkillCount = createDto.Skills?.Count ?? 0,
                    Skills = createDto.Skills?.Select(s => new JobSkillDto
                    {
                        SkillName = s.SkillName,
                        Weight = s.Weight,
                        IsRequired = s.IsRequired
                    }).ToList() ?? new List<JobSkillDto>()
                };

                return CreatedAtAction(nameof(GetJobPosition), new { id = position.JobPositionId },
                    new ApiResponse<JobPositionDto>
                    {
                        Success = true,
                        Data = positionDto,
                        Message = "Tạo vị trí tuyển dụng thành công"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job position");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo vị trí tuyển dụng"
                });
            }
        }

        // PUT: api/JobPositions/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<JobPositionDto>>> UpdateJobPosition(int id, UpdateJobPositionDto updateDto)
        {
            try
            {
                var position = await _context.JobPositions.FindAsync(id);

                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vị trí tuyển dụng với ID {id} không tồn tại"
                    });
                }

                // Check if new position name conflicts
                if (!string.IsNullOrEmpty(updateDto.PositionName) &&
                    updateDto.PositionName != position.PositionName)
                {
                    var existingName = await _context.JobPositions
                        .FirstOrDefaultAsync(jp => jp.PositionName == updateDto.PositionName &&
                                                  jp.JobPositionId != id);

                    if (existingName != null)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Tên vị trí '{updateDto.PositionName}' đã tồn tại"
                        });
                    }
                }

                // Update properties
                if (!string.IsNullOrEmpty(updateDto.PositionName))
                    position.PositionName = updateDto.PositionName;

                if (updateDto.Description != null)
                    position.Description = updateDto.Description;

                if (updateDto.MinScoreRequired.HasValue)
                    position.MinScoreRequired = updateDto.MinScoreRequired.Value;

                if (updateDto.IsActive.HasValue)
                    position.IsActive = updateDto.IsActive.Value;

                position.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var positionDto = new JobPositionDto
                {
                    JobPositionId = position.JobPositionId,
                    PositionName = position.PositionName,
                    Description = position.Description,
                    MinScoreRequired = position.MinScoreRequired,
                    IsActive = position.IsActive,
                    CreatedAt = position.CreatedAt,
                    UpdatedAt = position.UpdatedAt
                };

                return Ok(new ApiResponse<JobPositionDto>
                {
                    Success = true,
                    Data = positionDto,
                    Message = "Cập nhật vị trí tuyển dụng thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job position with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật vị trí tuyển dụng"
                });
            }
        }

        // DELETE: api/JobPositions/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteJobPosition(int id)
        {
            try
            {
                var position = await _context.JobPositions
                    .Include(jp => jp.CandidatePositionMatches)
                    .FirstOrDefaultAsync(jp => jp.JobPositionId == id);

                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vị trí tuyển dụng với ID {id} không tồn tại"
                    });
                }

                // Check if position has candidate matches
                if (position.CandidatePositionMatches.Any())
                {
                    // Soft delete - chỉ đánh dấu inactive
                    position.IsActive = false;
                    position.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Vị trí tuyển dụng đã được vô hiệu hóa (đã có ứng viên)"
                    });
                }

                // Hard delete - xóa hoàn toàn
                _context.JobPositions.Remove(position);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa vị trí tuyển dụng thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job position with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa vị trí tuyển dụng"
                });
            }
        }

        // =====================================================
        // QUẢN LÝ SKILLS CHO JOB POSITION
        // =====================================================

        // GET: api/JobPositions/{positionId}/skills
        [HttpGet("{positionId}/skills")]
        public async Task<ActionResult<ApiResponse<IEnumerable<JobSkillDto>>>> GetSkillsByPosition(int positionId)
        {
            try
            {
                var position = await _context.JobPositions
                    .FirstOrDefaultAsync(jp => jp.JobPositionId == positionId);

                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vị trí tuyển dụng với ID {positionId} không tồn tại"
                    });
                }

                var skills = await _context.JobSkills
                    .Where(s => s.JobPositionId == positionId && s.IsActive)
                    .Select(s => new JobSkillDto
                    {
                        JobSkillId = s.JobSkillId,
                        SkillName = s.SkillName,
                        Weight = s.Weight,
                        IsRequired = s.IsRequired
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<JobSkillDto>>
                {
                    Success = true,
                    Data = skills,
                    Message = "Danh sách kỹ năng"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving skills for position {PositionId}", positionId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách kỹ năng"
                });
            }
        }

        // POST: api/JobPositions/{positionId}/skills
        [HttpPost("{positionId}/skills")]
        public async Task<ActionResult<ApiResponse<JobSkillDto>>> AddSkillToPosition(int positionId, CreateJobSkillDto createDto)
        {
            try
            {
                var position = await _context.JobPositions
                    .FirstOrDefaultAsync(jp => jp.JobPositionId == positionId);

                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vị trí tuyển dụng với ID {positionId} không tồn tại"
                    });
                }

                // Check if skill already exists
                var existingSkill = await _context.JobSkills
                    .FirstOrDefaultAsync(s => s.JobPositionId == positionId &&
                                              s.SkillName == createDto.SkillName);

                if (existingSkill != null)
                {
                    if (existingSkill.IsActive)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Kỹ năng '{createDto.SkillName}' đã tồn tại cho vị trí này"
                        });
                    }
                    else
                    {
                        // Reactivate existing skill
                        existingSkill.IsActive = true;
                        existingSkill.Weight = createDto.Weight;
                        existingSkill.IsRequired = createDto.IsRequired;
                        await _context.SaveChangesAsync();

                        var skillDto = new JobSkillDto
                        {
                            JobSkillId = existingSkill.JobSkillId,
                            SkillName = existingSkill.SkillName,
                            Weight = existingSkill.Weight,
                            IsRequired = existingSkill.IsRequired
                        };

                        return Ok(new ApiResponse<JobSkillDto>
                        {
                            Success = true,
                            Data = skillDto,
                            Message = "Kích hoạt lại kỹ năng thành công"
                        });
                    }
                }

                var skill = new JobSkill
                {
                    JobPositionId = positionId,
                    SkillName = createDto.SkillName,
                    Weight = createDto.Weight,
                    IsRequired = createDto.IsRequired,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.JobSkills.Add(skill);
                await _context.SaveChangesAsync();

                var newSkillDto = new JobSkillDto
                {
                    JobSkillId = skill.JobSkillId,
                    SkillName = skill.SkillName,
                    Weight = skill.Weight,
                    IsRequired = skill.IsRequired
                };

                return Ok(new ApiResponse<JobSkillDto>
                {
                    Success = true,
                    Data = newSkillDto,
                    Message = "Thêm kỹ năng thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding skill to position {PositionId}", positionId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi thêm kỹ năng"
                });
            }
        }

        // PUT: api/JobPositions/skills/{skillId}
        [HttpPut("skills/{skillId}")]
        public async Task<ActionResult<ApiResponse<JobSkillDto>>> UpdateSkill(int skillId, UpdateJobSkillDto updateDto)
        {
            try
            {
                var skill = await _context.JobSkills.FindAsync(skillId);

                if (skill == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Kỹ năng với ID {skillId} không tồn tại"
                    });
                }

                if (!string.IsNullOrEmpty(updateDto.SkillName))
                    skill.SkillName = updateDto.SkillName;

                if (updateDto.Weight.HasValue)
                    skill.Weight = updateDto.Weight.Value;

                if (updateDto.IsRequired.HasValue)
                    skill.IsRequired = updateDto.IsRequired.Value;

                await _context.SaveChangesAsync();

                var skillDto = new JobSkillDto
                {
                    JobSkillId = skill.JobSkillId,
                    SkillName = skill.SkillName,
                    Weight = skill.Weight,
                    IsRequired = skill.IsRequired
                };

                return Ok(new ApiResponse<JobSkillDto>
                {
                    Success = true,
                    Data = skillDto,
                    Message = "Cập nhật kỹ năng thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating skill {SkillId}", skillId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật kỹ năng"
                });
            }
        }

        // DELETE: api/JobPositions/skills/{skillId}
        [HttpDelete("skills/{skillId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteSkill(int skillId)
        {
            try
            {
                var skill = await _context.JobSkills.FindAsync(skillId);

                if (skill == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Kỹ năng với ID {skillId} không tồn tại"
                    });
                }

                // Soft delete
                skill.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa kỹ năng thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting skill {SkillId}", skillId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa kỹ năng"
                });
            }
        }
    }

    // ========== DTO Classes cho JobPosition ==========

    public class JobPositionDto
    {
        public int JobPositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MinScoreRequired { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int SkillCount { get; set; }
        public List<JobSkillDto> Skills { get; set; } = new();
    }

    public class JobPositionDetailDto : JobPositionDto
    {
        public PositionCandidateStats Statistics { get; set; } = new();
    }

    public class JobSkillDto
    {
        public int JobSkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public int Weight { get; set; }
        public bool IsRequired { get; set; }
    }

    public class CreateJobPositionDto
    {
        [Required]
        [StringLength(100)]
        public string PositionName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 100)]
        public int MinScoreRequired { get; set; } = 40;

        public List<CreateJobSkillDto>? Skills { get; set; }
    }

    public class UpdateJobPositionDto
    {
        [StringLength(100)]
        public string? PositionName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 100)]
        public int? MinScoreRequired { get; set; }

        public bool? IsActive { get; set; }
    }

    public class CreateJobSkillDto
    {
        [Required]
        [StringLength(100)]
        public string SkillName { get; set; } = string.Empty;

        [Range(1, 50)]
        public int Weight { get; set; } = 20;

        public bool IsRequired { get; set; } = false;
    }

    public class UpdateJobSkillDto
    {
        [StringLength(100)]
        public string? SkillName { get; set; }

        [Range(1, 50)]
        public int? Weight { get; set; }

        public bool? IsRequired { get; set; }
    }

    // ========== Statistics DTOs ==========

    public class PositionCandidateStats
    {
        public int TotalCandidates { get; set; }
        public int SuitableCandidates { get; set; }
        public double AverageScore { get; set; }
        public List<TopCandidateDto> TopCandidates { get; set; } = new();
    }

    public class TopCandidateDto
    {
        public int CandidateId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int Score { get; set; }
        public DateTime MatchedAt { get; set; }
    }

    // ========== Common ApiResponse ==========

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public PaginationInfo Pagination { get; internal set; }
    }
}