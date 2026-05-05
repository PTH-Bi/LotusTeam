using Microsoft.AspNetCore.Mvc;
using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LotusTeam.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class PositionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(AppDbContext context, ILogger<PositionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tất cả chức vụ
        /// </summary>
        /// <returns>Danh sách chức vụ</returns>
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,EMPLOYEE")]
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
                    Message = "Lấy danh sách chức vụ thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving positions");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách chức vụ",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết chức vụ theo ID
        /// </summary>
        /// <param name="id">ID chức vụ</param>
        /// <returns>Thông tin chi tiết chức vụ</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
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
                        Message = $"Chức vụ với ID {id} không tồn tại",
                        StatusCode = 404
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

                return Ok(new ApiResponse<PositionDetailDto>
                {
                    Success = true,
                    Data = positionDetail,
                    Message = "Lấy thông tin chức vụ thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving position with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin chức vụ",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Tạo mới chức vụ (Chỉ HR và Admin)
        /// </summary>
        /// <param name="createDto">Thông tin chức vụ mới</param>
        /// <returns>Chức vụ vừa tạo</returns>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<ActionResult<ApiResponse<PositionDto>>> CreatePosition([FromBody] CreatePositionDto createDto)
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
                // Check if position code exists
                var existingPosition = await _context.Positions
                    .FirstOrDefaultAsync(p => p.PositionCode == createDto.PositionCode);

                if (existingPosition != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Mã chức vụ '{createDto.PositionCode}' đã tồn tại",
                        StatusCode = 400
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
                        Message = "Tạo chức vụ thành công",
                        StatusCode = 201
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating position");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo chức vụ",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin chức vụ (Chỉ HR và Admin)
        /// </summary>
        /// <param name="id">ID chức vụ</param>
        /// <param name="updateDto">Thông tin cập nhật</param>
        /// <returns>Chức vụ sau khi cập nhật</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<ActionResult<ApiResponse<PositionDto>>> UpdatePosition(int id, [FromBody] UpdatePositionDto updateDto)
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
                var position = await _context.Positions.FindAsync(id);

                if (position == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Chức vụ với ID {id} không tồn tại",
                        StatusCode = 404
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
                            Message = $"Mã chức vụ '{updateDto.PositionCode}' đã tồn tại",
                            StatusCode = 400
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
                    Message = "Cập nhật chức vụ thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật chức vụ",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Xóa chức vụ (Chỉ Admin cấp cao)
        /// </summary>
        /// <param name="id">ID chức vụ</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
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
                        Message = $"Chức vụ với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                // Check if position has employees
                if (position.Employees != null && position.Employees.Any())
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa chức vụ đang có nhân viên",
                        StatusCode = 400
                    });
                }

                _context.Positions.Remove(position);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa chức vụ thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting position with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa chức vụ",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        // Helper method to convert status code to text
        private string GetStatusText(short status)
        {
            return status switch
            {
                0 => "Đã nghỉ việc",
                1 => "Đang làm việc",
                2 => "Nghỉ phép",
                3 => "Tạm ngừng",
                4 => "Đã kết thúc",
                _ => "Không xác định"
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

    // Employee Status Enum
    public enum EmployeeStatus : short
    {
        Inactive = 0,
        Active = 1,
        OnLeave = 2,
        Terminated = 3,
        Suspended = 4
    }
}