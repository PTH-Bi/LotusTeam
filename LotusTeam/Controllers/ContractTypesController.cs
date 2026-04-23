using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using Microsoft.AspNetCore.Authorization;
using LotusTeam.DTOs;
using System.ComponentModel.DataAnnotations;

namespace LotusTeam.API.Controllers
{
    /// <summary>
    /// API quản lý loại hợp đồng
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
    public class ContractTypesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ContractTypesController> _logger;

        public ContractTypesController(AppDbContext context, ILogger<ContractTypesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tất cả loại hợp đồng
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER, HR_STAFF
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ContractTypeDto>>>> GetAll()
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

        /// <summary>
        /// Lấy chi tiết loại hợp đồng theo ID
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER, HR_STAFF
        /// </remarks>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ContractTypeDto>>> GetById(int id)
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

        /// <summary>
        /// Tạo mới loại hợp đồng
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER
        /// <br/>HR_STAFF không có quyền tạo loại hợp đồng mới
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<ContractTypeDto>>> Create(CreateContractTypeDto dto)
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

        /// <summary>
        /// Cập nhật loại hợp đồng
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN, HR_MANAGER
        /// <br/>HR_STAFF không có quyền cập nhật loại hợp đồng
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<ActionResult<ApiResponse<ContractTypeDto>>> Update(int id, UpdateContractTypeDto dto)
        {
            try
            {
                var entity = await _context.ContractTypes.FindAsync(id);

                if (entity == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không tìm thấy loại hợp đồng"
                    });
                }

                // Check duplicate code (excluding current record)
                if (!string.IsNullOrEmpty(dto.ContractTypeCode) && dto.ContractTypeCode != entity.ContractTypeCode)
                {
                    var exists = await _context.ContractTypes
                        .AnyAsync(x => x.ContractTypeCode == dto.ContractTypeCode && x.ContractTypeID != id);

                    if (exists)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Mã loại hợp đồng đã tồn tại"
                        });
                    }
                    entity.ContractTypeCode = dto.ContractTypeCode;
                }

                if (!string.IsNullOrEmpty(dto.ContractTypeName))
                    entity.ContractTypeName = dto.ContractTypeName;

                if (dto.IsIndefinite.HasValue)
                    entity.IsIndefinite = dto.IsIndefinite.Value;

                if (dto.Description != null)
                    entity.Description = dto.Description;

                if (dto.IsActive.HasValue)
                    entity.IsActive = dto.IsActive.Value;

                await _context.SaveChangesAsync();

                var resultDto = new ContractTypeDto
                {
                    ContractTypeId = entity.ContractTypeID,
                    ContractTypeCode = entity.ContractTypeCode,
                    ContractTypeName = entity.ContractTypeName,
                    IsIndefinite = entity.IsIndefinite,
                    Description = entity.Description,
                    IsActive = entity.IsActive
                };

                return Ok(new ApiResponse<ContractTypeDto>
                {
                    Success = true,
                    Data = resultDto,
                    Message = "Cập nhật loại hợp đồng thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contract type with ID {Id}", id);

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi cập nhật dữ liệu"
                });
            }
        }

        /// <summary>
        /// Xóa loại hợp đồng
        /// </summary>
        /// <remarks>
        /// Role truy cập: SUPER_ADMIN, ADMIN
        /// <br/>Chỉ SUPER_ADMIN và ADMIN mới có quyền xóa loại hợp đồng
        /// </remarks>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            try
            {
                var entity = await _context.ContractTypes.FindAsync(id);

                if (entity == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không tìm thấy loại hợp đồng"
                    });
                }

                // Check if any contract is using this contract type
                var hasRelatedContracts = await _context.Contracts
                    .AnyAsync(c => c.ContractTypeID == id);

                if (hasRelatedContracts)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa loại hợp đồng này vì đang được sử dụng trong các hợp đồng"
                    });
                }

                _context.ContractTypes.Remove(entity);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa loại hợp đồng thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contract type with ID {Id}", id);

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi xóa dữ liệu"
                });
            }
        }
    }

    #region DTO Classes

    /// <summary>
    /// DTO loại hợp đồng
    /// </summary>
    public class ContractTypeDto
    {
        /// <summary>
        /// ID loại hợp đồng
        /// </summary>
        public int ContractTypeId { get; set; }

        /// <summary>
        /// Mã loại hợp đồng (VD: CD, TV, NDLT...)
        /// </summary>
        public string ContractTypeCode { get; set; } = string.Empty;

        /// <summary>
        /// Tên loại hợp đồng (VD: Chính thức, Thử việc, Thời vụ...)
        /// </summary>
        public string ContractTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Hợp đồng không thời hạn
        /// </summary>
        public bool IsIndefinite { get; set; }

        /// <summary>
        /// Mô tả loại hợp đồng
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Trạng thái kích hoạt
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO tạo mới loại hợp đồng
    /// </summary>
    public class CreateContractTypeDto
    {
        /// <summary>
        /// Mã loại hợp đồng (bắt buộc, duy nhất)
        /// </summary>
        [Required(ErrorMessage = "Mã loại hợp đồng là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã loại hợp đồng tối đa 20 ký tự")]
        public string ContractTypeCode { get; set; } = string.Empty;

        /// <summary>
        /// Tên loại hợp đồng (bắt buộc)
        /// </summary>
        [Required(ErrorMessage = "Tên loại hợp đồng là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên loại hợp đồng tối đa 100 ký tự")]
        public string ContractTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Hợp đồng không thời hạn
        /// </summary>
        public bool IsIndefinite { get; set; }

        /// <summary>
        /// Mô tả loại hợp đồng
        /// </summary>
        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        public string? Description { get; set; }

        /// <summary>
        /// Trạng thái kích hoạt (mặc định = true)
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO cập nhật loại hợp đồng
    /// </summary>
    public class UpdateContractTypeDto
    {
        /// <summary>
        /// Mã loại hợp đồng
        /// </summary>
        [StringLength(20, ErrorMessage = "Mã loại hợp đồng tối đa 20 ký tự")]
        public string? ContractTypeCode { get; set; }

        /// <summary>
        /// Tên loại hợp đồng
        /// </summary>
        [StringLength(100, ErrorMessage = "Tên loại hợp đồng tối đa 100 ký tự")]
        public string? ContractTypeName { get; set; }

        /// <summary>
        /// Hợp đồng không thời hạn
        /// </summary>
        public bool? IsIndefinite { get; set; }

        /// <summary>
        /// Mô tả loại hợp đồng
        /// </summary>
        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        public string? Description { get; set; }

        /// <summary>
        /// Trạng thái kích hoạt
        /// </summary>
        public bool? IsActive { get; set; }
    }

    #endregion
}