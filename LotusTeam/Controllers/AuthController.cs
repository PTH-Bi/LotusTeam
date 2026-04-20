using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using System.ComponentModel.DataAnnotations;
using LotusTeam.Authorization;
using LotusTeam.Service;
using System.Security.Cryptography;

namespace LotusTeam.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // ========================= LOGIN =========================
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(LoginDto dto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u =>
                        u.Username == dto.Username &&
                        u.IsActive);

                if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Tên đăng nhập hoặc mật khẩu không đúng"
                    });
                }

                user.LastLogin = DateTime.Now;

                // ===== TOKEN =====
                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // lưu DB
                _context.RefreshTokens.Add(new RefreshTokens
                {
                    UserId = user.UserID,
                    Token = refreshToken,
                    ExpiryDate = DateTime.Now.AddDays(7),
                    IsRevoked = false
                });

                await _context.SaveChangesAsync();

                var roles = user.UserRoles
                    .Select(ur => ur.Role.RoleCode)
                    .Distinct()
                    .ToList();

                var permissions = user.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.PermissionCode)
                    .Distinct()
                    .ToList();

                var response = new LoginResponseDto
                {
                    UserId = user.UserID,
                    Username = user.Username,
                    FullName = "Administrator",
                    Email = null,
                    Token = token,
                    RefreshToken = refreshToken, // 🔥 thêm
                    Roles = roles,
                    Permissions = permissions
                };

                return Ok(new ApiResponse<LoginResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = "Đăng nhập thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi đăng nhập"
                });
            }
        }

        // ========================= REFRESH TOKEN =========================
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(RefreshRequest request)
        {
            try
            {
                var storedToken = await _context.RefreshTokens
                    .Include(x => x.User)
                        .ThenInclude(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                                .ThenInclude(r => r.RolePermissions)
                                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

                if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.Now)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Refresh token không hợp lệ"
                    });
                }

                var user = storedToken.User;

                var newAccessToken = GenerateJwtToken(user);
                var newRefreshToken = GenerateRefreshToken();

                // revoke token cũ
                storedToken.IsRevoked = true;

                _context.RefreshTokens.Add(new RefreshTokens
                {
                    UserId = user.UserID,
                    Token = newRefreshToken,
                    ExpiryDate = DateTime.Now.AddDays(7),
                    IsRevoked = false
                });

                await _context.SaveChangesAsync();

                var roles = user.UserRoles
                    .Select(ur => ur.Role.RoleCode)
                    .Distinct()
                    .ToList();

                var permissions = user.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.PermissionCode)
                    .Distinct()
                    .ToList();

                return Ok(new ApiResponse<LoginResponseDto>
                {
                    Success = true,
                    Message = "Refresh token thành công",
                    Data = new LoginResponseDto
                    {
                        UserId = user.UserID,
                        Username = user.Username,
                        FullName = "Administrator",
                        Email = null,
                        Token = newAccessToken,
                        RefreshToken = newRefreshToken,
                        Roles = roles,
                        Permissions = permissions
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token error");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi refresh token"
                });
            }
        }

        // ========================= LOGOUT =========================
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RefreshRequest request)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Đăng xuất thành công"
            });
        }

        // ========================= CHANGE PASSWORD =========================
        [HttpPost("change-password")]
        [Authorize]
        [HasPermission("AUTH_CHANGE_PASSWORD")]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại"
                });
            }

            if (!VerifyPassword(dto.OldPassword, user.PasswordHash))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Mật khẩu cũ không chính xác"
                });
            }

            user.PasswordHash = HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Đổi mật khẩu thành công"
            });
        }

        // ========================= PROFILE =========================
        [HttpGet("profile")]
        [Authorize]
        [HasPermission("AUTH_PROFILE_VIEW")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Department)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Position)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại"
                });
            }

            var profile = new UserProfileDto
            {
                UserId = user.UserID,
                Username = user.Username,
                FullName = user.Employee?.FullName ?? "Administrator",
                Email = user.Employee?.Email,
                EmployeeCode = user.Employee?.EmployeeCode,
                Phone = user.Employee?.Phone,
                DepartmentName = user.Employee?.Department?.DepartmentName,
                PositionName = user.Employee?.Position?.PositionName,
                Roles = user.UserRoles.Select(r => r.Role.RoleName).ToList(),
                LastLogin = user.LastLogin,
                IsActive = user.IsActive
            };

            return Ok(new ApiResponse<UserProfileDto>
            {
                Success = true,
                Data = profile
            });
        }

        // ========================= UPDATE PROFILE =========================
        [HttpPut("profile")]
        [Authorize]
        [HasPermission("AUTH_PROFILE_UPDATE")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile(UpdateProfileDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null || user.Employee == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Người dùng không tồn tại"
                });
            }

            user.Employee.Phone = dto.Phone ?? user.Employee.Phone;
            user.Employee.Address = dto.Address ?? user.Employee.Address;
            user.Employee.EmergencyContactName = dto.EmergencyContactName ?? user.Employee.EmergencyContactName;
            user.Employee.EmergencyContactPhone = dto.EmergencyContactPhone ?? user.Employee.EmergencyContactPhone;

            await _context.SaveChangesAsync();
            return await GetProfile();
        }

        // ========================= HELPER =========================
        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("FullName", "Administrator")
            };

            foreach (var role in user.UserRoles.Select(r => r.Role.RoleCode))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var permissions = user.UserRoles
                .SelectMany(r => r.Role.RolePermissions)
                .Select(rp => rp.Permission.PermissionCode)
                .Distinct();

            foreach (var perm in permissions)
            {
                claims.Add(new Claim("permission", perm));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    Convert.ToDouble(_configuration["JwtSettings:ExpiryInMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                    return false;

                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }

        private string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password);
    }

    // ========================= DTO =========================
    public class LoginDto
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = "";
    }

    public class LoginResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = "";

        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }

    public class ChangePasswordDto
    {
        [Required] public string OldPassword { get; set; } = string.Empty;
        [Required, MinLength(6)] public string NewPassword { get; set; } = string.Empty;
        [Required, Compare("NewPassword")] public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? EmployeeCode { get; set; }
        public string? Phone { get; set; }
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }
}