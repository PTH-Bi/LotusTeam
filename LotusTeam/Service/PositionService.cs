using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using System.ComponentModel.DataAnnotations;

namespace LotusTeam.Services
{
    public class PositionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PositionService> _logger;

        public PositionService(AppDbContext context, ILogger<PositionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<PositionDto>>> GetPositionsAsync()
        {
            try
            {
                var positions = await _context.Positions
                    .OrderBy(p => p.PositionName)
                    .ToListAsync();

                var positionDtos = positions.Select(p => new PositionDto
                {
                    PositionId = p.PositionID,
                    PositionCode = p.PositionCode,
                    PositionName = p.PositionName,
                    Description = p.Description
                }).ToList();

                return new ApiResponse<IEnumerable<PositionDto>>
                {
                    Success = true,
                    Data = positionDtos,
                    Message = "Danh sách chức vụ",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving positions");
                return new ApiResponse<IEnumerable<PositionDto>>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách chức vụ",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<PositionDetailDto>> GetPositionAsync(int id)
        {
            try
            {
                var position = await _context.Positions
                    .Include(p => p.Employees)
                        .ThenInclude(e => e.Department)
                    .FirstOrDefaultAsync(p => p.PositionID == id);

                if (position == null)
                {
                    return new ApiResponse<PositionDetailDto>
                    {
                        Success = false,
                        Message = $"Chức vụ với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                var employees = position.Employees.Select(e => new PositionEmployeeDto
                {
                    EmployeeId = e.EmployeeID,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Email = e.Email,
                    Phone = e.Phone,
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : "",
                    Status = e.Status,
                    StatusText = GetStatusText(e.Status)
                }).ToList();

                var positionDetail = new PositionDetailDto
                {
                    PositionId = position.PositionID,
                    PositionCode = position.PositionCode,
                    PositionName = position.PositionName,
                    Description = position.Description,
                    Employees = employees,
                    EmployeeCount = employees.Count
                };

                return new ApiResponse<PositionDetailDto>
                {
                    Success = true,
                    Data = positionDetail,
                    Message = "Thông tin chức vụ",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving position with ID {Id}", id);
                return new ApiResponse<PositionDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin chức vụ",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<PositionDto>> CreatePositionAsync(CreatePositionDto createDto)
        {
            try
            {
                // Check if position code exists
                var existingPosition = await _context.Positions
                    .FirstOrDefaultAsync(p => p.PositionCode == createDto.PositionCode);

                if (existingPosition != null)
                {
                    return new ApiResponse<PositionDto>
                    {
                        Success = false,
                        Message = $"Mã chức vụ '{createDto.PositionCode}' đã tồn tại",
                        StatusCode = 400
                    };
                }

                var position = new Position
                {
                    PositionCode = createDto.PositionCode,
                    PositionName = createDto.PositionName,
                    Description = createDto.Description
                };

                _context.Positions.Add(position);
                await _context.SaveChangesAsync();

                var positionDto = new PositionDto
                {
                    PositionId = position.PositionID,
                    PositionCode = position.PositionCode,
                    PositionName = position.PositionName,
                    Description = position.Description
                };

                return new ApiResponse<PositionDto>
                {
                    Success = true,
                    Data = positionDto,
                    Message = "Tạo chức vụ thành công",
                    StatusCode = 201
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating position");
                return new ApiResponse<PositionDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo chức vụ",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<PositionDto>> UpdatePositionAsync(int id, UpdatePositionDto updateDto)
        {
            try
            {
                var position = await _context.Positions.FindAsync(id);

                if (position == null)
                {
                    return new ApiResponse<PositionDto>
                    {
                        Success = false,
                        Message = $"Chức vụ với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Check if new position code conflicts
                if (!string.IsNullOrEmpty(updateDto.PositionCode) &&
                    updateDto.PositionCode != position.PositionCode)
                {
                    var existingCode = await _context.Positions
                        .FirstOrDefaultAsync(p => p.PositionCode == updateDto.PositionCode &&
                                                 p.PositionID != id);

                    if (existingCode != null)
                    {
                        return new ApiResponse<PositionDto>
                        {
                            Success = false,
                            Message = $"Mã chức vụ '{updateDto.PositionCode}' đã tồn tại",
                            StatusCode = 400
                        };
                    }
                }

                // Update properties
                if (!string.IsNullOrEmpty(updateDto.PositionCode))
                    position.PositionCode = updateDto.PositionCode;

                if (!string.IsNullOrEmpty(updateDto.PositionName))
                    position.PositionName = updateDto.PositionName;

                if (updateDto.Description != null)
                    position.Description = updateDto.Description;

                await _context.SaveChangesAsync();

                var positionDto = new PositionDto
                {
                    PositionId = position.PositionID,
                    PositionCode = position.PositionCode,
                    PositionName = position.PositionName,
                    Description = position.Description
                };

                return new ApiResponse<PositionDto>
                {
                    Success = true,
                    Data = positionDto,
                    Message = "Cập nhật chức vụ thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position with ID {Id}", id);
                return new ApiResponse<PositionDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật chức vụ",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<object>> DeletePositionAsync(int id)
        {
            try
            {
                var position = await _context.Positions
                    .Include(p => p.Employees)
                    .FirstOrDefaultAsync(p => p.PositionID == id);

                if (position == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Chức vụ với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Check if position has employees
                if (position.Employees.Any())
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa chức vụ đang có nhân viên",
                        StatusCode = 400
                    };
                }

                _context.Positions.Remove(position);
                await _context.SaveChangesAsync();

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa chức vụ thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting position with ID {Id}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa chức vụ",
                    StatusCode = 500
                };
            }
        }

        private string GetStatusText(short status)
        {
            return status switch
            {
                0 => "Đã nghỉ việc",
                1 => "Đang làm việc",
                2 => "Nghỉ phép",
                3 => "Tạm ngừng",
                _ => "Không xác định"
            };
        }

        // ========================= DTO Definitions =========================
        public class PositionDto
        {
            public int PositionId { get; set; }
            public string PositionCode { get; set; } = string.Empty;
            public string PositionName { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        public class PositionDetailDto : PositionDto
        {
            public int EmployeeCount { get; set; }
            public List<PositionEmployeeDto> Employees { get; set; } = new();
        }

        public class PositionEmployeeDto
        {
            public int EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? DepartmentName { get; set; }
            public short Status { get; set; }
            public string StatusText { get; set; } = string.Empty;
        }

        public class CreatePositionDto
        {
            [Required]
            [StringLength(10)]
            public string PositionCode { get; set; } = string.Empty;

            [Required]
            [StringLength(100)]
            public string PositionName { get; set; } = string.Empty;

            [StringLength(255)]
            public string? Description { get; set; }
        }

        public class UpdatePositionDto
        {
            [StringLength(10)]
            public string? PositionCode { get; set; }

            [StringLength(100)]
            public string? PositionName { get; set; }

            [StringLength(255)]
            public string? Description { get; set; }
        }
    }
}