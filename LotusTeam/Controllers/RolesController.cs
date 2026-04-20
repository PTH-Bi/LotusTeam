using Microsoft.AspNetCore.Mvc;
using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using LotusTeam.Service;


namespace LotusTeam.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
    public class RolesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RolesController> _logger;

        public RolesController(AppDbContext context, ILogger<RolesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR")]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleResponseDto>>>> GetRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .OrderBy(r => r.RoleID) // Sửa thành RoleID
                    .ToListAsync();

                var roleDtos = roles.Select(r => new RoleResponseDto
                {
                    RoleId = r.RoleID, // Sửa thành RoleID
                    RoleCode = r.RoleCode,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    IsSystem = r.IsSystem
                }).ToList();

                return Ok(new ApiResponse<IEnumerable<RoleResponseDto>>
                {
                    Success = true,
                    Data = roleDtos,
                    Message = "Danh sách vai trò"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách vai trò"
                });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<RoleDetailResponseDto>>> GetRole(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.RolePermissions!)
                        .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(r => r.RoleID == id); // Sửa thành RoleID

                if (role == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vai trò với ID {id} không tồn tại"
                    });
                }

                var permissions = role.RolePermissions
                    .Select(rp => new PermissionResponseDto
                    {
                        PermissionId = rp.Permission!.PermissionID, // Sửa thành PermissionID
                        PermissionCode = rp.Permission.PermissionCode,
                        PermissionName = rp.Permission.PermissionName,
                        Module = rp.Permission.Module
                    }).ToList();

                var roleDetail = new RoleDetailResponseDto
                {
                    RoleId = role.RoleID, // Sửa thành RoleID
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
                    Message = "Thông tin vai trò"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin vai trò"
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<RoleDetailResponseDto>>> CreateRole(CreateRoleDto createDto)
        {
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
                        Message = $"Mã vai trò '{createDto.RoleCode}' đã tồn tại"
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
                            RoleID = role.RoleID, // Sửa thành RoleID
                            PermissionID = permissionId // Sửa thành PermissionID
                        };
                        _context.RolePermissions.Add(rolePermission);
                    }
                    await _context.SaveChangesAsync();
                }

                return await GetRole(role.RoleID); // Sửa thành RoleID
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo vai trò"
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<RoleDetailResponseDto>>> UpdateRole(int id, UpdateRoleDto updateDto)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.RoleID == id); // Sửa thành RoleID

                if (role == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vai trò với ID {id} không tồn tại"
                    });
                }

                // System roles cannot be modified
                if (role.IsSystem)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể chỉnh sửa vai trò hệ thống"
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
                    var existingPermissions = role.RolePermissions.ToList();
                    _context.RolePermissions.RemoveRange(existingPermissions);

                    // Add new permissions
                    foreach (var permissionId in updateDto.PermissionIds)
                    {
                        var rolePermission = new RolePermissions
                        {
                            RoleID = role.RoleID, // Sửa thành RoleID
                            PermissionID = permissionId // Sửa thành PermissionID
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
                    Message = "Đã xảy ra lỗi khi cập nhật vai trò"
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteRole(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.UserRoles)
                    .FirstOrDefaultAsync(r => r.RoleID == id); // Sửa thành RoleID

                if (role == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vai trò với ID {id} không tồn tại"
                    });
                }

                // System roles cannot be deleted
                if (role.IsSystem)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa vai trò hệ thống"
                    });
                }

                // Check if role is assigned to any user
                if (role.UserRoles!.Any())
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa vai trò đang được sử dụng"
                    });
                }

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa vai trò thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa vai trò"
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