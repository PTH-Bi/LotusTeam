using Microsoft.AspNetCore.Mvc;
using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using LotusTeam.DTOs;
using LotusTeam.Service;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace LotusTeam.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ADMIN,HR")]
    public class WorkTypesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WorkTypesController> _logger;

        public WorkTypesController(AppDbContext context, ILogger<WorkTypesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<WorkTypeDto>>>> GetWorkTypes()
        {
            try
            {
                var workTypes = await _context.WorkTypes
                    .Where(w => w.IsActive)
                    .OrderBy(w => w.WorkTypeName)
                    .ToListAsync();

                var workTypeDtos = workTypes.Select(w => new WorkTypeDto
                {
                    WorkTypeId = w.WorkTypeID,
                    WorkTypeCode = w.WorkTypeCode,
                    WorkTypeName = w.WorkTypeName,
                    Description = w.Description,
                    IsActive = w.IsActive
                }).ToList();

                return Ok(new ApiResponse<IEnumerable<WorkTypeDto>>
                {
                    Success = true,
                    Data = workTypeDtos,
                    Message = "Danh sách loại hình làm việc"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving work types");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách loại hình làm việc"
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<WorkTypeDto>>> CreateWorkType(CreateWorkTypeDto createDto)
        {
            try
            {
                // Check if work type code exists
                var existingWorkType = await _context.WorkTypes
                    .FirstOrDefaultAsync(w => w.WorkTypeCode == createDto.WorkTypeCode);

                if (existingWorkType != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Mã loại hình làm việc '{createDto.WorkTypeCode}' đã tồn tại"
                    });
                }

                var workType = new WorkType
                {
                    WorkTypeCode = createDto.WorkTypeCode,
                    WorkTypeName = createDto.WorkTypeName,
                    Description = createDto.Description,
                    IsActive = true
                };

                _context.WorkTypes.Add(workType);
                await _context.SaveChangesAsync();

                var workTypeDto = new WorkTypeDto
                {
                    WorkTypeId = workType.WorkTypeID,
                    WorkTypeCode = workType.WorkTypeCode,
                    WorkTypeName = workType.WorkTypeName,
                    Description = workType.Description,
                    IsActive = workType.IsActive
                };

                return CreatedAtAction(nameof(GetWorkTypes), new { id = workType.WorkTypeID },
                    new ApiResponse<WorkTypeDto>
                    {
                        Success = true,
                        Data = workTypeDto,
                        Message = "Tạo loại hình làm việc thành công"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work type");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo loại hình làm việc"
                });
            }
        }

        [HttpPut("{id}/toggle-active")]
        public async Task<ActionResult<ApiResponse<object>>> ToggleWorkTypeActive(int id)
        {
            try
            {
                var workType = await _context.WorkTypes.FindAsync(id);
                if (workType == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Loại hình làm việc với ID {id} không tồn tại"
                    });
                }

                workType.IsActive = !workType.IsActive;
                await _context.SaveChangesAsync();

                var message = workType.IsActive ? "Kích hoạt loại hình làm việc thành công" : "Vô hiệu hóa loại hình làm việc thành công";

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling work type active with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi thay đổi trạng thái loại hình làm việc"
                });
            }
        }
    }

    public class WorkTypeDto
    {
        public int WorkTypeId { get; set; }
        public string WorkTypeCode { get; set; } = string.Empty;
        public string WorkTypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateWorkTypeDto
    {
        [Required]
        [StringLength(20)]
        public string WorkTypeCode { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string WorkTypeName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }
    }
}