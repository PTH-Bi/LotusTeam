using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using LotusTeam.Data;
using LotusTeam.Authorization;
using Microsoft.AspNetCore.Authorization;
using LotusTeam.Services;
using LotusTeam.Models;
using BCrypt.Net;
using LotusTeam.Service;
using Microsoft.Extensions.FileProviders;
using LotusTeam.Interfaces;

namespace LotusTeam
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ========================= SERVICES =========================

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler =
                        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition =
                        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                }); ;
            builder.Services.AddEndpointsApiExplorer();

            // ========================= SWAGGER + JWT =========================
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "LotusTeam API",
                    Version = "v1"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập: Bearer {token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ========================= DATABASE =========================
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                )
                .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging()
            );

            // ========================= JWT AUTHENTICATION =========================
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"];

            if (string.IsNullOrEmpty(secretKey))
                throw new Exception("JwtSettings:Secret chưa được cấu hình");

            var key = Encoding.UTF8.GetBytes(secretKey);

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],

                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            // ========================= AUTHORIZATION =========================
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("HrOnly", policy => policy.RequireRole("HR"));

                // Permission-based
                options.AddPolicy("Permission", policy =>
                {
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c => c.Type == "permission"));
                });
            });

            builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            // ========================= CACHE =========================
            // THÊM DÒNG NÀY ĐỂ FIX LỖI IMemoryCache
            builder.Services.AddMemoryCache();

            // ========================= BUSINESS SERVICES =========================
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<EmployeeService>();
            builder.Services.AddScoped<DepartmentService>();
            builder.Services.AddScoped<PositionService>();
            builder.Services.AddScoped<ContractService>();
            builder.Services.AddScoped<RoleService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<DashboardService>();
            builder.Services.AddScoped<WorkTypeService>();
            builder.Services.AddScoped<IRecruitmentService, RecruitmentService>();
            builder.Services.AddScoped<IPayrollService, PayrollService>();
            builder.Services.AddScoped<IPerformanceService, PerformanceService>();
            builder.Services.AddScoped<IProjectService, ProjectService>();
            builder.Services.AddScoped<ILeaveService, LeaveService>();
            builder.Services.AddScoped<IAssetService, AssetService>();
            builder.Services.AddScoped<IRewardDisciplineService, RewardDisciplineService>();
            builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
            builder.Services.AddScoped<ISurveyService, SurveyService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
            builder.Services.AddScoped<ICompanyInfoService, CompanyInfoService>();
            builder.Services.AddScoped<ITrainingService, TrainingService>();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<GmailService>();
            builder.Services.AddHostedService<GmailBackgroundService>();
            builder.Services.AddScoped<PdfParserService>();
            builder.Services.AddScoped<IAttendanceService, AttendanceService>();
            builder.Services.AddScoped<IBankPartnerService, BankPartnerService>();
            builder.Services.AddScoped<IWorkReportService, WorkReportService>();
            builder.Services.AddScoped<IFaceAttendanceService, FaceAttendanceService>();
            builder.Services.AddScoped<IPayrollBankTransferService, PayrollBankTransferService>();
            builder.Services.AddScoped<IRemoteAttendanceService, RemoteAttendanceService>();
            builder.Services.AddScoped<IMultiPositionCvFilterService, MultiPositionCvFilterService>();
            builder.Services.AddScoped<IFaceRecognitionProvider, MockFaceRecognitionProvider>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<CvFilterService>();
            builder.Services.AddScoped<ChatbotService>();
            builder.Services.AddScoped<HRQueryService>();
            builder.Services.AddHttpClient<AIService>();


            // ========================= CORS =========================
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddSwaggerGen(options =>
            {
                options.CustomSchemaIds(type => type.FullName);
            });

            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            app.Logger.LogInformation("Application starting...");

            // ========================= MIDDLEWARE =========================
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }

            // ========================= SEED DATA =========================
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Đảm bảo database được tạo
                await context.Database.EnsureCreatedAsync();

                try
                {
                    // 1. Seed Permissions
                    Console.WriteLine("🔄 Đang seed permissions...");
                    if (!context.Permissions.Any())
                    {
                        var permissions = new List<Permission>
                        {
                            // AUTH
                            new Permission { PermissionCode = "AUTH_PROFILE_VIEW", PermissionName = "Xem hồ sơ cá nhân", Module = "AUTH" },
                            new Permission { PermissionCode = "AUTH_PROFILE_UPDATE", PermissionName = "Cập nhật hồ sơ cá nhân", Module = "AUTH" },
                            new Permission { PermissionCode = "AUTH_CHANGE_PASSWORD", PermissionName = "Đổi mật khẩu", Module = "AUTH" },
                            new Permission { PermissionCode = "AUTH_LOGOUT", PermissionName = "Đăng xuất", Module = "AUTH" },
                            new Permission { PermissionCode = "AUTH_SESSION_MANAGE", PermissionName = "Quản lý phiên đăng nhập", Module = "AUTH" },

                            // USER
                            new Permission { PermissionCode = "USER_VIEW", PermissionName = "Xem người dùng", Module = "USER" },
                            new Permission { PermissionCode = "USER_CREATE", PermissionName = "Tạo người dùng", Module = "USER" },
                            new Permission { PermissionCode = "USER_UPDATE", PermissionName = "Cập nhật người dùng", Module = "USER" },
                            new Permission { PermissionCode = "USER_DELETE", PermissionName = "Xóa người dùng", Module = "USER" },
                            new Permission { PermissionCode = "USER_RESET_PASSWORD", PermissionName = "Reset mật khẩu", Module = "USER" },
                            new Permission { PermissionCode = "USER_TOGGLE_ACTIVE", PermissionName = "Khóa / Mở người dùng", Module = "USER" }
                        };

                        context.Permissions.AddRange(permissions);
                        await context.SaveChangesAsync();
                        Console.WriteLine($"✅ Đã seed {permissions.Count} permissions.");
                    }
                    else
                    {
                        Console.WriteLine("ℹ️ Permissions đã tồn tại, bỏ qua.");
                    }

                    // 2. Seed Roles
                    Console.WriteLine("🔄 Đang seed roles...");
                    if (!context.Roles.Any())
                    {
                        var adminRole = new Role
                        {
                            RoleCode = "ADMIN",
                            RoleName = "Administrator"
                        };

                        var hrRole = new Role
                        {
                            RoleCode = "HR",
                            RoleName = "Human Resource"
                        };

                        context.Roles.AddRange(adminRole, hrRole);
                        await context.SaveChangesAsync();
                        Console.WriteLine("✅ Đã seed 2 roles.");

                        // Gán permissions cho roles
                        Console.WriteLine("🔄 Đang gán permissions cho roles...");
                        var allPermissions = context.Permissions.ToList();

                        // Gán tất cả permissions cho Admin
                        foreach (var permission in allPermissions)
                        {
                            context.RolePermissions.Add(new RolePermissions
                            {
                                RoleID = adminRole.RoleID,
                                PermissionID = permission.PermissionID
                            });
                        }

                        // Gán một số permissions cho HR
                        var hrPermissions = allPermissions
                            .Where(p => p.PermissionCode.StartsWith("AUTH_") ||
                                        p.PermissionCode.StartsWith("USER_"))
                            .ToList();

                        foreach (var permission in hrPermissions)
                        {
                            context.RolePermissions.Add(new RolePermissions
                            {
                                RoleID = hrRole.RoleID,
                                PermissionID = permission.PermissionID
                            });
                        }

                        await context.SaveChangesAsync();
                        Console.WriteLine($"✅ Đã gán {allPermissions.Count} permissions cho Admin và {hrPermissions.Count} permissions cho HR.");
                    }
                    else
                    {
                        Console.WriteLine("ℹ️ Roles đã tồn tại, bỏ qua.");
                    }

                    // 3. Seed Admin User
                    Console.WriteLine("🔄 Đang seed admin user...");
                    if (!context.Users.Any())
                    {
                        // Xóa tất cả UserRoles cũ nếu có
                        if (context.UserRoles.Any())
                        {
                            context.UserRoles.RemoveRange(context.UserRoles);
                            await context.SaveChangesAsync();
                        }

                        // Xóa tất cả Users cũ nếu có
                        if (context.Users.Any())
                        {
                            context.Users.RemoveRange(context.Users);
                            await context.SaveChangesAsync();
                        }

                        // Tạo admin user - ĐẢM BẢO EmployeeID là null
                        var admin = new User
                        {
                            Username = "admin",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                            IsActive = true,
                            CreatedDate = DateTime.Now,
                            EmployeeID = null // QUAN TRỌNG: Để null
                        };

                        context.Users.Add(admin);
                        await context.SaveChangesAsync();

                        // Lấy admin role
                        var adminRole = await context.Roles.FirstAsync(r => r.RoleCode == "ADMIN");

                        // Gán role cho admin
                        context.UserRoles.Add(new UserRoles
                        {
                            UserID = admin.UserID,
                            RoleID = adminRole.RoleID
                        });

                        await context.SaveChangesAsync();

                        Console.WriteLine("✅ Admin user đã được seed thành công!");
                        Console.WriteLine($"   Username: admin");
                        Console.WriteLine($"   Password: 123456");
                        Console.WriteLine($"   Role: ADMIN");
                        Console.WriteLine($"   EmployeeID: null");
                    }
                    else
                    {
                        // Đảm bảo user admin có EmployeeID = null
                        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                        if (existingAdmin != null && existingAdmin.EmployeeID.HasValue)
                        {
                            existingAdmin.EmployeeID = null;
                            await context.SaveChangesAsync();
                            Console.WriteLine("✅ Đã cập nhật admin user: EmployeeID = null");
                        }
                        Console.WriteLine("ℹ️ User đã tồn tại, bỏ qua.");
                    }

                    Console.WriteLine("🎉 Seed data hoàn tất!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Lỗi khi seed data: {ex.Message}");
                    Console.WriteLine($"Chi tiết: {ex.InnerException?.Message}");
                }
            }

            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "Uploads")),
                RequestPath = "/Uploads"
            });
            app.UseHttpsRedirection();
            app.UseCors("AllowAll");

            // ⚠️ THỨ TỰ BẮT BUỘC
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}