using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Hosting;

namespace LotusTeam.Services
{
    public class EmployeeService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeService> _logger;
        private readonly IWebHostEnvironment _environment;

        public EmployeeService(
            AppDbContext context,
            ILogger<EmployeeService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        public async Task<ApiResponse<IEnumerable<EmployeeListDto>>> GetEmployeesAsync(
            int page = 1,
            int pageSize = 20,
            string? search = null,
            int? departmentId = null,
            int? positionId = null,
            short? status = null,
            DateTime? hireDateFrom = null,
            DateTime? hireDateTo = null)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Position)
                    .Include(e => e.Gender)
                    .AsQueryable();

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

                return new ApiResponse<IEnumerable<EmployeeListDto>>
                {
                    Success = true,
                    Data = employeeDtos,
                    Message = "Danh sách nhân viên",
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
                _logger.LogError(ex, "Error retrieving employees");
                return new ApiResponse<IEnumerable<EmployeeListDto>>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách nhân viên",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<EmployeeDetailDto>> GetEmployeeAsync(int id)
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
                    return new ApiResponse<EmployeeDetailDto>
                    {
                        Success = false,
                        Message = $"Nhân viên với ID {id} không tồn tại",
                        StatusCode = 404
                    };
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

                return new ApiResponse<EmployeeDetailDto>
                {
                    Success = true,
                    Data = employeeDetail,
                    Message = "Thông tin chi tiết nhân viên",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee with ID {Id}", id);
                return new ApiResponse<EmployeeDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin nhân viên",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<EmployeeDetailDto>> CreateEmployeeAsync(CreateEmployeeDto createDto)
        {
            try
            {
                // Validate
                if (!await IsValidDepartment(createDto.DepartmentId))
                {
                    return new ApiResponse<EmployeeDetailDto>
                    {
                        Success = false,
                        Message = "Phòng ban không tồn tại",
                        StatusCode = 400
                    };
                }

                if (!await IsValidPosition(createDto.PositionId))
                {
                    return new ApiResponse<EmployeeDetailDto>
                    {
                        Success = false,
                        Message = "Chức vụ không tồn tại",
                        StatusCode = 400
                    };
                }

                // Check if employee code exists
                var existingEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == createDto.EmployeeCode);

                if (existingEmployee != null)
                {
                    return new ApiResponse<EmployeeDetailDto>
                    {
                        Success = false,
                        Message = $"Mã nhân viên '{createDto.EmployeeCode}' đã tồn tại",
                        StatusCode = 400
                    };
                }

                // Check if email exists
                if (!string.IsNullOrEmpty(createDto.Email))
                {
                    var existingEmail = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Email == createDto.Email);

                    if (existingEmail != null)
                    {
                        return new ApiResponse<EmployeeDetailDto>
                        {
                            Success = false,
                            Message = $"Email '{createDto.Email}' đã được sử dụng",
                            StatusCode = 400
                        };
                    }
                }

                // Validate required fields
                if (!createDto.HireDate.HasValue)
                {
                    return new ApiResponse<EmployeeDetailDto>
                    {
                        Success = false,
                        Message = "Ngày vào làm không được để trống",
                        StatusCode = 400
                    };
                }

                if (createDto.DateOfBirth == default)
                {
                    return new ApiResponse<EmployeeDetailDto>
                    {
                        Success = false,
                        Message = "Ngày sinh không được để trống",
                        StatusCode = 400
                    };
                }

                // Create employee
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
                    Status = 1, // Active
                    MaritalStatus = createDto.MaritalStatus,
                    IdentityNumber = createDto.IdentityNumber,
                    BankAccount = createDto.BankAccount,
                    TaxCode = createDto.TaxCode,
                    EmergencyContactName = createDto.EmergencyContactName,
                    EmergencyContactPhone = createDto.EmergencyContactPhone,
                    CreatedDate = DateTime.Now
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Create initial contract if provided
                if (createDto.InitialContract != null)
                {
                    await CreateInitialContractAsync(employee.EmployeeID, createDto.InitialContract);
                }

                // Create user account if requested
                if (createDto.CreateUserAccount)
                {
                    await CreateUserAccountAsync(employee.EmployeeID, createDto.Username, createDto.Password);
                }

                return await GetEmployeeAsync(employee.EmployeeID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                return new ApiResponse<EmployeeDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo nhân viên",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<EmployeeDetailDto>> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateDto)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);

                if (employee == null)
                {
                    return new ApiResponse<EmployeeDetailDto>
                    {
                        Success = false,
                        Message = $"Nhân viên với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Validate
                if (updateDto.DepartmentId.HasValue && !await IsValidDepartment(updateDto.DepartmentId.Value))
                {
                    return new ApiResponse<EmployeeDetailDto>
                    {
                        Success = false,
                        Message = "Phòng ban không tồn tại",
                        StatusCode = 400
                    };
                }

                if (updateDto.PositionId.HasValue && !await IsValidPosition(updateDto.PositionId.Value))
                {
                    return new ApiResponse<EmployeeDetailDto>
                    {
                        Success = false,
                        Message = "Chức vụ không tồn tại",
                        StatusCode = 400
                    };
                }

                // Check if new email exists
                if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != employee.Email)
                {
                    var existingEmail = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Email == updateDto.Email && e.EmployeeID != id);

                    if (existingEmail != null)
                    {
                        return new ApiResponse<EmployeeDetailDto>
                        {
                            Success = false,
                            Message = $"Email '{updateDto.Email}' đã được sử dụng",
                            StatusCode = 400
                        };
                    }
                }

                // Update properties
                if (!string.IsNullOrEmpty(updateDto.FullName))
                    employee.FullName = updateDto.FullName;

                if (updateDto.GenderId.HasValue)
                    employee.GenderID = updateDto.GenderId;

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

                await _context.SaveChangesAsync();

                return await GetEmployeeAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID {Id}", id);
                return new ApiResponse<EmployeeDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật thông tin nhân viên",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<object>> TerminateEmployeeAsync(int id, TerminateEmployeeDto terminateDto)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Contracts)
                    .FirstOrDefaultAsync(e => e.EmployeeID == id);

                if (employee == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Nhân viên với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                if (employee.Status == 0)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Nhân viên đã ngừng làm việc",
                        StatusCode = 400
                    };
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

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Ngừng làm việc thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating employee with ID {Id}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi ngừng làm việc",
                    StatusCode = 500
                };
            }
        }

