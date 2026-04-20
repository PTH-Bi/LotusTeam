using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace LotusTeam.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // ================= LOGIN =================
        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto dto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Employee)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u =>
                        u.Username == dto.Username &&
                        u.IsActive);

                if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
                {
                    return new ApiResponse<LoginResponseDto>
                    {
                        Success = false,
                        Message = "Tên đăng nhập hoặc mật khẩu không đúng",
                        StatusCode = 401
                    };
                }

                user.LastLogin = DateTime.Now;

                // ===== TOKEN =====
                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // lưu refresh token DB
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
                    FullName = user.Employee?.FullName ?? "Administrator",
                    Email = user.Employee?.Email,
                    Token = token,
                    RefreshToken = refreshToken, // 🔥 thêm
                    Roles = roles,
                    Permissions = permissions
                };

                return new ApiResponse<LoginResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = "Đăng nhập thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return new ApiResponse<LoginResponseDto>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi đăng nhập",
                    StatusCode = 500
                };
            }
        }

        // ================= REFRESH TOKEN =================
        public async Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var storedToken = await _context.RefreshTokens
                    .Include(x => x.User)
                        .ThenInclude(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                                .ThenInclude(r => r.RolePermissions)
                                    .ThenInclude(rp => rp.Permission)
                    .Include(x => x.User)
                        .ThenInclude(u => u.Employee)
                    .FirstOrDefaultAsync(x => x.Token == refreshToken);

                if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.Now)
                {
                    return new ApiResponse<LoginResponseDto>
                    {
                        Success = false,
                        Message = "Refresh token không hợp lệ",
                        StatusCode = 401
                    };
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

                return new ApiResponse<LoginResponseDto>
                {
                    Success = true,
                    Data = new LoginResponseDto
                    {
                        UserId = user.UserID,
                        Username = user.Username,
                        FullName = user.Employee?.FullName ?? "Administrator",
                        Email = user.Employee?.Email,
                        Token = newAccessToken,
                        RefreshToken = newRefreshToken,
                        Roles = roles,
                        Permissions = permissions
                    },
                    Message = "Refresh token thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token error");
                return new ApiResponse<LoginResponseDto>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi refresh token",
                    StatusCode = 500
                };
            }
        }

        // ================= LOGOUT =================
        public async Task<ApiResponse<object>> LogoutAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync();
            }

            return new ApiResponse<object>
            {
                Success = true,
                Message = "Đăng xuất thành công",
                StatusCode = 200
            };
        }

        // ================= CHANGE PASSWORD =================
        public async Task<ApiResponse<object>> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Người dùng không tồn tại",
                        StatusCode = 404
                    };
                }

                if (!VerifyPassword(dto.OldPassword, user.PasswordHash))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mật khẩu cũ không chính xác",
                        StatusCode = 400
                    };
                }

                user.PasswordHash = HashPassword(dto.NewPassword);
                await _context.SaveChangesAsync();

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Đổi mật khẩu thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change password error for user ID {UserId}", userId);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi đổi mật khẩu",
                    StatusCode = 500
                };
            }
        }

        // ================= PROFILE =================
        public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(int userId)
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
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user == null)
                {
                    return new ApiResponse<UserProfileDto>
                    {
                        Success = false,
                        Message = "Người dùng không tồn tại",
                        StatusCode = 404
                    };
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

                return new ApiResponse<UserProfileDto>
                {
                    Success = true,
                    Data = profile,
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get profile error for user ID {UserId}", userId);
                return new ApiResponse<UserProfileDto>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi lấy thông tin hồ sơ",
                    StatusCode = 500
                };
            }
        }

        // ================= UPDATE PROFILE =================
        public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user == null || user.Employee == null)
                {
                    return new ApiResponse<UserProfileDto>
                    {
                        Success = false,
                        Message = "Người dùng không tồn tại",
                        StatusCode = 404
                    };
                }

                user.Employee.Phone = dto.Phone ?? user.Employee.Phone;
                user.Employee.Address = dto.Address ?? user.Employee.Address;
                user.Employee.EmergencyContactName = dto.EmergencyContactName ?? user.Employee.EmergencyContactName;
                user.Employee.EmergencyContactPhone = dto.EmergencyContactPhone ?? user.Employee.EmergencyContactPhone;

                await _context.SaveChangesAsync();

                return await GetProfileAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update profile error for user ID {UserId}", userId);
                return new ApiResponse<UserProfileDto>
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi cập nhật hồ sơ",
                    StatusCode = 500
                };
            }
        }

        // ================= HELPER =================
        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
            => BCrypt.Net.BCrypt.Verify(password, hash);

        private string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password);

        // ========================= DTO Definitions =========================
        public class LoginDto
        {
            [Required] public string Username { get; set; } = string.Empty;
            [Required] public string Password { get; set; } = string.Empty;
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
}