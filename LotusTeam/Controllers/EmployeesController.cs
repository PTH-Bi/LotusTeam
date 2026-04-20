using Microsoft.AspNetCore.Mvc;
using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LotusTeam.Service;
using Microsoft.AspNetCore.Identity;


namespace LotusTeam.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
    public class EmployeesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeesController> _logger;
        private readonly IWebHostEnvironment _environment;

        public EmployeesController(
            AppDbContext context,
            ILogger<EmployeesController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        #region Reference Data

        [HttpGet("genders")]
        [AllowAnonymous] // Hoặc giữ nguyên quyền truy cập
        public async Task<ActionResult<ApiResponse<IEnumerable<GenderDto>>>> GetGenders()
        {
            try
            {
                var genders = await _context.Genders
                    .OrderBy(g => g.GenderID)
                    .Select(g => new GenderDto
                    {
                        GenderId = g.GenderID,
                        GenderCode = g.GenderCode,
                        GenderName = g.GenderName
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<GenderDto>>
                {
                    Success = true,
                    Data = genders,
                    Message = "Danh sách giới tính"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genders");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách giới tính"
                });
            }
        }

        [HttpGet("departments")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,EMPLOYEE,INTERN")]
        public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentInfoDto>>>> GetDepartments()
        {
            try
            {
                var departments = await _context.Departments
                    // Bỏ dòng Where(d => d.IsActive) nếu chưa có
                    .OrderBy(d => d.DepartmentName)
                    .Select(d => new DepartmentInfoDto
                    {
                        DepartmentId = d.DepartmentID,
                        DepartmentCode = d.DepartmentCode,
                        DepartmentName = d.DepartmentName,
                        Description = d.Description
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<DepartmentInfoDto>>
                {
                    Success = true,
                    Data = departments,
                    Message = "Danh sách phòng ban"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving departments");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách phòng ban"
                });
            }
        }

        [HttpGet("positions")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,EMPLOYEE,INTERN")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PositionInfoDto>>>> GetPositions()
        {
            try
            {
                var positions = await _context.Positions
                    // Bỏ dòng Where(p => p.IsActive) nếu chưa có
                    .OrderBy(p => p.PositionName)
                    .Select(p => new PositionInfoDto
                    {
                        PositionId = p.PositionID,
                        PositionCode = p.PositionCode,
                        PositionName = p.PositionName,
                        Description = p.Description
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<PositionInfoDto>>
                {
                    Success = true,
                    Data = positions,
                    Message = "Danh sách chức vụ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving positions");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách chức vụ"
                });
            }
        }

        #endregion

        #region Employee Management

        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        public async Task<ActionResult<ApiResponse<IEnumerable<EmployeeListDto>>>> GetEmployees(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? departmentId = null,
            [FromQuery] int? positionId = null,
            [FromQuery] short? status = null,
            [FromQuery] DateTime? hireDateFrom = null,
            [FromQuery] DateTime? hireDateTo = null)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Position)
                    .Include(e => e.Gender)
                    .AsQueryable();

                // Nếu là Manager hoặc Team Leader, chỉ xem nhân viên trong phòng của họ
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var currentUserId = GetCurrentUserId();

                if (currentUserRole == "MANAGER" || currentUserRole == "TEAM_LEADER")
                {
                    // Lấy thông tin user hiện tại
                    var currentUser = await _context.Users
                        .Include(u => u.Employee)
                        .FirstOrDefaultAsync(u => u.UserID == currentUserId);

                    if (currentUser?.Employee?.DepartmentID != null)
                    {
                        query = query.Where(e => e.DepartmentID == currentUser.Employee.DepartmentID);
                    }
                }

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(e =>
                        e.FullName.Contains(search) ||
                        e.EmployeeCode.Contains(search) ||
                        (e.Email != null && e.Email.Contains(search)) ||
                        (e.Phone != null && e.Phone.Contains(search)) ||
                        (e.IdentityNumber != null && e.IdentityNumber.Contains(search)));
                }

                if (departmentId.HasValue)
                {
                    query = query.Where(e => e.DepartmentID == departmentId);
                }

                if (positionId.HasValue)
                {
                    query = query.Where(e => e.PositionID == positionId);
                }

                if (status.HasValue)
                {
                    query = query.Where(e => e.Status == status);
                }

                if (hireDateFrom.HasValue)
                {
                    query = query.Where(e => e.HireDate >= hireDateFrom);
                }

                if (hireDateTo.HasValue)
                {
                    query = query.Where(e => e.HireDate <= hireDateTo);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var employees = await query
                    .OrderByDescending(e => e.EmployeeID)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var employeeDtos = employees.Select(e => new EmployeeListDto
                {
                    EmployeeId = e.EmployeeID,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    GenderName = e.Gender?.GenderName ?? "",
                    DateOfBirth = e.DateOfBirth,
                    DepartmentName = e.Department?.DepartmentName ?? "",
                    PositionName = e.Position?.PositionName ?? "",
                    Email = e.Email,
                    Phone = e.Phone,
                    HireDate = e.HireDate,
                    BaseSalary = e.BaseSalary,
                    Status = e.Status,
                    StatusText = GetStatusText(e.Status)
                }).ToList();

                var response = new ApiResponse<IEnumerable<EmployeeListDto>>
                {
                    Success = true,
                    Data = employeeDtos,
                    Message = "Danh sách nhân viên",
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
                _logger.LogError(ex, "Error retrieving employees");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách nhân viên"
                });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> GetEmployee(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Position)
                    .Include(e => e.Gender)
                    .FirstOrDefaultAsync(e => e.EmployeeID == id);

                if (employee == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Nhân viên với ID {id} không tồn tại"
                    });
                }

                // Kiểm tra quyền xem thông tin nhân viên
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var currentUserId = GetCurrentUserId();

                if (currentUserRole == "MANAGER" || currentUserRole == "TEAM_LEADER")
                {
                    var currentUser = await _context.Users
                        .Include(u => u.Employee)
                        .FirstOrDefaultAsync(u => u.UserID == currentUserId);

                    // Manager chỉ xem được nhân viên cùng phòng
                    if (currentUser?.Employee?.DepartmentID != employee.DepartmentID)
                    {
                        return Forbid();
                    }
                }
                else if (currentUserRole == "EMPLOYEE" || currentUserRole == "INTERN" || currentUserRole == "PROBATION_STAFF")
                {
                    // Nhân viên chỉ xem được thông tin của chính mình
                    var currentUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserID == currentUserId);

                    if (currentUser?.EmployeeID != id)
                    {
                        return Forbid();
                    }
                }

                // Get current contract
                var currentContract = await _context.Contracts
                    .Include(c => c.ContractType)
                    .Where(c => c.EmployeeID == id &&
                               (c.EndDate == null || c.EndDate >= DateTime.Today))
                    .OrderByDescending(c => c.StartDate)
                    .FirstOrDefaultAsync();

                var employeeDetail = new EmployeeDetailDto
                {
                    EmployeeId = employee.EmployeeID,
                    EmployeeCode = employee.EmployeeCode,
                    FullName = employee.FullName,
                    GenderId = employee.GenderID,
                    GenderName = employee.Gender?.GenderName ?? "",
                    DateOfBirth = employee.DateOfBirth,
                    DepartmentId = employee.DepartmentID,
                    DepartmentName = employee.Department?.DepartmentName ?? "",
                    PositionId = employee.PositionID,
                    PositionName = employee.Position?.PositionName ?? "",
                    HireDate = employee.HireDate,
                    BaseSalary = employee.BaseSalary,
                    Email = employee.Email,
                    Phone = employee.Phone,
                    Address = employee.Address,
                    Status = employee.Status,
                    StatusText = GetStatusText(employee.Status),
                    MaritalStatus = employee.MaritalStatus,
                    IdentityNumber = employee.IdentityNumber,
                    BankAccount = employee.BankAccount,
                    TaxCode = employee.TaxCode,
                    EmergencyContactName = employee.EmergencyContactName,
                    EmergencyContactPhone = employee.EmergencyContactPhone,
                    CreatedDate = employee.CreatedDate,
                    CurrentContract = currentContract != null ? new ContractInfoDto
                    {
                        ContractId = currentContract.ContractID,
                        ContractCode = currentContract.ContractCode,
                        ContractTypeName = currentContract.ContractType?.ContractTypeName ?? "",
                        StartDate = currentContract.StartDate,
                        EndDate = currentContract.EndDate,
                        Salary = currentContract.Salary
                    } : null,
                    HasAvatar = !string.IsNullOrEmpty(employee.AvatarPath)
                };

                return Ok(new ApiResponse<EmployeeDetailDto>
                {
                    Success = true,
                    Data = employeeDetail,
                    Message = "Thông tin chi tiết nhân viên"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin nhân viên"
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<ActionResult<ApiResponse<object>>> CreateEmployee(CreateEmployeeDto createDto)
        {
            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("=== CREATE EMPLOYEE REQUEST ===");
                _logger.LogInformation("EmployeeCode: {EmployeeCode}", createDto.EmployeeCode);
                _logger.LogInformation("Phone: {Phone}", createDto.Phone);
                _logger.LogInformation("CreateUserAccount: {CreateUserAccount}", createDto.CreateUserAccount);
                _logger.LogInformation("Username: {Username}", createDto.Username);

                // ===== VALIDATION =====
                if (!await IsValidDepartment(createDto.DepartmentId))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Phòng ban không tồn tại"
                    });
                }

                if (!await IsValidPosition(createDto.PositionId))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Chức vụ không tồn tại"
                    });
                }

                if (!createDto.HireDate.HasValue)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Ngày vào làm không được để trống"
                    });
                }

                if (createDto.DateOfBirth == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Ngày sinh không được để trống"
                    });
                }

                // 🔥 Bắt buộc có số điện thoại nếu tạo tài khoản
                if (createDto.CreateUserAccount && string.IsNullOrEmpty(createDto.Phone))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Phải có số điện thoại để tạo tài khoản"
                    });
                }

                // ===== CHECK TRÙNG =====
                var existingEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == createDto.EmployeeCode);

                if (existingEmployee != null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Mã nhân viên '{createDto.EmployeeCode}' đã tồn tại"
                    });
                }

                if (!string.IsNullOrEmpty(createDto.Email))
                {
                    var existingEmail = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Email == createDto.Email);

                    if (existingEmail != null)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Email '{createDto.Email}' đã được sử dụng"
                        });
                    }
                }

                // Check số điện thoại trùng
                if (!string.IsNullOrEmpty(createDto.Phone))
                {
                    var existingPhone = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Phone == createDto.Phone);

                    if (existingPhone != null)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Số điện thoại '{createDto.Phone}' đã được sử dụng"
                        });
                    }
                }

                // ===== CREATE EMPLOYEE =====
                var employee = new Employees
                {
                    EmployeeCode = createDto.EmployeeCode,
                    FullName = createDto.FullName,
                    GenderID = createDto.GenderId,
                    DateOfBirth = createDto.DateOfBirth.Value,
                    DepartmentID = createDto.DepartmentId,
                    PositionID = createDto.PositionId,
                    HireDate = createDto.HireDate.Value,
                    BaseSalary = createDto.BaseSalary,
                    Email = createDto.Email,
                    Phone = createDto.Phone,
                    Address = createDto.Address,
                    Status = 1,
                    MaritalStatus = createDto.MaritalStatus?.ToString(),
                    IdentityNumber = createDto.IdentityNumber,
                    BankAccount = createDto.BankAccount,
                    TaxCode = createDto.TaxCode,
                    EmergencyContactName = createDto.EmergencyContactName,
                    EmergencyContactPhone = createDto.EmergencyContactPhone,
                    CreatedDate = DateTime.Now,
                    BankPartnerID = createDto.BankPartnerID,
                    BankAccountName = createDto.BankAccountName
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Employee created with ID: {EmployeeId}", employee.EmployeeID);

                // ===== CONTRACT =====
                if (createDto.InitialContract != null)
                {
                    await CreateInitialContract(employee.EmployeeID, createDto.InitialContract);
                    _logger.LogInformation("✅ Contract created for employee {EmployeeId}", employee.EmployeeID);
                }

                // ===== CREATE USER ACCOUNT =====
                string? defaultPassword = null;
                bool userAccountCreated = false;

                if (createDto.CreateUserAccount)
                {
                    _logger.LogInformation("🔐 Attempting to create user account for employee {EmployeeId}", employee.EmployeeID);

                    try
                    {
                        defaultPassword = await CreateUserAccount(employee, createDto.Username);
                        userAccountCreated = true;
                        _logger.LogInformation("✅ User account created successfully for employee {EmployeeId}", employee.EmployeeID);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to create user account for employee {EmployeeId}", employee.EmployeeID);
                        // Rollback transaction nếu tạo user thất bại
                        await transaction.RollbackAsync();
                        return StatusCode(500, new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Tạo nhân viên thành công nhưng tạo tài khoản thất bại: {ex.Message}. Đã rollback."
                        });
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ CreateUserAccount is FALSE for employee {EmployeeId}", employee.EmployeeID);
                }

                // Commit transaction nếu mọi thứ thành công
                await transaction.CommitAsync();
                _logger.LogInformation("✅ Transaction committed successfully");

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = userAccountCreated
                        ? "Tạo nhân viên và tài khoản thành công"
                        : "Tạo nhân viên thành công (không tạo tài khoản)",
                    Data = new
                    {
                        EmployeeId = employee.EmployeeID,
                        DefaultPassword = defaultPassword,
                        UserAccountCreated = userAccountCreated,
                        Username = userAccountCreated ? (createDto.Username ?? employee.Email ?? employee.EmployeeCode) : null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating employee");
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo nhân viên: " + ex.Message
                });
            }
        }

        private async Task<string> CreateUserAccount(Employees EMPLOYEE, string? usernameInput)
        {
            _logger.LogInformation("CreateUserAccount started - EmployeeID: {EmployeeId}, Phone: {Phone}",
                EMPLOYEE.EmployeeID, EMPLOYEE.Phone);

            // Validate
            if (string.IsNullOrEmpty(EMPLOYEE.Phone))
            {
                throw new InvalidOperationException("Nhân viên chưa có số điện thoại, không thể tạo tài khoản");
            }

            // Tạo username
            string username = !string.IsNullOrEmpty(usernameInput)
                ? usernameInput
                : (!string.IsNullOrEmpty(EMPLOYEE.Email)
                    ? EMPLOYEE.Email
                    : EMPLOYEE.EmployeeCode);

            _logger.LogInformation("Generated username: {Username}", username);

            // Check username exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (existingUser != null)
            {
                throw new InvalidOperationException($"Username '{username}' đã tồn tại trong hệ thống");
            }

            // Check employee already has user account
            var existingUserForEmployee = await _context.Users
                .FirstOrDefaultAsync(u => u.EmployeeID == EMPLOYEE.EmployeeID);

            if (existingUserForEmployee != null)
            {
                throw new InvalidOperationException($"Nhân viên {EMPLOYEE.EmployeeCode} đã có tài khoản");
            }

            // Tạo user account
            var defaultPassword = EMPLOYEE.Phone;
            var passwordHasher = new PasswordHasher<User>();

            var user = new User
            {
                Username = username,
                EmployeeID = EMPLOYEE.EmployeeID,
                IsActive = true,
                CreatedDate = DateTime.Now,
                // Nếu có các field khác
                BankAccountNumber = null,
                BankName = null,
                BankBranch = null
            };

            user.PasswordHash = passwordHasher.HashPassword(user, defaultPassword);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created with ID: {UserId}, Username: {Username}", user.UserID, username);

            // Gán role Employee
            await AssignDefaultRole(user.UserID);
            _logger.LogInformation("Role assigned to user {UserId}", user.UserID);

            return defaultPassword;
        }

        private async Task AssignDefaultRole(int userId)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "EMPLOYEE");

            if (role == null)
            {
                _logger.LogWarning("Role 'EMPLOYEE' not found, skipping role assignment");
                return;
            }

            // Check if already assigned
            var existingUserRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserID == userId && ur.RoleID == role.RoleID);

            if (existingUserRole == null)
            {
                var userRole = new UserRoles
                {
                    UserID = userId,
                    RoleID = role.RoleID
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER")]
        public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> UpdateEmployee(
            int id, UpdateEmployeeDto updateDto)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);

                if (employee == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Nhân viên với ID {id} không tồn tại"
                    });
                }

                // Kiểm tra quyền update
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var currentUserId = GetCurrentUserId();

                if (currentUserRole == "MANAGER")
                {
                    var currentUser = await _context.Users
                        .Include(u => u.Employee)
                        .FirstOrDefaultAsync(u => u.UserID == currentUserId);

                    // Manager chỉ update được nhân viên cùng phòng
                    if (currentUser?.Employee?.DepartmentID != employee.DepartmentID)
                    {
                        return Forbid();
                    }

                    // Manager không được sửa lương
                    updateDto.BaseSalary = null;
                }
                else if (currentUserRole == "EMPLOYEE" || currentUserRole == "INTERN" || currentUserRole == "PROBATION_STAFF")
                {
                    // Nhân viên chỉ update được thông tin cá nhân của chính mình
                    var currentUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserID == currentUserId);

                    if (currentUser?.EmployeeID != id)
                    {
                        return Forbid();
                    }

                    // Nhân viên chỉ được sửa một số trường cơ bản
                    updateDto.BaseSalary = null;
                    updateDto.Status = null;
                    updateDto.DepartmentId = null;
                    updateDto.PositionId = null;
                }

                // Validate
                if (updateDto.DepartmentId.HasValue && !await IsValidDepartment(updateDto.DepartmentId.Value))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Phòng ban không tồn tại"
                    });
                }

                if (updateDto.PositionId.HasValue && !await IsValidPosition(updateDto.PositionId.Value))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Chức vụ không tồn tại"
                    });
                }

                // Check if new email exists
                if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != employee.Email)
                {
                    var existingEmail = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Email == updateDto.Email && e.EmployeeID != id);

                    if (existingEmail != null)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Email '{updateDto.Email}' đã được sử dụng"
                        });
                    }
                }

                // Store old values for history
                var oldDepartmentId = employee.DepartmentID;
                var oldPositionId = employee.PositionID;

                // Update Employee
                if (!string.IsNullOrEmpty(updateDto.FullName))
                    employee.FullName = updateDto.FullName;

                if (updateDto.GenderId.HasValue)
                    employee.GenderID = (byte?)updateDto.GenderId; // Cast sang byte?

                if (updateDto.DateOfBirth.HasValue)
                    employee.DateOfBirth = updateDto.DateOfBirth.Value;

                if (updateDto.DepartmentId.HasValue)
                    employee.DepartmentID = updateDto.DepartmentId;

                if (updateDto.PositionId.HasValue)
                    employee.PositionID = updateDto.PositionId;

                if (updateDto.BaseSalary.HasValue)
                    employee.BaseSalary = updateDto.BaseSalary;

                if (updateDto.Email != null)
                    employee.Email = updateDto.Email;

                if (updateDto.Phone != null)
                    employee.Phone = updateDto.Phone;

                if (updateDto.Address != null)
                    employee.Address = updateDto.Address;

                if (updateDto.Status.HasValue)
                    employee.Status = updateDto.Status.Value;

                if (updateDto.MaritalStatus != null)
                    employee.MaritalStatus = updateDto.MaritalStatus;

                if (updateDto.IdentityNumber != null)
                    employee.IdentityNumber = updateDto.IdentityNumber;

                if (updateDto.BankAccount != null)
                    employee.BankAccount = updateDto.BankAccount;

                if (updateDto.TaxCode != null)
                    employee.TaxCode = updateDto.TaxCode;

                if (updateDto.EmergencyContactName != null)
                    employee.EmergencyContactName = updateDto.EmergencyContactName;

                if (updateDto.EmergencyContactPhone != null)
                    employee.EmergencyContactPhone = updateDto.EmergencyContactPhone;

                if (updateDto.BankPartnerID.HasValue)
                    employee.BankPartnerID = updateDto.BankPartnerID;

                if (updateDto.BankAccountName != null)
                    employee.BankAccountName = updateDto.BankAccountName;

                await _context.SaveChangesAsync();

                // Create job history if department or position changed
                await CreateJobHistoryIfChanged(id, oldDepartmentId, employee.DepartmentID,
                    oldPositionId, employee.PositionID, "Cập nhật thông tin");

                return await GetEmployee(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật thông tin nhân viên"
                });
            }
        }

        [HttpPut("{id}/terminate")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<object>>> TerminateEmployee(
            int id, TerminateEmployeeDto terminateDto)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Contracts)
                    .FirstOrDefaultAsync(e => e.EmployeeID == id);

                if (employee == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Nhân viên với ID {id} không tồn tại"
                    });
                }

                if (employee.Status == 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nhân viên đã ngừng làm việc"
                    });
                }

                // Update employee status
                employee.Status = 0; // Terminated

                // Terminate active contracts
                var activeContracts = employee.Contracts
                    .Where(c => c.EndDate == null || c.EndDate >= DateTime.Today)
                    .ToList();

                foreach (var contract in activeContracts)
                {
                    contract.EndDate = terminateDto.TerminationDate;
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Ngừng làm việc thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating employee with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi ngừng làm việc"
                });
            }
        }

        [HttpPost("{id}/upload-avatar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,EMPLOYEE,INTERN,PROBATION_STAFF")]
        public async Task<ActionResult<ApiResponse<object>>> UploadAvatar(int id, IFormFile file)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Nhân viên với ID {id} không tồn tại"
                    });
                }

                // Kiểm tra quyền: chỉ nhân viên đó hoặc HR mới được upload avatar
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var currentUserId = GetCurrentUserId();

                if (currentUserRole == "EMPLOYEE" || currentUserRole == "INTERN" || currentUserRole == "PROBATION_STAFF")
                {
                    var currentUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserID == currentUserId);

                    if (currentUser?.EmployeeID != id)
                    {
                        return Forbid();
                    }
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Vui lòng chọn file ảnh"
                    });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)"
                    });
                }

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "File ảnh không được vượt quá 5MB"
                    });
                }

                // Create uploads directory if not exists
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var fileName = $"avatar_{id}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Update employee record
                var relativePath = $"/uploads/avatars/{fileName}";

                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(employee.AvatarPath))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath, employee.AvatarPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                employee.AvatarPath = relativePath;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { AvatarPath = relativePath },
                    Message = "Upload ảnh đại diện thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for employee with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi upload ảnh đại diện"
                });
            }
        }

        [HttpDelete("{id}/avatar")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,EMPLOYEE,INTERN,PROBATION_STAFF")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAvatar(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Nhân viên với ID {id} không tồn tại"
                    });
                }

                if (string.IsNullOrEmpty(employee.AvatarPath))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nhân viên không có ảnh đại diện"
                    });
                }

                // Delete file
                var filePath = Path.Combine(_environment.WebRootPath, employee.AvatarPath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Update database
                employee.AvatarPath = null;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa ảnh đại diện thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar for employee with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa ảnh đại diện"
                });
            }
        }

        #endregion

        #region Helper Methods

        private async Task<bool> IsValidDepartment(int? departmentId)
        {
            if (!departmentId.HasValue) return true;
            return await _context.Departments.AnyAsync(d => d.DepartmentID == departmentId);
        }

        private async Task<bool> IsValidPosition(int? positionId)
        {
            if (!positionId.HasValue) return true;
            return await _context.Positions.AnyAsync(p => p.PositionID == positionId);
        }

        private async Task CreateInitialContract(int employeeId, InitialContractDto contractDto)
        {
            var contract = new Contract
            {
                EmployeeID = employeeId,
                ContractCode = GenerateContractCode(),
                ContractTypeID = contractDto.ContractTypeId,
                StartDate = contractDto.StartDate,
                EndDate = contractDto.EndDate,
                Salary = contractDto.Salary,
                SignedDate = contractDto.SignedDate ?? DateTime.Today
            };

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
        }

        private async Task CreateUserAccount(int employeeId, string username, string password)
        {
            // Check if username already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (existingUser != null)
            {
                throw new Exception($"Tên đăng nhập '{username}' đã tồn tại");
            }

            var user = new User
            {
                EmployeeID = employeeId,
                Username = username,
                PasswordHash = HashPassword(password),
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign default employee role
            var employeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "EMPLOYEE");
            if (employeeRole != null)
            {
                var userRole = new UserRoles
                {
                    UserID = user.UserID,
                    RoleID = employeeRole.RoleID
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }
        }

        private async Task CreateJobHistoryIfChanged(int employeeId, int? oldDepartmentId, int? newDepartmentId,
            int? oldPositionId, int? newPositionId, string changeReason)
        {
            bool departmentChanged = oldDepartmentId != newDepartmentId;
            bool positionChanged = oldPositionId != newPositionId;

            if (departmentChanged || positionChanged)
            {
                // End previous job history if exists
                var previousHistory = await _context.JobHistories
                    .Where(jh => jh.EmployeeID == employeeId && jh.EndDate == null)
                    .FirstOrDefaultAsync();

                if (previousHistory != null)
                {
                    previousHistory.EndDate = DateTime.Now.AddDays(-1); // End yesterday
                }

                // Create new job history
                var jobHistory = new JobHistory
                {
                    EmployeeID = employeeId,
                    DepartmentID = newDepartmentId,
                    PositionID = newPositionId,
                    StartDate = DateTime.Now,
                    ChangeReason = changeReason
                };

                _context.JobHistories.Add(jobHistory);
                await _context.SaveChangesAsync();
            }
        }

        private string GetStatusText(short status)
        {
            return status switch
            {
                0 => "Đã nghỉ việc",
                1 => "Đang làm việc",
                2 => "Nghỉ phép",
                3 => "Tạm ngừng",
                _ => "Không xác định"
            };
        }

        private string GenerateContractCode()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"HD-{date}-{random}";
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private int? GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userId, out int id))
            {
                return id;
            }
            return null;
        }

        #endregion
    }

    #region DTO Classes

    public class EmployeeListDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string GenderName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime? HireDate { get; set; }
        public decimal? BaseSalary { get; set; }
        public short Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
    }

    public class EmployeeDetailDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public byte? GenderId { get; set; } // Sửa thành byte?
        public string GenderName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public int? DepartmentId { get; set; }
        public int? BankPartnerID { get; set; }

        public string? BankAccountName { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int? PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public DateTime? HireDate { get; set; }
        public decimal? BaseSalary { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public short Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string? MaritalStatus { get; set; }
        public string? IdentityNumber { get; set; }
        public string? BankAccount { get; set; }
        public string? TaxCode { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public DateTime CreatedDate { get; set; }
        public ContractInfoDto? CurrentContract { get; set; }
        public bool HasAvatar { get; set; }
    }

    public class CreateEmployeeDto
    {
        [Required]
        [StringLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        public byte? GenderId { get; set; }

        [Required]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public int? DepartmentId { get; set; }

        [Required]
        public int? PositionId { get; set; }

        [Required]
        public DateTime? HireDate { get; set; }

        public decimal? BaseSalary { get; set; }

        public int? BankPartnerID { get; set; }
        public string? BankAccountName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        // 🔥 BẮT BUỘC nếu tạo account
        [Phone]
        public string? Phone { get; set; }

        public string? Address { get; set; }

        // 👉 nên dùng int để đồng bộ DB
        public int? MaritalStatus { get; set; }

        public string? IdentityNumber { get; set; }
        public string? BankAccount { get; set; }
        public string? TaxCode { get; set; }

        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }

        public InitialContractDto? InitialContract { get; set; }

        public bool CreateUserAccount { get; set; } = false;

        public string? Username { get; set; }

        // ❌ XOÁ HOÀN TOÀN
        // public string? Password { get; set; }
    }

    public class UpdateEmployeeDto
    {
        [StringLength(100)]
        public string? FullName { get; set; }
        public byte? GenderId { get; set; } // Sửa thành byte?
        public DateTime? DateOfBirth { get; set; }
        public int? DepartmentId { get; set; }
        public int? PositionId { get; set; }
        public int? BankPartnerID { get; set; }

        public string? BankAccountName { get; set; }
        public decimal? BaseSalary { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public short? Status { get; set; }
        public string? MaritalStatus { get; set; }
        public string? IdentityNumber { get; set; }
        public string? BankAccount { get; set; }
        public string? TaxCode { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }

    public class TerminateEmployeeDto
    {
        [Required]
        public DateTime TerminationDate { get; set; }
        public string TerminationType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class ContractInfoDto
    {
        public int ContractId { get; set; }
        public string ContractCode { get; set; } = string.Empty;
        public string ContractTypeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Salary { get; set; }
    }

    public class InitialContractDto
    {
        [Required]
        public int ContractTypeId { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Required]
        public decimal Salary { get; set; }
        public DateTime? SignedDate { get; set; }
    }

    public class GenderDto
    {
        public byte GenderId { get; set; }
        public string GenderCode { get; set; } = string.Empty;
        public string GenderName { get; set; } = string.Empty;
    }

    // Đổi tên PositionDto thành PositionInfoDto để tránh xung đột với model Position
    public class PositionInfoDto
    {
        public int PositionId { get; set; }
        public string PositionCode { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    // Đổi tên DepartmentDto thành DepartmentInfoDto
    public class DepartmentInfoDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    #endregion
}