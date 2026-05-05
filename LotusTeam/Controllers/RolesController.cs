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
    public class RolesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RolesController> _logger;

        public RolesController(AppDbContext context, ILogger<RolesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tất cả vai trò
        /// </summary>
        /// <returns>Danh sách vai trò</returns>
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleResponseDto>>>> GetRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .OrderBy(r => r.RoleID)
                    .ToListAsync();

                var roleDtos = roles.Select(r => new RoleResponseDto
                {
                    RoleId = r.RoleID,
                    RoleCode = r.RoleCode,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    IsSystem = r.IsSystem
                }).ToList();

                return Ok(new ApiResponse<IEnumerable<RoleResponseDto>>
                {
                    Success = true,
                    Data = roleDtos,
                    Message = "Lấy danh sách vai trò thành công",
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = roleDtos.Count,
                        Page = 1,
                        PageSize = roleDtos.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách vai trò",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết vai trò theo ID (kèm danh sách quyền)
        /// </summary>
        /// <param name="id">ID vai trò</param>
        /// <returns>Thông tin chi tiết vai trò</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<RoleDetailResponseDto>>> GetRole(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.RolePermissions!)
                        .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(r => r.RoleID == id);

                if (role == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vai trò với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                var permissions = role.RolePermissions?
                    .Where(rp => rp.Permission != null)
                    .Select(rp => new PermissionResponseDto
                    {
                        PermissionId = rp.Permission!.PermissionID,
                        PermissionCode = rp.Permission.PermissionCode,
                        PermissionName = rp.Permission.PermissionName,
                        Module = rp.Permission.Module
                    }).ToList() ?? new List<PermissionResponseDto>();

                var roleDetail = new RoleDetailResponseDto
                {
                    RoleId = role.RoleID,
                    RoleCode = role.RoleCode,
                    RoleName = role.RoleName,
                    Description = role.Description,
                    IsSystem = role.IsSystem,
                    Permissions = permissions
                };

                return Ok(new ApiResponse<RoleDetailResponseDto>
                {
                    Success = true,
                    Data = roleDetail,
                    Message = "Lấy thông tin vai trò thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin vai trò",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả quyền hạn (Permission)
        /// </summary>
        /// <returns>Danh sách quyền hạn</returns>
        [HttpGet("permissions")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PermissionResponseDto>>>> GetPermissions()
        {
            try
            {
                var permissions = await _context.Permissions
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.PermissionName)
                    .ToListAsync();

                var permissionDtos = permissions.Select(p => new PermissionResponseDto
                {
                    PermissionId = p.PermissionID,
                    PermissionCode = p.PermissionCode,
                    PermissionName = p.PermissionName,
                    Module = p.Module
                }).ToList();

                return Ok(new ApiResponse<IEnumerable<PermissionResponseDto>>
                {
                    Success = true,
                    Data = permissionDtos,
                    Message = "Lấy danh sách quyền hạn thành công",
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = permissionDtos.Count,
                        Page = 1,
                        PageSize = permissionDtos.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách quyền hạn",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Tạo mới vai trò (Chỉ SUPER_ADMIN)
        /// </summary>
        /// <param name="createDto">Thông tin vai trò mới</param>
        /// <returns>Vai trò vừa tạo</returns>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<RoleDetailResponseDto>>> CreateRole([FromBody] CreateRoleDto createDto)
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
                // Check if role code exists
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleCode == createDto.RoleCode);

                if (existingRole != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Mã vai trò '{createDto.RoleCode}' đã tồn tại",
                        StatusCode = 400
                    });
                }

                // Create role
                var role = new Role
                {
                    RoleCode = createDto.RoleCode,
                    RoleName = createDto.RoleName,
                    Description = createDto.Description,
                    IsSystem = false
                };

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                // Assign permissions
                if (createDto.PermissionIds != null && createDto.PermissionIds.Any())
                {
                    foreach (var permissionId in createDto.PermissionIds)
                    {
                        var rolePermission = new RolePermissions
                        {
                            RoleID = role.RoleID,
                            PermissionID = permissionId
                        };
                        _context.RolePermissions.Add(rolePermission);
                    }
                    await _context.SaveChangesAsync();
                }

                return await GetRole(role.RoleID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo vai trò",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin vai trò (Chỉ SUPER_ADMIN)
        /// </summary>
        /// <param name="id">ID vai trò</param>
        /// <param name="updateDto">Thông tin cập nhật</param>
        /// <returns>Vai trò sau khi cập nhật</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<RoleDetailResponseDto>>> UpdateRole(int id, [FromBody] UpdateRoleDto updateDto)
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
                var role = await _context.Roles
                    .Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.RoleID == id);

                if (role == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vai trò với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                // System roles cannot be modified
                if (role.IsSystem)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể chỉnh sửa vai trò hệ thống",
                        StatusCode = 400
                    });
                }

                // Update basic info
                if (!string.IsNullOrEmpty(updateDto.RoleName))
                    role.RoleName = updateDto.RoleName;

                if (!string.IsNullOrEmpty(updateDto.Description))
                    role.Description = updateDto.Description;

                // Update permissions if provided
                if (updateDto.PermissionIds != null)
                {
                    // Remove existing permissions
                    var existingPermissions = role.RolePermissions?.ToList() ?? new List<RolePermissions>();
                    if (existingPermissions.Any())
                    {
                        _context.RolePermissions.RemoveRange(existingPermissions);
                    }

                    // Add new permissions
                    foreach (var permissionId in updateDto.PermissionIds)
                    {
                        var rolePermission = new RolePermissions
                        {
                            RoleID = role.RoleID,
                            PermissionID = permissionId
                        };
                        _context.RolePermissions.Add(rolePermission);
                    }
                }

                await _context.SaveChangesAsync();

                return await GetRole(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật vai trò",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Xóa vai trò (Chỉ SUPER_ADMIN)
        /// </summary>
        /// <param name="id">ID vai trò</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteRole(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.UserRoles)
                    .FirstOrDefaultAsync(r => r.RoleID == id);

                if (role == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vai trò với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                // System roles cannot be deleted
                if (role.IsSystem)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa vai trò hệ thống",
                        StatusCode = 400
                    });
                }

                // Check if role is assigned to any user
                if (role.UserRoles != null && role.UserRoles.Any())
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa vai trò đang được sử dụng",
                        StatusCode = 400
                    });
                }

                // Remove role permissions first
                var rolePermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleID == id)
                    .ToListAsync();
                if (rolePermissions.Any())
                {
                    _context.RolePermissions.RemoveRange(rolePermissions);
                }

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa vai trò thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa vai trò",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }
    }

    // ========== DTO Classes ==========

    public class RoleResponseDto
    {
        public int RoleId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
    }

    public class RoleDetailResponseDto : RoleResponseDto
    {
        public List<PermissionResponseDto> Permissions { get; set; } = new();
    }

    public class CreateRoleDto
    {
        [Required]
        [StringLength(50)]
        public string RoleCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }
        public List<int> PermissionIds { get; set; } = new();
    }

    public class UpdateRoleDto
    {
        [StringLength(100)]
        public string? RoleName { get; set; }

        public string? Description { get; set; }
        public List<int>? PermissionIds { get; set; }
    }

    public class PermissionResponseDto
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
    }
}