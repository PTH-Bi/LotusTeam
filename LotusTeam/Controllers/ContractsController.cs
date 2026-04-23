using Microsoft.AspNetCore.Mvc;
using LotusTeam.Data;
using Microsoft.EntityFrameworkCore;
using LotusTeam.DTOs;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LotusTeam.Service;

namespace LotusTeam.API.Controllers
{
    /// <summary>
    /// API quản lý hợp đồng và phúc lợi nhân viên
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
    public class ContractsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ContractsController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ContractsController(
            AppDbContext context,
            ILogger<ContractsController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        #region Contract Management

        /// <summary>
        /// Lấy danh sách hợp đồng (có phân trang, lọc, tìm kiếm)
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER, HR_STAFF
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ContractDto>>>> GetContracts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? employeeId = null,
            [FromQuery] int? contractTypeId = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDateFrom = null,
            [FromQuery] DateTime? startDateTo = null,
            [FromQuery] DateTime? endDateFrom = null,
            [FromQuery] DateTime? endDateTo = null)
        {
            try
            {
                var query = _context.Contracts
                    .Include(c => c.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(c => c.Employee)
                        .ThenInclude(e => e.Position)
                    .Include(c => c.ContractType)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(c =>
                        c.ContractCode.Contains(search) ||
                        c.Employee.FullName.Contains(search) ||
                        c.Employee.EmployeeCode.Contains(search));

                if (employeeId.HasValue)
                    query = query.Where(c => c.EmployeeID == employeeId);

                if (contractTypeId.HasValue)
                    query = query.Where(c => c.ContractTypeID == contractTypeId);

                if (!string.IsNullOrEmpty(status))
                {
                    var today = DateTime.Today;
                    switch (status.ToUpper())
                    {
                        case "ACTIVE":
                            query = query.Where(c => c.EndDate == null || c.EndDate >= today);
                            break;
                        case "EXPIRED":
                            query = query.Where(c => c.EndDate < today);
                            break;
                        case "UPCOMING":
                            query = query.Where(c => c.StartDate > today);
                            break;
                    }
                }

                if (startDateFrom.HasValue)
                    query = query.Where(c => c.StartDate >= startDateFrom);

                if (startDateTo.HasValue)
                    query = query.Where(c => c.StartDate <= startDateTo);

                if (endDateFrom.HasValue)
                    query = query.Where(c => c.EndDate >= endDateFrom);

                if (endDateTo.HasValue)
                    query = query.Where(c => c.EndDate <= endDateTo);

                var totalCount = await query.CountAsync();

                var contracts = await query
                    .OrderByDescending(c => c.ContractID)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var contractDtos = contracts.Select(c => new ContractDto
                {
                    ContractId = c.ContractID,
                    ContractCode = c.ContractCode,
                    EmployeeId = c.EmployeeID,
                    EmployeeCode = c.Employee.EmployeeCode,
                    EmployeeName = c.Employee.FullName,
                    DepartmentName = c.Employee.Department?.DepartmentName ?? "",
                    PositionName = c.Employee.Position?.PositionName ?? "",
                    ContractTypeId = c.ContractTypeID,
                    ContractTypeName = c.ContractType.ContractTypeName,
                    ContractTypeCode = c.ContractType.ContractTypeCode,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    SignedDate = c.SignedDate,
                    Salary = c.Salary,
                    Status = GetContractStatus(c)
                }).ToList();

                return Ok(new ApiResponse<IEnumerable<ContractDto>>
                {
                    Success = true,
                    Data = contractDtos,
                    Message = "Danh sách hợp đồng",
                    Pagination = new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contracts");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách hợp đồng"
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết hợp đồng theo ID
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER, HR_STAFF
        /// </remarks>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ContractDetailDto>>> GetContract(int id)
        {
            try
            {
                var contract = await _context.Contracts
                    .Include(c => c.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(c => c.Employee)
                        .ThenInclude(e => e.Position)
                    .Include(c => c.ContractType)
                    .FirstOrDefaultAsync(c => c.ContractID == id);

                if (contract == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = $"Hợp đồng với ID {id} không tồn tại" });

                var benefits = await _context.Benefits
                    .Where(b => b.EmployeeID == contract.EmployeeID)
                    .Select(b => new BenefitDto
                    {
                        BenefitId = b.BenefitID,
                        InsuranceType = b.InsuranceType,
                        InsuranceNumber = b.InsuranceNumber,
                        StartDate = b.StartDate,
                        EndDate = b.EndDate,
                        ContributionRate = b.ContributionRate,
                        CompanyContribution = b.CompanyContribution,
                        EmployeeContribution = b.EmployeeContribution
                    })
                    .ToListAsync();

                var contractDetail = new ContractDetailDto
                {
                    ContractId = contract.ContractID,
                    ContractCode = contract.ContractCode,
                    EmployeeId = contract.EmployeeID,
                    EmployeeCode = contract.Employee.EmployeeCode,
                    EmployeeName = contract.Employee.FullName,
                    DepartmentName = contract.Employee.Department?.DepartmentName ?? "",
                    PositionName = contract.Employee.Position?.PositionName ?? "",
                    ContractTypeId = contract.ContractTypeID,
                    ContractTypeName = contract.ContractType.ContractTypeName,
                    ContractTypeCode = contract.ContractType.ContractTypeCode,
                    IsIndefinite = contract.ContractType.IsIndefinite,
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    SignedDate = contract.SignedDate,
                    Salary = contract.Salary,
                    Status = GetContractStatus(contract),
                    Benefits = benefits
                };

                return Ok(new ApiResponse<ContractDetailDto> { Success = true, Data = contractDetail, Message = "Thông tin chi tiết hợp đồng" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contract with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi khi lấy thông tin hợp đồng" });
            }
        }

        /// <summary>
        /// Tạo hợp đồng mới cho nhân viên
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER, HR_STAFF
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<ContractDetailDto>>> CreateContract(CreateContractDto createDto)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(createDto.EmployeeId);
                if (employee == null)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Nhân viên không tồn tại" });

                var contractType = await _context.ContractTypes.FindAsync(createDto.ContractTypeId);
                if (contractType == null)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Loại hợp đồng không tồn tại" });

                var overlappingContract = await _context.Contracts
                    .Where(c => c.EmployeeID == createDto.EmployeeId &&
                               ((c.StartDate <= createDto.StartDate && (c.EndDate == null || c.EndDate >= createDto.StartDate)) ||
                                (createDto.EndDate.HasValue && c.StartDate <= createDto.EndDate.Value &&
                                 (c.EndDate == null || c.EndDate >= createDto.EndDate.Value))))
                    .FirstOrDefaultAsync();

                if (overlappingContract != null)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = $"Nhân viên đã có hợp đồng hiệu lực từ {overlappingContract.StartDate:dd/MM/yyyy}{(overlappingContract.EndDate.HasValue ? $" đến {overlappingContract.EndDate:dd/MM/yyyy}" : "")}" });

                var contract = new Contract
                {
                    ContractCode = GenerateContractCode(),
                    EmployeeID = createDto.EmployeeId,
                    ContractTypeID = createDto.ContractTypeId,
                    StartDate = createDto.StartDate,
                    EndDate = createDto.EndDate,
                    SignedDate = createDto.SignedDate ?? createDto.StartDate,
                    Salary = createDto.Salary
                };

                _context.Contracts.Add(contract);
                await _context.SaveChangesAsync();

                return await GetContract(contract.ContractID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract");
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi khi tạo hợp đồng" });
            }
        }

        /// <summary>
        /// Cập nhật thông tin hợp đồng
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<ContractDetailDto>>> UpdateContract(int id, UpdateContractDto updateDto)
        {
            try
            {
                var contract = await _context.Contracts.FindAsync(id);
                if (contract == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = $"Hợp đồng với ID {id} không tồn tại" });

                if (updateDto.ContractTypeId.HasValue)
                {
                    var contractType = await _context.ContractTypes.FindAsync(updateDto.ContractTypeId.Value);
                    if (contractType == null)
                        return BadRequest(new ApiResponse<object> { Success = false, Message = "Loại hợp đồng không tồn tại" });
                    contract.ContractTypeID = updateDto.ContractTypeId.Value;
                }

                if (updateDto.StartDate.HasValue) contract.StartDate = updateDto.StartDate.Value;
                if (updateDto.EndDate.HasValue) contract.EndDate = updateDto.EndDate;
                if (updateDto.SignedDate.HasValue) contract.SignedDate = updateDto.SignedDate.Value;
                if (updateDto.Salary.HasValue) contract.Salary = updateDto.Salary.Value;

                await _context.SaveChangesAsync();
                return await GetContract(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contract with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi khi cập nhật hợp đồng" });
            }
        }

        /// <summary>
        /// Gia hạn hợp đồng
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER
        /// </remarks>
        [HttpPut("{id}/extend")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<ContractDetailDto>>> ExtendContract(int id, ExtendContractDto extendDto)
        {
            try
            {
                var contract = await _context.Contracts.FindAsync(id);
                if (contract == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = $"Hợp đồng với ID {id} không tồn tại" });

                if (contract.EndDate.HasValue && contract.EndDate.Value < DateTime.Today)
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Không thể gia hạn hợp đồng đã hết hạn" });

                contract.EndDate = extendDto.NewEndDate;
                await _context.SaveChangesAsync();

                return await GetContract(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending contract with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi khi gia hạn hợp đồng" });
            }
        }

        #endregion

        #region Benefits Management

        /// <summary>
        /// Lấy danh sách phúc lợi của nhân viên
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER, HR_STAFF
        /// </remarks>
        [HttpGet("employee/{employeeId}/benefits")]
        public async Task<ActionResult<ApiResponse<IEnumerable<BenefitDto>>>> GetEmployeeBenefits(int employeeId)
        {
            try
            {
                var benefits = await _context.Benefits
                    .Where(b => b.EmployeeID == employeeId)
                    .OrderBy(b => b.StartDate)
                    .Select(b => new BenefitDto
                    {
                        BenefitId = b.BenefitID,
                        InsuranceType = b.InsuranceType,
                        InsuranceNumber = b.InsuranceNumber,
                        StartDate = b.StartDate,
                        EndDate = b.EndDate,
                        ContributionRate = b.ContributionRate,
                        CompanyContribution = b.CompanyContribution,
                        EmployeeContribution = b.EmployeeContribution
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<BenefitDto>> { Success = true, Data = benefits, Message = "Danh sách phúc lợi nhân viên" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving benefits for employee with ID {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi khi lấy danh sách phúc lợi" });
            }
        }

        /// <summary>
        /// Thêm phúc lợi cho nhân viên
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER, HR_STAFF
        /// </remarks>
        [HttpPost("employee/{employeeId}/benefits")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<BenefitDto>>> AddBenefit(int employeeId, CreateBenefitDto createDto)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(employeeId);
                if (employee == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = $"Nhân viên với ID {employeeId} không tồn tại" });

                var benefit = new Benefit
                {
                    EmployeeID = employeeId,
                    InsuranceType = createDto.InsuranceType,
                    InsuranceNumber = createDto.InsuranceNumber,
                    StartDate = createDto.StartDate,
                    EndDate = createDto.EndDate,
                    ContributionRate = createDto.ContributionRate,
                    CompanyContribution = createDto.CompanyContribution,
                    EmployeeContribution = createDto.EmployeeContribution
                };

                _context.Benefits.Add(benefit);
                await _context.SaveChangesAsync();

                var benefitDto = new BenefitDto
                {
                    BenefitId = benefit.BenefitID,
                    InsuranceType = benefit.InsuranceType,
                    InsuranceNumber = benefit.InsuranceNumber,
                    StartDate = benefit.StartDate,
                    EndDate = benefit.EndDate,
                    ContributionRate = benefit.ContributionRate,
                    CompanyContribution = benefit.CompanyContribution,
                    EmployeeContribution = benefit.EmployeeContribution
                };

                return Ok(new ApiResponse<BenefitDto> { Success = true, Data = benefitDto, Message = "Thêm phúc lợi thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding benefit for employee with ID {EmployeeId}", employeeId);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi khi thêm phúc lợi" });
            }
        }

        /// <summary>
        /// Cập nhật phúc lợi
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER
        /// </remarks>
        [HttpPut("benefits/{benefitId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<BenefitDto>>> UpdateBenefit(int benefitId, UpdateBenefitDto updateDto)
        {
            try
            {
                var benefit = await _context.Benefits.FindAsync(benefitId);
                if (benefit == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = $"Phúc lợi với ID {benefitId} không tồn tại" });

                if (!string.IsNullOrEmpty(updateDto.InsuranceType))
                    benefit.InsuranceType = updateDto.InsuranceType;

                if (updateDto.InsuranceNumber != null)
                    benefit.InsuranceNumber = updateDto.InsuranceNumber;

                if (updateDto.StartDate.HasValue)
                    benefit.StartDate = updateDto.StartDate.Value;

                if (updateDto.EndDate.HasValue)
                    benefit.EndDate = updateDto.EndDate;

                if (updateDto.ContributionRate.HasValue)
                    benefit.ContributionRate = updateDto.ContributionRate;

                if (updateDto.CompanyContribution.HasValue)
                    benefit.CompanyContribution = updateDto.CompanyContribution;

                if (updateDto.EmployeeContribution.HasValue)
                    benefit.EmployeeContribution = updateDto.EmployeeContribution;

                await _context.SaveChangesAsync();

                var benefitDto = new BenefitDto
                {
                    BenefitId = benefit.BenefitID,
                    InsuranceType = benefit.InsuranceType,
                    InsuranceNumber = benefit.InsuranceNumber,
                    StartDate = benefit.StartDate,
                    EndDate = benefit.EndDate,
                    ContributionRate = benefit.ContributionRate,
                    CompanyContribution = benefit.CompanyContribution,
                    EmployeeContribution = benefit.EmployeeContribution
                };

                return Ok(new ApiResponse<BenefitDto> { Success = true, Data = benefitDto, Message = "Cập nhật phúc lợi thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating benefit with ID {BenefitId}", benefitId);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi khi cập nhật phúc lợi" });
            }
        }

        /// <summary>
        /// Xóa phúc lợi
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER
        /// </remarks>
        [HttpDelete("benefits/{benefitId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteBenefit(int benefitId)
        {
            try
            {
                var benefit = await _context.Benefits.FindAsync(benefitId);
                if (benefit == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = $"Phúc lợi với ID {benefitId} không tồn tại" });

                _context.Benefits.Remove(benefit);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object> { Success = true, Message = "Xóa phúc lợi thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting benefit with ID {BenefitId}", benefitId);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Đã xảy ra lỗi khi xóa phúc lợi" });
            }
        }

        #endregion

        #region Helper Methods

        private string GetContractStatus(Contract contract)
        {
            var today = DateTime.Today;
            if (contract.StartDate > today) return "Sắp hiệu lực";
            if (contract.EndDate.HasValue)
            {
                if (contract.EndDate.Value < today) return "Đã hết hạn";
                if (contract.EndDate.Value.AddMonths(-1) <= today) return "Sắp hết hạn";
            }
            return "Đang hiệu lực";
        }

        private string GenerateContractCode()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"HD-{date}-{random}";
        }

        private int? GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userId, out int id) ? id : null;
        }

        #endregion
    }

    #region DTO Classes

    /// <summary>
    /// DTO danh sách hợp đồng
    /// </summary>
    public class ContractDto
    {
        public int ContractId { get; set; }
        public string ContractCode { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public int ContractTypeId { get; set; }
        public string ContractTypeName { get; set; } = string.Empty;
        public string ContractTypeCode { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime SignedDate { get; set; }
        public decimal Salary { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO chi tiết hợp đồng (kèm phúc lợi)
    /// </summary>
    public class ContractDetailDto : ContractDto
    {
        public bool IsIndefinite { get; set; }
        public List<BenefitDto> Benefits { get; set; } = new();
    }

    /// <summary>
    /// DTO tạo hợp đồng mới
    /// </summary>
    public class CreateContractDto
    {
        [Required] public int EmployeeId { get; set; }
        [Required] public int ContractTypeId { get; set; }
        [Required] public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? SignedDate { get; set; }
        [Required][Range(0, double.MaxValue)] public decimal Salary { get; set; }
    }

    /// <summary>
    /// DTO cập nhật hợp đồng
    /// </summary>
    public class UpdateContractDto
    {
        public int? ContractTypeId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? SignedDate { get; set; }
        public decimal? Salary { get; set; }
    }

    /// <summary>
    /// DTO gia hạn hợp đồng
    /// </summary>
    public class ExtendContractDto
    {
        [Required] public DateTime NewEndDate { get; set; }
    }

    /// <summary>
    /// DTO phúc lợi
    /// </summary>
    public class BenefitDto
    {
        public int BenefitId { get; set; }
        [Required][StringLength(50)] public string InsuranceType { get; set; } = string.Empty;
        public string? InsuranceNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Range(0, 100)] public decimal? ContributionRate { get; set; }
        [Range(0, double.MaxValue)] public decimal? CompanyContribution { get; set; }
        [Range(0, double.MaxValue)] public decimal? EmployeeContribution { get; set; }
    }

    /// <summary>
    /// DTO tạo phúc lợi mới
    /// </summary>
    public class CreateBenefitDto
    {
        [Required][StringLength(50)] public string InsuranceType { get; set; } = string.Empty;
        public string? InsuranceNumber { get; set; }
        [Required] public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Range(0, 100)] public decimal? ContributionRate { get; set; }
        [Range(0, double.MaxValue)] public decimal? CompanyContribution { get; set; }
        [Range(0, double.MaxValue)] public decimal? EmployeeContribution { get; set; }
    }

    /// <summary>
    /// DTO cập nhật phúc lợi
    /// </summary>
    public class UpdateBenefitDto
    {
        [StringLength(50)] public string? InsuranceType { get; set; }
        public string? InsuranceNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Range(0, 100)] public decimal? ContributionRate { get; set; }
        [Range(0, double.MaxValue)] public decimal? CompanyContribution { get; set; }
        [Range(0, double.MaxValue)] public decimal? EmployeeContribution { get; set; }
    }

    #endregion
}