        // ========================= Helper Methods =========================
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

        private async Task CreateInitialContractAsync(int employeeId, InitialContractDto contractDto)
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

        private async Task CreateUserAccountAsync(int employeeId, string username, string password)
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

        // ========================= DTO Definitions =========================
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
            public byte? GenderId { get; set; }
            public string GenderName { get; set; } = string.Empty;
            public DateTime? DateOfBirth { get; set; }
            public int? DepartmentId { get; set; }
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
            public DateTime? DateOfBirth { get; set; }
            public int? DepartmentId { get; set; }
            public int? PositionId { get; set; }
            public DateTime? HireDate { get; set; }
            public decimal? BaseSalary { get; set; }

            [EmailAddress]
            public string? Email { get; set; }

            [Phone]
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? MaritalStatus { get; set; }
            public string? IdentityNumber { get; set; }
            public string? BankAccount { get; set; }
            public string? TaxCode { get; set; }
            public string? EmergencyContactName { get; set; }
            public string? EmergencyContactPhone { get; set; }
            public InitialContractDto? InitialContract { get; set; }
            public bool CreateUserAccount { get; set; } = false;
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        public class UpdateEmployeeDto
        {
            [StringLength(100)]
            public string? FullName { get; set; }
            public byte? GenderId { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public int? DepartmentId { get; set; }
            public int? PositionId { get; set; }
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
    }
}