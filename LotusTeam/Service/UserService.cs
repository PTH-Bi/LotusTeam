using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using System.ComponentModel.DataAnnotations;

namespace LotusTeam.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(AppDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<UserDto>>> GetUsersAsync(
            int page = 1,
            int pageSize = 20,
            string? search = null,
            bool? isActive = null)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(u => u.Employee)
                        .ThenInclude(e => e.Position)
                    .Include(u => u.UserRoles)  
                        .ThenInclude(ur => ur.Role)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u =>
                        u.Username.Contains(search) ||
                        (u.Employee != null && (
                            u.Employee.FullName.Contains(search) ||
                            u.Employee.EmployeeCode.Contains(search) ||
                            u.Employee.Email.Contains(search))));
                }

                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var users = await query
                    .OrderBy(u => u.UserID)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userDtos = users.Select(u => new UserDto
                {
                    UserId = u.UserID,
                    Username = u.Username,
                    FullName = u.Employee?.FullName ?? "Administrator",
                    EmployeeCode = u.Employee?.EmployeeCode,
                    DepartmentName = u.Employee?.Department?.DepartmentName,
                    PositionName = u.Employee?.Position?.PositionName,
                    Email = u.Employee?.Email,
                    Phone = u.Employee?.Phone,
                    IsActive = u.IsActive,
                    LastLogin = u.LastLogin,
                    CreatedDate = u.CreatedDate,
                    Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList()
                }).ToList();

                return new ApiResponse<IEnumerable<UserDto>>
                {
                    Success = true,
                    Data = userDtos,
                    Message = "Danh sách người dùng",
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return new ApiResponse<IEnumerable<UserDto>>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách người dùng",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<UserDetailDto>> GetUserAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(u => u.Employee)
                        .ThenInclude(e => e.Position)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserID == id);

                if (user == null)
                {
                    return new ApiResponse<UserDetailDto>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Get permissions
                var permissions = await _context.UserRoles
                    .Where(ur => ur.UserID == id)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => new PermissionDto
                    {
                        PermissionId = rp.Permission.PermissionID,
                        PermissionCode = rp.Permission.PermissionCode,
                        PermissionName = rp.Permission.PermissionName,
                        Module = rp.Permission.Module
                    })
                    .Distinct()
                    .ToListAsync();

                var userDetail = new UserDetailDto
                {
                    UserId = user.UserID,
                    Username = user.Username,
                    FullName = user.Employee?.FullName ?? "Administrator",
                    EmployeeId = user.EmployeeID,
                    EmployeeCode = user.Employee?.EmployeeCode,
                    DepartmentName = user.Employee?.Department?.DepartmentName,
                    PositionName = user.Employee?.Position?.PositionName,
                    Email = user.Employee?.Email,
                    Phone = user.Employee?.Phone,
                    IsActive = user.IsActive,
                    LastLogin = user.LastLogin,
                    CreatedDate = user.CreatedDate,
                    RolesList = user.UserRoles.Select(ur => new RoleDto
                    {
                        RoleId = ur.Role.RoleID,
                        RoleCode = ur.Role.RoleCode,
                        RoleName = ur.Role.RoleName
                    }).ToList(),
                    Permissions = permissions
                };

                return new ApiResponse<UserDetailDto>
                {
                    Success = true,
                    Data = userDetail,
                    Message = "Thông tin người dùng",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID {Id}", id);
                return new ApiResponse<UserDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin người dùng",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<UserDetailDto>> CreateUserAsync(CreateUserDto createDto)
        {
            try
            {
                // Check if username exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == createDto.Username);

                if (existingUser != null)
                {
                    return new ApiResponse<UserDetailDto>
                    {
                        Success = false,
                        Message = $"Tên đăng nhập '{createDto.Username}' đã tồn tại",
                        StatusCode = 400
                    };
                }

                // Check if employee already has a user account
                if (createDto.EmployeeId.HasValue)
                {
                    var existingEmployeeUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.EmployeeID == createDto.EmployeeId);

                    if (existingEmployeeUser != null)
                    {
                        return new ApiResponse<UserDetailDto>
                        {
                            Success = false,
                            Message = "Nhân viên này đã có tài khoản người dùng",
                            StatusCode = 400
                        };
                    }
                }

                // Create user
                var user = new User
                {
                    Username = createDto.Username,
                    PasswordHash = HashPassword(createDto.Password),
                    EmployeeID = createDto.EmployeeId,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Assign roles
                if (createDto.RoleIds != null && createDto.RoleIds.Any())
                {
                    foreach (var roleId in createDto.RoleIds)
                    {
                        var userRole = new UserRoles
                        {
                            UserID = user.UserID,
                            RoleID = roleId
                        };
                        _context.UserRoles.Add(userRole);
                    }
                    await _context.SaveChangesAsync();
                }

                return await GetUserAsync(user.UserID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return new ApiResponse<UserDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo người dùng",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<UserDetailDto>> UpdateUserAsync(int id, UpdateUserDto updateDto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.UserID == id);

                if (user == null)
                {
                    return new ApiResponse<UserDetailDto>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Update basic info
                if (updateDto.EmployeeId.HasValue)
                {
                    // Check if new employee already has a user account
                    var existingEmployeeUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.EmployeeID == updateDto.EmployeeId && u.UserID != id);

                    if (existingEmployeeUser != null)
                    {
                        return new ApiResponse<UserDetailDto>
                        {
                            Success = false,
                            Message = "Nhân viên này đã có tài khoản người dùng khác",
                            StatusCode = 400
                        };
                    }
                    user.EmployeeID = updateDto.EmployeeId;
                }

                if (updateDto.IsActive.HasValue)
                    user.IsActive = updateDto.IsActive.Value;

                // Update roles if provided
                if (updateDto.RoleIds != null)
                {
                    // Remove existing roles
                    var existingRoles = user.UserRoles.ToList();
                    _context.UserRoles.RemoveRange(existingRoles);

                    // Add new roles
                    foreach (var roleId in updateDto.RoleIds)
                    {
                        var userRole = new UserRoles
                        {
                            UserID = user.UserID,
                            RoleID = roleId
                        };
                        _context.UserRoles.Add(userRole);
                    }
                }

                await _context.SaveChangesAsync();

                return await GetUserAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {Id}", id);
                return new ApiResponse<UserDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật người dùng",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<object>> ToggleUserActiveAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                var message = user.IsActive ? "Mở khóa người dùng thành công" : "Khóa người dùng thành công";

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = message,
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user active with ID {Id}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi thay đổi trạng thái người dùng",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<object>> ResetPasswordAsync(int id, ResetPasswordDto resetDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                user.PasswordHash = HashPassword(resetDto.NewPassword);
                await _context.SaveChangesAsync();

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Đặt lại mật khẩu thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user with ID {Id}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi đặt lại mật khẩu",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<object>> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Soft delete by deactivating
                user.IsActive = false;
                await _context.SaveChangesAsync();

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa người dùng thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {Id}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa người dùng",
                    StatusCode = 500
                };
            }
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // ========================= DTO Definitions =========================
        public class UserDto
        {
            public int UserId { get; set; }
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? EmployeeCode { get; set; }
            public string? DepartmentName { get; set; }
            public string? PositionName { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public bool IsActive { get; set; }
            public DateTime? LastLogin { get; set; }
            public DateTime CreatedDate { get; set; }
            public List<string> Roles { get; set; } = new();
        }

        public class UserDetailDto : UserDto
        {
            public int? EmployeeId { get; set; }
            public List<RoleDto> RolesList { get; set; } = new();
            public List<PermissionDto> Permissions { get; set; } = new();
        }

        public class CreateUserDto
        {
            [Required]
            public string Username { get; set; } = string.Empty;

            [Required]
            [MinLength(6)]
            public string Password { get; set; } = string.Empty;

            public int? EmployeeId { get; set; }
            public List<int> RoleIds { get; set; } = new();
        }

        public class UpdateUserDto
        {
            public int? EmployeeId { get; set; }
            public bool? IsActive { get; set; }
            public List<int>? RoleIds { get; set; }
        }

        public class ResetPasswordDto
        {
            [Required]
            [MinLength(6)]
            public string NewPassword { get; set; } = string.Empty;

            [Required]
            [Compare("NewPassword")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public class RoleDto
        {
            public int RoleId { get; set; }
            public string RoleCode { get; set; } = string.Empty;
            public string RoleName { get; set; } = string.Empty;
        }

        public class PermissionDto
        {
            public int PermissionId { get; set; }
            public string PermissionCode { get; set; } = string.Empty;
            public string PermissionName { get; set; } = string.Empty;
            public string Module { get; set; } = string.Empty;
        }
    }
}