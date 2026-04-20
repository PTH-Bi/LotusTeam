using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using LotusTeam.DTOs;
using System.ComponentModel.DataAnnotations;

namespace LotusTeam.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ADMIN,HR")]
    public class ContractTypesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ContractTypesController> _logger;

        public ContractTypesController(AppDbContext context, ILogger<ContractTypesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ======================
        // GET ALL
        // ======================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _context.ContractTypes
                    .Select(ct => new ContractTypeDto
                    {
                        ContractTypeId = ct.ContractTypeID,
                        ContractTypeCode = ct.ContractTypeCode,
                        ContractTypeName = ct.ContractTypeName,
                        IsIndefinite = ct.IsIndefinite,
                        Description = ct.Description,
                        IsActive = ct.IsActive
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<ContractTypeDto>>
                {
                    Success = true,
                    Data = data,
                    Message = "Danh sách loại hợp đồng"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contract types");

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi lấy dữ liệu"
                });
            }
        }

        // ======================
        // GET BY ID
        // ======================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ct = await _context.ContractTypes.FindAsync(id);

            if (ct == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Không tìm thấy loại hợp đồng"
                });
            }

            var dto = new ContractTypeDto
            {
                ContractTypeId = ct.ContractTypeID,
                ContractTypeCode = ct.ContractTypeCode,
                ContractTypeName = ct.ContractTypeName,
                IsIndefinite = ct.IsIndefinite,
                Description = ct.Description,
                IsActive = ct.IsActive
            };

            return Ok(new ApiResponse<ContractTypeDto>
            {
                Success = true,
                Data = dto,
                Message = "Chi tiết loại hợp đồng"
            });
        }

        // ======================
        // POST (CREATE)
        // ======================
        [HttpPost]
        public async Task<IActionResult> Create(CreateContractTypeDto dto)
        {
            try
            {
                // Check duplicate code
                var exists = await _context.ContractTypes
                    .AnyAsync(x => x.ContractTypeCode == dto.ContractTypeCode);

                if (exists)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Mã loại hợp đồng đã tồn tại"
                    });
                }

                var entity = new ContractType
                {
                    ContractTypeCode = dto.ContractTypeCode,
                    ContractTypeName = dto.ContractTypeName,
                    IsIndefinite = dto.IsIndefinite,
                    Description = dto.Description,
                    IsActive = dto.IsActive
                };

                _context.ContractTypes.Add(entity);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<ContractTypeDto>
                {
                    Success = true,
                    Data = new ContractTypeDto
                    {
                        ContractTypeId = entity.ContractTypeID,
                        ContractTypeCode = entity.ContractTypeCode,
                        ContractTypeName = entity.ContractTypeName,
                        IsIndefinite = entity.IsIndefinite,
                        Description = entity.Description,
                        IsActive = entity.IsActive
                    },
                    Message = "Tạo loại hợp đồng thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract type");

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi tạo dữ liệu"
                });
            }
        }
    }

    public class ContractTypeDto
    {
        public int ContractTypeId { get; set; }
        public string ContractTypeCode { get; set; } = string.Empty;
        public string ContractTypeName { get; set; } = string.Empty;
        public bool IsIndefinite { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateContractTypeDto
    {
        [Required]
        public string ContractTypeCode { get; set; } = string.Empty;

        [Required]
        public string ContractTypeName { get; set; } = string.Empty;

        public bool IsIndefinite { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}