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
using Microsoft.AspNetCore.Identity;

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
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState
                });
            }

            _logger.LogInformation("=== LOGIN ATTEMPT ===");
            _logger.LogInformation("Username: {Username}", dto.Username);

            var user = await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u =>
                    u.Username == dto.Username &&
                    u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("User not found: {Username}", dto.Username);
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Tên đăng nhập hoặc mật khẩu không đúng"
                });
            }

            _logger.LogInformation("User found: {Username}", user.Username);

            if (!VerifyPassword(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for user: {Username}", dto.Username);
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Tên đăng nhập hoặc mật khẩu không đúng"
                });
            }

            _logger.LogInformation("Password verified successfully for user: {Username}", user.Username);

            // Auto-migrate Identity hash -> BCrypt
            if (user.PasswordHash.StartsWith("AQAAAA"))
            {
                _logger.LogInformation("Migrating password hash to BCrypt for user: {Username}", user.Username);
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            // Update last login
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            // Generate tokens
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshTokens
            {
                UserId = user.UserID,
                Token = refreshToken,
                ExpiryDate = DateTime.Now.AddDays(7),
                IsRevoked = false
            });
            await _context.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.RoleCode).Distinct().ToList();
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.PermissionCode)
                .Distinct()
                .ToList();

            _logger.LogInformation("Login successful for user: {Username}", user.Username);

            return Ok(new ApiResponse<LoginResponseDto>
            {
                Success = true,
                Message = "Đăng nhập thành công",
                Data = new LoginResponseDto
                {
                    UserId = user.UserID,
                    Username = user.Username,
                    FullName = user.Employee?.FullName ?? user.Username,
                    Email = user.Employee?.Email,
                    Token = token,
                    RefreshToken = refreshToken,
                    Roles = roles,
                    Permissions = permissions
                }
            });
        }

        // ========================= REFRESH TOKEN =========================
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(RefreshRequest request)
        {
            try
            {
                _logger.LogInformation("=== REFRESH TOKEN ATTEMPT ===");

                var storedToken = await _context.RefreshTokens
                    .Include(x => x.User)
                        .ThenInclude(u => u.Employee)
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

                if (storedToken.User == null || !storedToken.User.IsActive)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User không hợp lệ"
                    });
                }

                var user = storedToken.User;
                var newAccessToken = GenerateJwtToken(user);
                var newRefreshToken = GenerateRefreshToken();

                storedToken.IsRevoked = true;

                _context.RefreshTokens.Add(new RefreshTokens
                {
                    UserId = user.UserID,
                    Token = newRefreshToken,
                    ExpiryDate = DateTime.Now.AddDays(7),
                    IsRevoked = false
                });

                await _context.SaveChangesAsync();

                var roles = user.UserRoles.Select(ur => ur.Role.RoleCode).Distinct().ToList();
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
                        FullName = user.Employee?.FullName ?? user.Username,
                        Email = user.Employee?.Email,
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
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RefreshRequest request)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi đăng xuất"
                });
            }
        }

        // ========================= CHANGE PASSWORD =========================
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordDto dto)
        {
            try
            {
                var userId = GetUserId();
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

                // Always store as BCrypt going forward
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

                var tokens = await _context.RefreshTokens
                    .Where(x => x.UserId == userId && !x.IsRevoked)
                    .ToListAsync();

                foreach (var t in tokens)
                    t.IsRevoked = true;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Đổi mật khẩu thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change password error");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi đổi mật khẩu"
                });
            }
        }

        // ========================= PROFILE =========================
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
        {
            try
            {
                var userId = GetUserId();

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

                return Ok(new ApiResponse<UserProfileDto>
                {
                    Success = true,
                    Data = new UserProfileDto
                    {
                        UserId = user.UserID,
                        Username = user.Username,
                        FullName = user.Employee?.FullName ?? user.Username,
                        Email = user.Employee?.Email,
                        EmployeeCode = user.Employee?.EmployeeCode,
                        Phone = user.Employee?.Phone,
                        DepartmentName = user.Employee?.Department?.DepartmentName,
                        PositionName = user.Employee?.Position?.PositionName,
                        Roles = user.UserRoles.Select(r => r.Role.RoleName).ToList(),
                        LastLogin = user.LastLogin,
                        IsActive = user.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get profile error");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi lấy thông tin profile"
                });
            }
        }

        // ========================= UPDATE PROFILE =========================
        [HttpPut("profile")]
        [Authorize]
        [HasPermission("AUTH_PROFILE_UPDATE")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile(UpdateProfileDto dto)
        {
            try
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

                if (!string.IsNullOrEmpty(dto.Phone))
                    user.Employee.Phone = dto.Phone;

                if (!string.IsNullOrEmpty(dto.Address))
                    user.Employee.Address = dto.Address;

                if (!string.IsNullOrEmpty(dto.EmergencyContactName))
                    user.Employee.EmergencyContactName = dto.EmergencyContactName;

                if (!string.IsNullOrEmpty(dto.EmergencyContactPhone))
                    user.Employee.EmergencyContactPhone = dto.EmergencyContactPhone;

                await _context.SaveChangesAsync();

                return await GetProfile();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update profile error");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi cập nhật profile"
                });
            }
        }

        // ========================= HELPER METHODS =========================

        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("FullName", user.Employee?.FullName ?? user.Username)
            };

            foreach (var role in user.UserRoles.Select(r => r.Role.RoleCode))
                claims.Add(new Claim(ClaimTypes.Role, role));

            var permissions = user.UserRoles
                .SelectMany(r => r.Role.RolePermissions)
                .Select(rp => rp.Permission.PermissionCode)
                .Distinct();

            foreach (var perm in permissions)
                claims.Add(new Claim("permission", perm));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = Convert.ToDouble(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60");

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(expiry),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("Token không chứa userId");

            return int.Parse(userIdClaim.Value);
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
                {
                    _logger.LogWarning("Password or hash is null or empty");
                    return false;
                }

                // ASP.NET Identity hash (version 2 = AQAAAAE, version 3 = AQAAAAI)
                if (hash.StartsWith("AQAAAA"))
                {
                    _logger.LogInformation("Detected ASP.NET Identity hash");
                    var hasher = new PasswordHasher<object>();
                    var result = hasher.VerifyHashedPassword(null!, hash, password);
                    bool isValid = result == PasswordVerificationResult.Success
                                || result == PasswordVerificationResult.SuccessRehashNeeded;
                    _logger.LogInformation("Identity verify result: {IsValid}", isValid);
                    return isValid;
                }

                // BCrypt hash
                bool bcryptValid = BCrypt.Net.BCrypt.Verify(password, hash);
                _logger.LogInformation("BCrypt verify result: {IsValid}", bcryptValid);
                return bcryptValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }
    }

    // ========================= DTOs =========================
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
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$",
            ErrorMessage = "Mật khẩu phải có chữ và số")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
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