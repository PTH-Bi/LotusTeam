using Microsoft.AspNetCore.Mvc;
using LotusTeam.DTOs;
using LotusTeam.Data;
using LotusTeam.Service;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using LotusTeam.Authorization;


namespace LotusTeam.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [HasPermission("USER_VIEW")]

        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Employee!)
                        .ThenInclude(e => e.Department)
                    .Include(u => u.Employee!)
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
                            u.Employee.EmployeeCode!.Contains(search) ||
                            u.Employee.Email!.Contains(search))));
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
                    UserId = u.UserID, // Sửa thành UserID
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

                var response = new ApiResponse<IEnumerable<UserDto>>
                {
                    Success = true,
                    Data = userDtos,
                    Message = "Danh sách người dùng",
                    Pagination = new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách người dùng"
                });
            }
        }

        [HttpGet("{id}")]
        [HasPermission("USER_VIEW")]

        public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Employee!)
                        .ThenInclude(e => e.Department)
                    .Include(u => u.Employee!)
                        .ThenInclude(e => e.Position)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserID == id); // Sửa thành UserID

                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại"
                    });
                }

                // Get permissions
                var permissions = await _context.UserRoles
                    .Where(ur => ur.UserID == id) // Sửa thành UserID
                    .SelectMany(ur => ur.Role!.RolePermissions!)
                    .Select(rp => new PermissionDto
                    {
                        PermissionId = rp.Permission!.PermissionID,
                        PermissionCode = rp.Permission.PermissionCode,
                        PermissionName = rp.Permission.PermissionName,
                        Module = rp.Permission.Module
                    })
                    .Distinct()
                    .ToListAsync();

                var userDetail = new UserDetailDto
                {
                    UserId = user.UserID, // Sửa thành UserID
                    Username = user.Username,
                    FullName = user.Employee?.FullName ?? "Administrator",
                    EmployeeId = user.EmployeeID, // Sửa thành EmployeeID
                    EmployeeCode = user.Employee?.EmployeeCode,
                    DepartmentName = user.Employee?.Department?.DepartmentName,
                    PositionName = user.Employee?.Position?.PositionName,
                    Email = user.Employee?.Email,
                    Phone = user.Employee?.Phone,
                    IsActive = user.IsActive,
                    LastLogin = user.LastLogin,
                    CreatedDate = user.CreatedDate,
                    RolesList = user.UserRoles.Select(ur => new RoleDto // Đổi tên thành RolesList
                    {
                        RoleId = ur.Role!.RoleID,
                        RoleCode = ur.Role.RoleCode,
                        RoleName = ur.Role.RoleName
                    }).ToList(),
                    Permissions = permissions
                };

                return Ok(new ApiResponse<UserDetailDto>
                {
                    Success = true,
                    Data = userDetail,
                    Message = "Thông tin người dùng"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin người dùng"
                });
            }
        }

        [HttpPost]
        [HasPermission("USER_CREATE")]

        public async Task<ActionResult<ApiResponse<UserDetailDto>>> CreateUser(CreateUserDto createDto)
        {
            try
            {
                // Check if username exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == createDto.Username);

                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Tên đăng nhập '{createDto.Username}' đã tồn tại"
                    });
                }

                // Create user
                var user = new User
                {
                    Username = createDto.Username,
                    PasswordHash = HashPassword(createDto.Password),
                    EmployeeID = createDto.EmployeeId, // Sửa thành EmployeeID
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
                            UserID = user.UserID, // Sửa thành UserID
                            RoleID = roleId // Sửa thành RoleID
                        };
                        _context.UserRoles.Add(userRole);
                    }
                    await _context.SaveChangesAsync();
                }

                return await GetUser(user.UserID); // Sửa thành UserID
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo người dùng"
                });
            }
        }

        [HttpPut("{id}")]
        [HasPermission("USER_UPDATE")]

        public async Task<ActionResult<ApiResponse<UserDetailDto>>> UpdateUser(int id, UpdateUserDto updateDto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.UserID == id); // Sửa thành UserID

                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại"
                    });
                }

                // Update basic info
                if (updateDto.EmployeeId.HasValue)
                    user.EmployeeID = updateDto.EmployeeId; // Sửa thành EmployeeID

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
                            UserID = user.UserID, // Sửa thành UserID
                            RoleID = roleId // Sửa thành RoleID
                        };
                        _context.UserRoles.Add(userRole);
                    }
                }

                await _context.SaveChangesAsync();

                return await GetUser(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật người dùng"
                });
            }
        }

        [HttpPut("{id}/toggle-active")]
        [HasPermission("USER_TOGGLE_ACTIVE")]

        public async Task<ActionResult<ApiResponse<object>>> ToggleUserActive(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại"
                    });
                }

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                var message = user.IsActive ? "Mở khóa người dùng thành công" : "Khóa người dùng thành công";

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user active with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi thay đổi trạng thái người dùng"
                });
            }
        }

        [HttpPut("{id}/reset-password")]
        [HasPermission("USER_RESET_PASSWORD")]

        public async Task<ActionResult<ApiResponse<object>>> ResetPassword(int id, ResetPasswordDto resetDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại"
                    });
                }

                user.PasswordHash = HashPassword(resetDto.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Đặt lại mật khẩu thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi đặt lại mật khẩu"
                });
            }
        }

        [HttpDelete("{id}")]
        [HasPermission("USER_DELETE")]

        public async Task<ActionResult<ApiResponse<object>>> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại"
                    });
                }

                user.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa người dùng thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa người dùng"
                });
            }
        }

        private string HashPassword(string password)
        {
            // Sử dụng BCrypt thay vì plain text
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }

    // DTO Classes
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
        public List<RoleDto> RolesList { get; set; } = new(); // Đổi tên để tránh conflict
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