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
    public class PositionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(AppDbContext context, ILogger<PositionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<PositionDto>>>> GetPositions()
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

                return Ok(new ApiResponse<IEnumerable<PositionDto>>
                {
                    Success = true,
                    Data = positionDtos,
                    Message = "Danh sách chức vụ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving positions");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách chức vụ"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PositionDetailDto>>> GetPosition(int id)
        {
            try
            {
                var position = await _context.Positions
                    .Include(p => p.Employees)
                        .ThenInclude(e => e.Department)
                    .FirstOrDefaultAsync(p => p.PositionID == id);

                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Chức vụ với ID {id} không tồn tại"
                    });
                }

                var employees = position.Employees.Select(e => new PositionEmployeeDto
                {
                    EmployeeId = e.EmployeeID,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Email = e.Email,
                    Phone = e.Phone,
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : "",
                    Status = e.Status, // Đây là short
                    StatusText = GetStatusText(e.Status) // Thêm property để hiển thị text
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

                return Ok(new ApiResponse<PositionDetailDto>
                {
                    Success = true,
                    Data = positionDetail,
                    Message = "Thông tin chức vụ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving position with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin chức vụ"
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<PositionDto>>> CreatePosition(CreatePositionDto createDto)
        {
            try
            {
                // Check if position code exists
                var existingPosition = await _context.Positions
                    .FirstOrDefaultAsync(p => p.PositionCode == createDto.PositionCode);

                if (existingPosition != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Mã chức vụ '{createDto.PositionCode}' đã tồn tại"
                    });
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

                return CreatedAtAction(nameof(GetPosition), new { id = position.PositionID },
                    new ApiResponse<PositionDto>
                    {
                        Success = true,
                        Data = positionDto,
                        Message = "Tạo chức vụ thành công"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating position");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo chức vụ"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<PositionDto>>> UpdatePosition(int id, UpdatePositionDto updateDto)
        {
            try
            {
                var position = await _context.Positions.FindAsync(id);

                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Chức vụ với ID {id} không tồn tại"
                    });
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
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Mã chức vụ '{updateDto.PositionCode}' đã tồn tại"
                        });
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

                return Ok(new ApiResponse<PositionDto>
                {
                    Success = true,
                    Data = positionDto,
                    Message = "Cập nhật chức vụ thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật chức vụ"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeletePosition(int id)
        {
            try
            {
                var position = await _context.Positions
                    .Include(p => p.Employees)
                    .FirstOrDefaultAsync(p => p.PositionID == id);

                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Chức vụ với ID {id} không tồn tại"
                    });
                }

                // Check if position has employees
                if (position.Employees.Any())
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa chức vụ đang có nhân viên"
                    });
                }

                _context.Positions.Remove(position);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa chức vụ thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting position with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa chức vụ"
                });
            }
        }

        // Helper method to convert status code to text
        private string GetStatusText(short status)
        {
            return status switch
            {
                0 => "Inactive",
                1 => "Active",
                2 => "On Leave",
                3 => "Terminated",
                4 => "Suspended",
                _ => "Unknown"
            };
        }

        // Hoặc nếu bạn có enum
        private string GetStatusTextFromEnum(short status)
        {
            // Nếu bạn có enum EmployeeStatus
            // return ((EmployeeStatus)status).ToString();

            return status switch
            {
                (short)EmployeeStatus.Inactive => "Inactive",
                (short)EmployeeStatus.Active => "Active",
                (short)EmployeeStatus.OnLeave => "On Leave",
                (short)EmployeeStatus.Terminated => "Terminated",
                _ => "Unknown"
            };
        }
    }

    // ========== DTO Classes ==========

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
        public short Status { get; set; } // Kiểu short
        public string StatusText { get; set; } = string.Empty; // Thêm để hiển thị text
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

    // Nếu bạn chưa có enum, có thể thêm vào
    public enum EmployeeStatus : short
    {
        Inactive = 0,
        Active = 1,
        OnLeave = 2,
        Terminated = 3,
        Suspended = 4
    }
}