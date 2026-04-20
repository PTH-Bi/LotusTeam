using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using System.ComponentModel.DataAnnotations;

namespace LotusTeam.Services
{
    public class ContractService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ContractService> _logger;

        public ContractService(AppDbContext context, ILogger<ContractService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<ContractDto>>> GetContractsAsync(
            int page = 1,
            int pageSize = 20,
            string? search = null,
            int? employeeId = null,
            int? contractTypeId = null,
            string? status = null,
            DateTime? startDateFrom = null,
            DateTime? startDateTo = null,
            DateTime? endDateFrom = null,
            DateTime? endDateTo = null)
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

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c =>
                        c.ContractCode.Contains(search) ||
                        c.Employee.FullName.Contains(search) ||
                        c.Employee.EmployeeCode.Contains(search));
                }

                if (employeeId.HasValue)
                {
                    query = query.Where(c => c.EmployeeID == employeeId);
                }

                if (contractTypeId.HasValue)
                {
                    query = query.Where(c => c.ContractTypeID == contractTypeId);
                }

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
                {
                    query = query.Where(c => c.StartDate >= startDateFrom);
                }

                if (startDateTo.HasValue)
                {
                    query = query.Where(c => c.StartDate <= startDateTo);
                }

                if (endDateFrom.HasValue)
                {
                    query = query.Where(c => c.EndDate >= endDateFrom);
                }

                if (endDateTo.HasValue)
                {
                    query = query.Where(c => c.EndDate <= endDateTo);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
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
                    DepartmentName = c.Employee.Department != null ? c.Employee.Department.DepartmentName : "",
                    PositionName = c.Employee.Position != null ? c.Employee.Position.PositionName : "",
                    ContractTypeId = c.ContractTypeID,
                    ContractTypeName = c.ContractType.ContractTypeName,
                    ContractTypeCode = c.ContractType.ContractTypeCode,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    SignedDate = c.SignedDate,
                    Salary = c.Salary,
                    Status = GetContractStatus(c)
                }).ToList();

                return new ApiResponse<IEnumerable<ContractDto>>
                {
                    Success = true,
                    Data = contractDtos,
                    Message = "Danh sách hợp đồng",
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
                _logger.LogError(ex, "Error retrieving contracts");
                return new ApiResponse<IEnumerable<ContractDto>>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách hợp đồng",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<ContractDetailDto>> GetContractAsync(int id)
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
                {
                    return new ApiResponse<ContractDetailDto>
                    {
                        Success = false,
                        Message = $"Hợp đồng với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Get benefits for this employee
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
                    DepartmentName = contract.Employee.Department != null ?
                        contract.Employee.Department.DepartmentName : "",
                    PositionName = contract.Employee.Position != null ?
                        contract.Employee.Position.PositionName : "",
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

                return new ApiResponse<ContractDetailDto>
                {
                    Success = true,
                    Data = contractDetail,
                    Message = "Thông tin chi tiết hợp đồng",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contract with ID {Id}", id);
                return new ApiResponse<ContractDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin hợp đồng",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<ContractDetailDto>> CreateContractAsync(CreateContractDto createDto)
        {
            try
            {
                // Validate employee
                var employee = await _context.Employees.FindAsync(createDto.EmployeeId);
                if (employee == null)
                {
                    return new ApiResponse<ContractDetailDto>
                    {
                        Success = false,
                        Message = "Nhân viên không tồn tại",
                        StatusCode = 400
                    };
                }

                // Validate contract type
                var contractType = await _context.ContractTypes.FindAsync(createDto.ContractTypeId);
                if (contractType == null)
                {
                    return new ApiResponse<ContractDetailDto>
                    {
                        Success = false,
                        Message = "Loại hợp đồng không tồn tại",
                        StatusCode = 400
                    };
                }

                // Check for overlapping contracts
                var overlappingContract = await _context.Contracts
                    .Where(c => c.EmployeeID == createDto.EmployeeId &&
                               ((c.StartDate <= createDto.StartDate && (c.EndDate == null || c.EndDate >= createDto.StartDate)) ||
                                (createDto.EndDate.HasValue && c.StartDate <= createDto.EndDate.Value &&
                                 (c.EndDate == null || c.EndDate >= createDto.EndDate.Value))))
                    .FirstOrDefaultAsync();

                if (overlappingContract != null)
                {
                    return new ApiResponse<ContractDetailDto>
                    {
                        Success = false,
                        Message = $"Nhân viên đã có hợp đồng hiệu lực từ {overlappingContract.StartDate:dd/MM/yyyy}" +
                                 $"{(overlappingContract.EndDate.HasValue ? $" đến {overlappingContract.EndDate:dd/MM/yyyy}" : "")}",
                        StatusCode = 400
                    };
                }

                // Generate contract code
                var contractCode = GenerateContractCode();

                // Create contract
                var contract = new Contract
                {
                    ContractCode = contractCode,
                    EmployeeID = createDto.EmployeeId,
                    ContractTypeID = createDto.ContractTypeId,
                    StartDate = createDto.StartDate,
                    EndDate = createDto.EndDate,
                    SignedDate = createDto.SignedDate ?? createDto.StartDate,
                    Salary = createDto.Salary
                };

                _context.Contracts.Add(contract);
                await _context.SaveChangesAsync();

                return await GetContractAsync(contract.ContractID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract");
                return new ApiResponse<ContractDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo hợp đồng",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<ContractDetailDto>> UpdateContractAsync(int id, UpdateContractDto updateDto)
        {
            try
            {
                var contract = await _context.Contracts.FindAsync(id);

                if (contract == null)
                {
                    return new ApiResponse<ContractDetailDto>
                    {
                        Success = false,
                        Message = $"Hợp đồng với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Check if contract type needs update
                if (updateDto.ContractTypeId.HasValue)
                {
                    var contractType = await _context.ContractTypes.FindAsync(updateDto.ContractTypeId.Value);
                    if (contractType == null)
                    {
                        return new ApiResponse<ContractDetailDto>
                        {
                            Success = false,
                            Message = "Loại hợp đồng không tồn tại",
                            StatusCode = 400
                        };
                    }
                    contract.ContractTypeID = updateDto.ContractTypeId.Value;
                }

                // Update properties
                if (updateDto.StartDate.HasValue)
                    contract.StartDate = updateDto.StartDate.Value;

                if (updateDto.EndDate.HasValue)
                    contract.EndDate = updateDto.EndDate;

                if (updateDto.SignedDate.HasValue)
                    contract.SignedDate = updateDto.SignedDate.Value;

                if (updateDto.Salary.HasValue)
                    contract.Salary = updateDto.Salary.Value;

                await _context.SaveChangesAsync();

                return await GetContractAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contract with ID {Id}", id);
                return new ApiResponse<ContractDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật hợp đồng",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<ContractDetailDto>> ExtendContractAsync(int id, ExtendContractDto extendDto)
        {
            try
            {
                var contract = await _context.Contracts.FindAsync(id);

                if (contract == null)
                {
                    return new ApiResponse<ContractDetailDto>
                    {
                        Success = false,
                        Message = $"Hợp đồng với ID {id} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Check if contract is expired
                if (contract.EndDate.HasValue && contract.EndDate.Value < DateTime.Today)
                {
                    return new ApiResponse<ContractDetailDto>
                    {
                        Success = false,
                        Message = "Không thể gia hạn hợp đồng đã hết hạn",
                        StatusCode = 400
                    };
                }

                // Extend contract
                contract.EndDate = extendDto.NewEndDate;

                await _context.SaveChangesAsync();

                return await GetContractAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending contract with ID {Id}", id);
                return new ApiResponse<ContractDetailDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi gia hạn hợp đồng",
                    StatusCode = 500
                };
            }
        }

        // ========================= Benefits Management =========================
        public async Task<ApiResponse<IEnumerable<BenefitDto>>> GetEmployeeBenefitsAsync(int employeeId)
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

                return new ApiResponse<IEnumerable<BenefitDto>>
                {
                    Success = true,
                    Data = benefits,
                    Message = "Danh sách phúc lợi nhân viên",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving benefits for employee with ID {EmployeeId}", employeeId);
                return new ApiResponse<IEnumerable<BenefitDto>>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách phúc lợi",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<BenefitDto>> AddBenefitAsync(int employeeId, CreateBenefitDto createDto)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(employeeId);
                if (employee == null)
                {
                    return new ApiResponse<BenefitDto>
                    {
                        Success = false,
                        Message = $"Nhân viên với ID {employeeId} không tồn tại",
                        StatusCode = 404
                    };
                }

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

                return new ApiResponse<BenefitDto>
                {
                    Success = true,
                    Data = benefitDto,
                    Message = "Thêm phúc lợi thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding benefit for employee with ID {EmployeeId}", employeeId);
                return new ApiResponse<BenefitDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi thêm phúc lợi",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<BenefitDto>> UpdateBenefitAsync(int benefitId, UpdateBenefitDto updateDto)
        {
            try
            {
                var benefit = await _context.Benefits.FindAsync(benefitId);
                if (benefit == null)
                {
                    return new ApiResponse<BenefitDto>
                    {
                        Success = false,
                        Message = $"Phúc lợi với ID {benefitId} không tồn tại",
                        StatusCode = 404
                    };
                }

                // Update properties
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

                return new ApiResponse<BenefitDto>
                {
                    Success = true,
                    Data = benefitDto,
                    Message = "Cập nhật phúc lợi thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating benefit with ID {BenefitId}", benefitId);
                return new ApiResponse<BenefitDto>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật phúc lợi",
                    StatusCode = 500
                };
            }
        }

        public async Task<ApiResponse<object>> DeleteBenefitAsync(int benefitId)
        {
            try
            {
                var benefit = await _context.Benefits.FindAsync(benefitId);
                if (benefit == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Phúc lợi với ID {benefitId} không tồn tại",
                        StatusCode = 404
                    };
                }

                _context.Benefits.Remove(benefit);
                await _context.SaveChangesAsync();

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa phúc lợi thành công",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting benefit with ID {BenefitId}", benefitId);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa phúc lợi",
                    StatusCode = 500
                };
            }
        }

        // ========================= Helper Methods =========================
        private string GetContractStatus(Contract contract)
        {
            var today = DateTime.Today;

            if (contract.StartDate > today)
                return "Sắp hiệu lực";

            if (contract.EndDate.HasValue)
            {
                if (contract.EndDate.Value < today)
                    return "Đã hết hạn";

                if (contract.EndDate.Value.AddMonths(-1) <= today)
                    return "Sắp hết hạn";
            }

            return "Đang hiệu lực";
        }

        private string GenerateContractCode()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"HD-{date}-{random}";
        }

        // ========================= DTO Definitions =========================
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

        public class ContractDetailDto : ContractDto
        {
            public bool IsIndefinite { get; set; }
            public List<BenefitDto> Benefits { get; set; } = new();
        }

        public class CreateContractDto
        {
            [Required]
            public int EmployeeId { get; set; }

            [Required]
            public int ContractTypeId { get; set; }

            [Required]
            public DateTime StartDate { get; set; }

            public DateTime? EndDate { get; set; }
            public DateTime? SignedDate { get; set; }

            [Required]
            [Range(0, double.MaxValue)]
            public decimal Salary { get; set; }
        }

        public class UpdateContractDto
        {
            public int? ContractTypeId { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public DateTime? SignedDate { get; set; }
            public decimal? Salary { get; set; }
        }

        public class ExtendContractDto
        {
            [Required]
            public DateTime NewEndDate { get; set; }
        }

        public class BenefitDto
        {
            public int BenefitId { get; set; }

            [Required]
            [StringLength(50)]
            public string InsuranceType { get; set; } = string.Empty;

            public string? InsuranceNumber { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }

            [Range(0, 100)]
            public decimal? ContributionRate { get; set; }

            [Range(0, double.MaxValue)]
            public decimal? CompanyContribution { get; set; }

            [Range(0, double.MaxValue)]
            public decimal? EmployeeContribution { get; set; }
        }

        public class CreateBenefitDto
        {
            [Required]
            [StringLength(50)]
            public string InsuranceType { get; set; } = string.Empty;

            public string? InsuranceNumber { get; set; }

            [Required]
            public DateTime StartDate { get; set; }

            public DateTime? EndDate { get; set; }

            [Range(0, 100)]
            public decimal? ContributionRate { get; set; }

            [Range(0, double.MaxValue)]
            public decimal? CompanyContribution { get; set; }

            [Range(0, double.MaxValue)]
            public decimal? EmployeeContribution { get; set; }
        }

        public class UpdateBenefitDto
        {
            [StringLength(50)]
            public string? InsuranceType { get; set; }

            public string? InsuranceNumber { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }

            [Range(0, 100)]
            public decimal? ContributionRate { get; set; }

            [Range(0, double.MaxValue)]
            public decimal? CompanyContribution { get; set; }

            [Range(0, double.MaxValue)]
            public decimal? EmployeeContribution { get; set; }
        }
    }
}