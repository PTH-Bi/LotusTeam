using Microsoft.AspNetCore.Mvc;
using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using LotusTeam.DTOs;
using LotusTeam.Service;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LotusTeam.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class WorkTypesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WorkTypesController> _logger;

        public WorkTypesController(AppDbContext context, ILogger<WorkTypesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách loại hình làm việc (đang hoạt động)
        /// </summary>
        /// <returns>Danh sách loại hình làm việc</returns>
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,EMPLOYEE")]
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
                    Message = "Lấy danh sách loại hình làm việc thành công",
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = workTypeDtos.Count,
                        Page = 1,
                        PageSize = workTypeDtos.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving work types");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách loại hình làm việc",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả loại hình làm việc (kể cả không hoạt động) - Chỉ HR
        /// </summary>
        /// <returns>Danh sách tất cả loại hình làm việc</returns>
        [HttpGet("all")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<ActionResult<ApiResponse<IEnumerable<WorkTypeDto>>>> GetAllWorkTypes()
        {
            try
            {
                var workTypes = await _context.WorkTypes
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
                    Message = "Lấy danh sách tất cả loại hình làm việc thành công",
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = workTypeDtos.Count,
                        Page = 1,
                        PageSize = workTypeDtos.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all work types");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách loại hình làm việc",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết loại hình làm việc theo ID
        /// </summary>
        /// <param name="id">ID loại hình làm việc</param>
        /// <returns>Thông tin chi tiết</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        public async Task<ActionResult<ApiResponse<WorkTypeDto>>> GetWorkTypeById(int id)
        {
            try
            {
                var workType = await _context.WorkTypes
                    .FirstOrDefaultAsync(w => w.WorkTypeID == id);

                if (workType == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Loại hình làm việc với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                var workTypeDto = new WorkTypeDto
                {
                    WorkTypeId = workType.WorkTypeID,
                    WorkTypeCode = workType.WorkTypeCode,
                    WorkTypeName = workType.WorkTypeName,
                    Description = workType.Description,
                    IsActive = workType.IsActive
                };

                return Ok(new ApiResponse<WorkTypeDto>
                {
                    Success = true,
                    Data = workTypeDto,
                    Message = "Lấy thông tin loại hình làm việc thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving work type with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin loại hình làm việc",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Tạo mới loại hình làm việc (Chỉ HR)
        /// </summary>
        /// <param name="createDto">Thông tin loại hình làm việc mới</param>
        /// <returns>Loại hình làm việc vừa tạo</returns>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<ActionResult<ApiResponse<WorkTypeDto>>> CreateWorkType([FromBody] CreateWorkTypeDto createDto)
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
                // Check if work type code exists
                var existingWorkType = await _context.WorkTypes
                    .FirstOrDefaultAsync(w => w.WorkTypeCode == createDto.WorkTypeCode);

                if (existingWorkType != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Mã loại hình làm việc '{createDto.WorkTypeCode}' đã tồn tại",
                        StatusCode = 400
                    });
                }

                // Check if work type name exists
                var existingByName = await _context.WorkTypes
                    .FirstOrDefaultAsync(w => w.WorkTypeName == createDto.WorkTypeName);

                if (existingByName != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Tên loại hình làm việc '{createDto.WorkTypeName}' đã tồn tại",
                        StatusCode = 400
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

                return CreatedAtAction(nameof(GetWorkTypeById), new { id = workType.WorkTypeID },
                    new ApiResponse<WorkTypeDto>
                    {
                        Success = true,
                        Data = workTypeDto,
                        Message = "Tạo loại hình làm việc thành công",
                        StatusCode = 201
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work type");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo loại hình làm việc",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin loại hình làm việc (Chỉ HR)
        /// </summary>
        /// <param name="id">ID loại hình làm việc</param>
        /// <param name="updateDto">Thông tin cập nhật</param>
        /// <returns>Loại hình làm việc sau khi cập nhật</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<ActionResult<ApiResponse<WorkTypeDto>>> UpdateWorkType(int id, [FromBody] UpdateWorkTypeDto updateDto)
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
                var workType = await _context.WorkTypes.FindAsync(id);
                if (workType == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Loại hình làm việc với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                // Check if new code conflicts (excluding current)
                if (!string.IsNullOrEmpty(updateDto.WorkTypeCode) && updateDto.WorkTypeCode != workType.WorkTypeCode)
                {
                    var existingCode = await _context.WorkTypes
                        .FirstOrDefaultAsync(w => w.WorkTypeCode == updateDto.WorkTypeCode && w.WorkTypeID != id);

                    if (existingCode != null)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Mã loại hình làm việc '{updateDto.WorkTypeCode}' đã tồn tại",
                            StatusCode = 400
                        });
                    }
                    workType.WorkTypeCode = updateDto.WorkTypeCode;
                }

                // Check if new name conflicts (excluding current)
                if (!string.IsNullOrEmpty(updateDto.WorkTypeName) && updateDto.WorkTypeName != workType.WorkTypeName)
                {
                    var existingName = await _context.WorkTypes
                        .FirstOrDefaultAsync(w => w.WorkTypeName == updateDto.WorkTypeName && w.WorkTypeID != id);

                    if (existingName != null)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Tên loại hình làm việc '{updateDto.WorkTypeName}' đã tồn tại",
                            StatusCode = 400
                        });
                    }
                    workType.WorkTypeName = updateDto.WorkTypeName;
                }

                // Update description
                if (updateDto.Description != null)
                    workType.Description = updateDto.Description;

                await _context.SaveChangesAsync();

                var workTypeDto = new WorkTypeDto
                {
                    WorkTypeId = workType.WorkTypeID,
                    WorkTypeCode = workType.WorkTypeCode,
                    WorkTypeName = workType.WorkTypeName,
                    Description = workType.Description,
                    IsActive = workType.IsActive
                };

                return Ok(new ApiResponse<WorkTypeDto>
                {
                    Success = true,
                    Data = workTypeDto,
                    Message = "Cập nhật loại hình làm việc thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating work type with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật loại hình làm việc",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Bật/tắt trạng thái hoạt động của loại hình làm việc (Chỉ HR)
        /// </summary>
        /// <param name="id">ID loại hình làm việc</param>
        /// <returns>Kết quả bật/tắt</returns>
        [HttpPut("{id}/toggle-active")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
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
                        Message = $"Loại hình làm việc với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                workType.IsActive = !workType.IsActive;
                await _context.SaveChangesAsync();

                var message = workType.IsActive ?
                    "Kích hoạt loại hình làm việc thành công" :
                    "Vô hiệu hóa loại hình làm việc thành công";

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = message,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling work type active with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi thay đổi trạng thái loại hình làm việc",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Xóa loại hình làm việc (Chỉ SUPER_ADMIN)
        /// </summary>
        /// <param name="id">ID loại hình làm việc</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteWorkType(int id)
        {
            try
            {
                var workType = await _context.WorkTypes
                    .Include(w => w.Attendances)
                    .FirstOrDefaultAsync(w => w.WorkTypeID == id);

                if (workType == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Loại hình làm việc với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                // Check if work type is being used in attendances
                if (workType.Attendances != null && workType.Attendances.Any())
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa loại hình làm việc đang được sử dụng trong chấm công",
                        StatusCode = 400
                    });
                }

                _context.WorkTypes.Remove(workType);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa loại hình làm việc thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting work type with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa loại hình làm việc",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }
    }

    // ========== DTO Classes (nên chuyển sang DTOs folder) ==========

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

    public class UpdateWorkTypeDto
    {
        [StringLength(20)]
        public string? WorkTypeCode { get; set; }

        [StringLength(50)]
        public string? WorkTypeName { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }
    }
}