using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using LotusTeam.Data;
using LotusTeam.Services;
using LotusTeam.Models;
using Microsoft.Extensions.FileProviders;
using LotusTeam.Interfaces;
using System.Net;
using System.Security.Claims;
using LotusTeam.Service;

namespace LotusTeam
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ========================= SERVICES =========================
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // ========================= SWAGGER =========================
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "LotusTeam API",
                    Version = "v1"
                });

                c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

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

            // ========================= JWT =========================
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

                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = ClaimTypes.Name,

                        ClockSkew = TimeSpan.Zero
                    };
                });

            // ========================= AUTHORIZATION (ROLE-BASED) =========================
            builder.Services.AddAuthorization();

            // ========================= CACHE =========================
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

            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient<AIService>();

            builder.Services.AddHttpClient<GmailService>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = WebRequest.GetSystemWebProxy(),
                    PreAuthenticate = true,
                    UseDefaultCredentials = true
                });

            builder.Services.AddHostedService<GmailBackgroundService>();
            builder.Services.AddScoped<PdfParserService>();

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

            // ========================= SEED ROLE + ADMIN =========================
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await context.Database.EnsureCreatedAsync();

                if (!context.Roles.Any())
                {
                    var roles = new List<Role>
                    {
                        new Role { RoleCode = "SUPER_ADMIN", RoleName = "Super Admin" },
                        new Role { RoleCode = "ADMIN", RoleName = "Admin" },
                        new Role { RoleCode = "HR_MANAGER", RoleName = "HR Manager" },
                        new Role { RoleCode = "HR_STAFF", RoleName = "HR Staff" },
                        new Role { RoleCode = "DIRECTOR", RoleName = "Director" },
                        new Role { RoleCode = "MANAGER", RoleName = "Manager" },
                        new Role { RoleCode = "TEAM_LEADER", RoleName = "Team Leader" },
                        new Role { RoleCode = "EMPLOYEE", RoleName = "Employee" },
                        new Role { RoleCode = "INTERN", RoleName = "Intern" }
                    };

                    context.Roles.AddRange(roles);
                    await context.SaveChangesAsync();
                }

                if (!context.Users.Any())
                {
                    var superAdmin = new User
                    {
                        Username = "superadmin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    context.Users.Add(superAdmin);
                    await context.SaveChangesAsync();

                    var role = context.Roles.First(r => r.RoleCode == "SUPER_ADMIN");

                    context.UserRoles.Add(new UserRoles
                    {
                        UserID = superAdmin.UserID,
                        RoleID = role.RoleID
                    });

                    await context.SaveChangesAsync();

                    Console.WriteLine("✅ Super Admin created: superadmin / 123456");
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}