using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using System.ComponentModel.DataAnnotations;

namespace LotusTeam.Services
{
    public class RoleService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoleService> _logger;

        public RoleService(AppDbContext context, ILogger<RoleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<RoleResponseDto>>> GetRolesAsync()
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

                return new ApiResponse<IEnumerable<RoleResponseDto>>
                {
                    Success = true,
                    Data = roleDtos,
                    Message = "Danh sách vai trò",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return new ApiResponse<IEnumerable<RoleResponseDto>>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách vai trò",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<RoleDetailResponseDto>> GetRoleAsync(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(r => r.RoleID == id);

                if (role == null)
                {
                    return new ApiResponse<RoleDetailResponseDto>
                    {
                        Success = false,
                        Message = $"Vai trò với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                var permissions = role.RolePermissions
                    .Select(rp => new PermissionResponseDto
                    {
                        PermissionId = rp.Permission.PermissionID,
                        PermissionCode = rp.Permission.PermissionCode,
                        PermissionName = rp.Permission.PermissionName,
                        Module = rp.Permission.Module
                    }).ToList();

                var roleDetail = new RoleDetailResponseDto
                {
                    RoleId = role.RoleID,
                    RoleCode = role.RoleCode,
                    RoleName = role.RoleName,
                    Description = role.Description,
                    IsSystem = role.IsSystem,
                    Permissions = permissions
                };

                return new ApiResponse<RoleDetailResponseDto>
                {
                    Success = true,
                    Data = roleDetail,
                    Message = "Thông tin vai trò",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role with ID {Id}", id);
                return new ApiResponse<RoleDetailResponseDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin vai trò",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<RoleDetailResponseDto>> CreateRoleAsync(CreateRoleDto createDto)
        {
            try
            {
                // Check if role code exists
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleCode == createDto.RoleCode);

                if (existingRole != null)
                {
                    return new ApiResponse<RoleDetailResponseDto>
                    {
                        Success = false,
                        Message = $"Mã vai trò '{createDto.RoleCode}' đã tồn tại",
                        StatusCode = 400
                    };
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

                return await GetRoleAsync(role.RoleID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return new ApiResponse<RoleDetailResponseDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo vai trò",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<RoleDetailResponseDto>> UpdateRoleAsync(int id, UpdateRoleDto updateDto)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.RoleID == id);

                if (role == null)
                {
                    return new ApiResponse<RoleDetailResponseDto>
                    {
                        Success = false,
                        Message = $"Vai trò với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // System roles cannot be modified
                if (role.IsSystem)
                {
                    return new ApiResponse<RoleDetailResponseDto>
                    {
                        Success = false,
                        Message = "Không thể chỉnh sửa vai trò hệ thống",
                        StatusCode = 400
                    };
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
                            RoleID = role.RoleID,
                            PermissionID = permissionId
                        };
                        _context.RolePermissions.Add(rolePermission);
                    }
                }

                await _context.SaveChangesAsync();

                return await GetRoleAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role with ID {Id}", id);
                return new ApiResponse<RoleDetailResponseDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật vai trò",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<object>> DeleteRoleAsync(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.UserRoles)
                    .FirstOrDefaultAsync(r => r.RoleID == id);

                if (role == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Vai trò với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // System roles cannot be deleted
                if (role.IsSystem)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa vai trò hệ thống",
                        StatusCode = 400
                    };
                }

                // Check if role is assigned to any user
                if (role.UserRoles.Any())
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa vai trò đang được sử dụng",
                        StatusCode = 400
                    };
                }

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa vai trò thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role with ID {Id}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa vai trò",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<PermissionResponseDto>>> GetPermissionsAsync()
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

                return new ApiResponse<IEnumerable<PermissionResponseDto>>
                {
                    Success = true,
                    Data = permissionDtos,
                    Message = "Danh sách quyền hạn",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions");
                return new ApiResponse<IEnumerable<PermissionResponseDto>>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách quyền hạn",
                    StatusCode = 500
                };
            }
        }

        // ========================= DTO Definitions =========================
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
}