using Microsoft.AspNetCore.Mvc;
using LotusTeam.DTOs;
using LotusTeam.Data;
using LotusTeam.Service;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LotusTeam.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách người dùng (Chỉ SUPER_ADMIN, ADMIN, HR_MANAGER)
        /// </summary>
        /// <param name="page">Số trang</param>
        /// <param name="pageSize">Số bản ghi mỗi trang</param>
        /// <param name="search">Từ khóa tìm kiếm</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động</param>
        /// <returns>Danh sách người dùng</returns>
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
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

                var response = new ApiResponse<IEnumerable<UserDto>>
                {
                    Success = true,
                    Data = userDtos,
                    Message = "Lấy danh sách người dùng thành công",
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount
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
                    Message = "Đã xảy ra lỗi khi lấy danh sách người dùng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết người dùng theo ID
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <returns>Thông tin chi tiết người dùng</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
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
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.UserID == id);

                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                // Get permissions
                var permissions = await _context.UserRoles
                    .Where(ur => ur.UserID == id)
                    .SelectMany(ur => ur.Role!.RolePermissions!)
                    .Where(rp => rp.Permission != null)
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
                    Message = "Lấy thông tin người dùng thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin người dùng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Tạo mới người dùng (Chỉ SUPER_ADMIN)
        /// </summary>
        /// <param name="createDto">Thông tin người dùng mới</param>
        /// <returns>Người dùng vừa tạo</returns>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> CreateUser([FromBody] CreateUserDto createDto)
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
                // Check if username exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == createDto.Username);

                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Tên đăng nhập '{createDto.Username}' đã tồn tại",
                        StatusCode = 400
                    });
                }

                // Check if employee already has a user account
                if (createDto.EmployeeId.HasValue)
                {
                    var existingEmployeeUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.EmployeeID == createDto.EmployeeId);

                    if (existingEmployeeUser != null)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Nhân viên này đã có tài khoản người dùng",
                            StatusCode = 400
                        });
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

                return await GetUser(user.UserID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo người dùng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin người dùng (Chỉ SUPER_ADMIN)
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <param name="updateDto">Thông tin cập nhật</param>
        /// <returns>Người dùng sau khi cập nhật</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> UpdateUser(int id, [FromBody] UpdateUserDto updateDto)
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
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.UserID == id);

                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                // Update basic info
                if (updateDto.EmployeeId.HasValue)
                {
                    // Check if new employee already has a user account
                    var existingEmployeeUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.EmployeeID == updateDto.EmployeeId && u.UserID != id);

                    if (existingEmployeeUser != null)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Nhân viên này đã có tài khoản người dùng khác",
                            StatusCode = 400
                        });
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

                return await GetUser(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật người dùng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Bật/tắt trạng thái hoạt động của người dùng (Chỉ SUPER_ADMIN)
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <returns>Kết quả bật/tắt</returns>
        [HttpPut("{id}/toggle-active")]
        [Authorize(Roles = "SUPER_ADMIN")]
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
                        Message = $"Người dùng với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                var message = user.IsActive ? "Mở khóa người dùng thành công" : "Khóa người dùng thành công";

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = message,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user active with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi thay đổi trạng thái người dùng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Đặt lại mật khẩu cho người dùng (Chỉ SUPER_ADMIN)
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <param name="resetDto">Thông tin mật khẩu mới</param>
        /// <returns>Kết quả đặt lại mật khẩu</returns>
        [HttpPut("{id}/reset-password")]
        [Authorize(Roles = "SUPER_ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> ResetPassword(int id, [FromBody] ResetPasswordDto resetDto)
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
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Người dùng với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                user.PasswordHash = HashPassword(resetDto.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Đặt lại mật khẩu thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi đặt lại mật khẩu",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Xóa người dùng (vô hiệu hóa) - Chỉ SUPER_ADMIN
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN")]
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
                        Message = $"Người dùng với ID {id} không tồn tại",
                        StatusCode = 404
                    });
                }

                // Soft delete by deactivating
                user.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa người dùng thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa người dùng",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }

    // ========== DTO Classes ==========

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