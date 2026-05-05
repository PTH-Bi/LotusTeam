using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using System.ComponentModel.DataAnnotations;

namespace LotusTeam.Services
{
    public class WorkTypeService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WorkTypeService> _logger;

        public WorkTypeService(AppDbContext context, ILogger<WorkTypeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<WorkTypeDto>>> GetWorkTypesAsync()
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

                return new ApiResponse<IEnumerable<WorkTypeDto>>
                {
                    Success = true,
                    Data = workTypeDtos,
                    Message = "Danh sách loại hình làm việc",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving work types");
                return new ApiResponse<IEnumerable<WorkTypeDto>>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách loại hình làm việc",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<WorkTypeDto>> CreateWorkTypeAsync(CreateWorkTypeDto createDto)
        {
            try
            {
                // Check if work type code exists
                var existingWorkType = await _context.WorkTypes
                    .FirstOrDefaultAsync(w => w.WorkTypeCode == createDto.WorkTypeCode);

                if (existingWorkType != null)
                {
                    return new ApiResponse<WorkTypeDto>
                    {
                        Success = false,
                        Message = $"Mã loại hình làm việc '{createDto.WorkTypeCode}' đã tồn tại",
                        StatusCode = 400
                    };
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

                return new ApiResponse<WorkTypeDto>
                {
                    Success = true,
                    Data = workTypeDto,
                    Message = "Tạo loại hình làm việc thành công",
                    StatusCode = 201
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work type");
                return new ApiResponse<WorkTypeDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo loại hình làm việc",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<object>> ToggleWorkTypeActiveAsync(int id)
        {
            try
            {
                var workType = await _context.WorkTypes.FindAsync(id);
                if (workType == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Loại hình làm việc với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                workType.IsActive = !workType.IsActive;
                await _context.SaveChangesAsync();

                var message = workType.IsActive ?
                    "Kích hoạt loại hình làm việc thành công" :
                    "Vô hiệu hóa loại hình làm việc thành công";

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = message,
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling work type active with ID {Id}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi thay đổi trạng thái loại hình làm việc",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<WorkTypeDto>> UpdateWorkTypeAsync(int id, UpdateWorkTypeDto updateDto)
        {
            try
            {
                var workType = await _context.WorkTypes.FindAsync(id);
                if (workType == null)
                {
                    return new ApiResponse<WorkTypeDto>
                    {
                        Success = false,
                        Message = $"Loại hình làm việc với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Update properties
                if (!string.IsNullOrEmpty(updateDto.WorkTypeCode))
                    workType.WorkTypeCode = updateDto.WorkTypeCode;

                if (!string.IsNullOrEmpty(updateDto.WorkTypeName))
                    workType.WorkTypeName = updateDto.WorkTypeName;

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

                return new ApiResponse<WorkTypeDto>
                {
                    Success = true,
                    Data = workTypeDto,
                    Message = "Cập nhật loại hình làm việc thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating work type with ID {Id}", id);
                return new ApiResponse<WorkTypeDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật loại hình làm việc",
                    StatusCode = 500
                };
            }
        }

        // ========================= DTO Definitions =========================
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
}