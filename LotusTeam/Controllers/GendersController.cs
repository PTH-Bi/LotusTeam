using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace LotusTeam.Controllers
{
    /// <summary>
    /// API quản lý danh mục giới tính
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GendersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GendersController> _logger;

        public GendersController(AppDbContext context, ILogger<GendersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tất cả giới tính
        /// </summary>
        /// <remarks>
        /// **Role truy cập:** TẤT CẢ ROLE
        /// 
        /// **Dữ liệu trả về:**
        /// - GenderID: Mã giới tính (1: Nam, 2: Nữ, 3: Khác)
        /// - GenderCode: Mã code (M, F, O)
        /// - GenderName: Tên giới tính (Nam, Nữ, Khác)
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<GenderDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<IEnumerable<GenderDto>>>> GetAll()
        {
            try
            {
                var genders = await _context.Genders
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
                    Message = "Danh sách giới tính",
                    Data = genders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting genders");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách giới tính",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin giới tính theo ID
        /// </summary>
        /// <param name="id">Mã giới tính (1: Nam, 2: Nữ, 3: Khác)</param>
        /// <remarks>
        /// **Role truy cập:** TẤT CẢ ROLE
        /// </remarks>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<GenderDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<GenderDto>>> GetById(byte id)
        {
            try
            {
                var gender = await _context.Genders.FindAsync(id);

                if (gender == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy giới tính với ID {id}"
                    });
                }

                var genderDto = new GenderDto
                {
                    GenderId = gender.GenderID,
                    GenderCode = gender.GenderCode,
                    GenderName = gender.GenderName
                };

                return Ok(new ApiResponse<GenderDto>
                {
                    Success = true,
                    Message = "Thông tin giới tính",
                    Data = genderDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gender with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy thông tin giới tính",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Tạo mới giới tính (Chỉ dành cho Admin)
        /// </summary>
        /// <remarks>
        /// **Role truy cập:** SUPER_ADMIN, ADMIN
        /// 
        /// **Lưu ý:** 
        /// - GenderCode phải là duy nhất
        /// - GenderCode không được để trống
        /// - GenderName không được để trống
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<GenderDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<GenderDto>>> Create([FromBody] CreateGenderDto createDto)
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(createDto.GenderCode))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "GenderCode không được để trống"
                    });
                }

                if (string.IsNullOrWhiteSpace(createDto.GenderName))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "GenderName không được để trống"
                    });
                }

                // Check duplicate code
                var exists = await _context.Genders
                    .AnyAsync(x => x.GenderCode == createDto.GenderCode);

                if (exists)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"GenderCode '{createDto.GenderCode}' đã tồn tại"
                    });
                }

                // Find max ID for new gender
                byte maxId = 0;
                if (await _context.Genders.AnyAsync())
                {
                    maxId = await _context.Genders.MaxAsync(x => x.GenderID);
                }
                var newId = (byte)(maxId + 1);

                var gender = new Gender
                {
                    GenderID = newId,
                    GenderCode = createDto.GenderCode,
                    GenderName = createDto.GenderName
                };

                _context.Genders.Add(gender);
                await _context.SaveChangesAsync();

                var genderDto = new GenderDto
                {
                    GenderId = gender.GenderID,
                    GenderCode = gender.GenderCode,
                    GenderName = gender.GenderName
                };

                return CreatedAtAction(nameof(GetById), new { id = gender.GenderID }, new ApiResponse<GenderDto>
                {
                    Success = true,
                    Message = "Tạo giới tính thành công",
                    Data = genderDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gender");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo giới tính",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin giới tính (Chỉ dành cho Admin)
        /// </summary>
        /// <remarks>
        /// **Role truy cập:** SUPER_ADMIN, ADMIN
        /// 
        /// **Lưu ý:** 
        /// - Không thể cập nhật các giới tính mặc định (Nam, Nữ, Khác) nếu đã được sử dụng
        /// - GenderCode phải là duy nhất
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<object>>> Update(byte id, [FromBody] UpdateGenderDto updateDto)
        {
            try
            {
                var existing = await _context.Genders.FindAsync(id);
                if (existing == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy giới tính với ID {id}"
                    });
                }

                // Check if this is a system default gender being used
                var isUsed = await _context.Employees.AnyAsync(e => e.GenderID == id);
                if (isUsed && (id == 1 || id == 2))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể sửa giới tính mặc định (Nam/Nữ) vì đang được sử dụng"
                    });
                }

                // Check duplicate code (if changing)
                if (!string.IsNullOrEmpty(updateDto.GenderCode) && updateDto.GenderCode != existing.GenderCode)
                {
                    var exists = await _context.Genders
                        .AnyAsync(x => x.GenderCode == updateDto.GenderCode && x.GenderID != id);

                    if (exists)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"GenderCode '{updateDto.GenderCode}' đã tồn tại"
                        });
                    }
                    existing.GenderCode = updateDto.GenderCode;
                }

                // Update fields
                if (!string.IsNullOrEmpty(updateDto.GenderName))
                {
                    existing.GenderName = updateDto.GenderName;
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Cập nhật giới tính thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating gender with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật giới tính",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Xóa giới tính (Chỉ dành cho Admin)
        /// </summary>
        /// <remarks>
        /// **Role truy cập:** SUPER_ADMIN, ADMIN
        /// 
        /// **Lưu ý:** 
        /// - Không thể xóa giới tính mặc định (Nam, Nữ, Khác)
        /// - Không thể xóa giới tính đang được sử dụng bởi nhân viên
        /// </remarks>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<object>>> Delete(byte id)
        {
            try
            {
                var gender = await _context.Genders.FindAsync(id);

                if (gender == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy giới tính với ID {id}"
                    });
                }

                // Prevent deletion of default genders
                if (id == 1 || id == 2 || id == 3)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa giới tính mặc định (Nam/Nữ/Khác)"
                    });
                }

                // Check if gender is being used by any employee
                var isUsed = await _context.Employees.AnyAsync(e => e.GenderID == id);
                if (isUsed)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không thể xóa giới tính này vì đang được sử dụng bởi nhân viên"
                    });
                }

                _context.Genders.Remove(gender);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa giới tính thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gender with ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xóa giới tính",
                    Errors = ex.Message
                });
            }
        }
    }

    #region DTO Classes

    /// <summary>
    /// DTO danh sách giới tính
    /// </summary>
    public class GenderDto
    {
        /// <summary>Mã giới tính</summary>
        public byte GenderId { get; set; }

        /// <summary>Mã code (M, F, O)</summary>
        public string GenderCode { get; set; } = string.Empty;

        /// <summary>Tên giới tính (Nam, Nữ, Khác)</summary>
        public string GenderName { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO tạo mới giới tính
    /// </summary>
    public class CreateGenderDto
    {
        /// <summary>Mã code (M, F, O, ...)</summary>
        [Required(ErrorMessage = "GenderCode là bắt buộc")]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "GenderCode phải từ 1-10 ký tự")]
        public string GenderCode { get; set; } = string.Empty;

        /// <summary>Tên giới tính</summary>
        [Required(ErrorMessage = "GenderName là bắt buộc")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "GenderName phải từ 2-50 ký tự")]
        public string GenderName { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cập nhật giới tính
    /// </summary>
    public class UpdateGenderDto
    {
        /// <summary>Mã code (M, F, O, ...)</summary>
        [StringLength(10, ErrorMessage = "GenderCode tối đa 10 ký tự")]
        public string? GenderCode { get; set; }

        /// <summary>Tên giới tính</summary>
        [StringLength(50, ErrorMessage = "GenderName tối đa 50 ký tự")]
        public string? GenderName { get; set; }
    }

    #endregion
}