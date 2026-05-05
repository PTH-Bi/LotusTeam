using LotusTeam.DTOs;
using LotusTeam.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    [Route("api/payroll-bank-transfers")]
    [ApiController]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class PayrollBankTransfersController : ControllerBase
    {
        private readonly IPayrollBankTransferService _service;
        private readonly ILogger<PayrollBankTransfersController> _logger;

        public PayrollBankTransfersController(
            IPayrollBankTransferService service,
            ILogger<PayrollBankTransfersController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tất cả các lần chuyển lương ngân hàng
        /// </summary>
        /// <returns>Danh sách chuyển lương</returns>
        [HttpGet]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT,FINANCE_MANAGER")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var transfers = await _service.GetAllAsync();
                return Ok(new ApiResponse<IEnumerable<PayrollBankTransferDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách chuyển lương thành công",
                    Data = transfers,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách chuyển lương");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách chuyển lương",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết một lần chuyển lương theo ID
        /// </summary>
        /// <param name="id">ID chuyển lương</param>
        /// <returns>Chi tiết chuyển lương</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT,FINANCE_MANAGER")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var transfer = await _service.GetTransferDetailAsync(id);
                if (transfer == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy chuyển lương với ID {id}",
                        StatusCode = 404
                    });
                }

                return Ok(new ApiResponse<PayrollBankTransferDetailDto>
                {
                    Success = true,
                    Message = "Lấy chi tiết chuyển lương thành công",
                    Data = transfer,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết chuyển lương ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy chi tiết chuyển lương",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Tạo mới một lần chuyển lương (Chỉ HR và Admin)
        /// </summary>
        /// <param name="createDto">Thông tin tạo mới</param>
        /// <returns>Chuyển lương vừa tạo</returns>
        [HttpPost]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<IActionResult> Create([FromBody] CreatePayrollBankTransferDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    StatusCode = 400
                });
            }

            try
            {
                var transfer = await _service.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = transfer.TransferID }, new ApiResponse<PayrollBankTransferDto>
                {
                    Success = true,
                    Message = "Tạo chuyển lương thành công",
                    Data = transfer,
                    StatusCode = 201
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo chuyển lương cho payroll {PayrollId}", createDto.PayrollID);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Không thể tạo chuyển lương",
                    Errors = ex.Message,
                    StatusCode = 400
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin chuyển lương (Chỉ HR và Admin)
        /// </summary>
        /// <param name="id">ID chuyển lương</param>
        /// <param name="updateDto">Thông tin cập nhật</param>
        /// <returns>Chuyển lương sau khi cập nhật</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePayrollBankTransferDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    StatusCode = 400
                });
            }

            try
            {
                var transfer = await _service.UpdateAsync(id, updateDto);
                if (transfer == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy chuyển lương với ID {id}",
                        StatusCode = 404
                    });
                }

                return Ok(new ApiResponse<PayrollBankTransferDto>
                {
                    Success = true,
                    Message = "Cập nhật chuyển lương thành công",
                    Data = transfer,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật chuyển lương ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi cập nhật chuyển lương",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Xóa chuyển lương (Chỉ Admin cấp cao - chỉ xóa được ở trạng thái DRAFT/PENDING)
        /// </summary>
        /// <param name="id">ID chuyển lương</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy chuyển lương với ID {id}",
                        StatusCode = 404
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa chuyển lương thành công",
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa chuyển lương ID {Id}", id);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Không thể xóa chuyển lương",
                    Errors = ex.Message,
                    StatusCode = 400
                });
            }
        }

        /// <summary>
        /// Xử lý chuyển lương (gửi lệnh sang ngân hàng) - Chỉ HR và Admin
        /// </summary>
        /// <param name="id">ID chuyển lương</param>
        /// <returns>Chuyển lương sau khi xử lý</returns>
        [HttpPost("{id}/process")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT")]
        public async Task<IActionResult> ProcessTransfer(int id)
        {
            try
            {
                var transfer = await _service.ProcessTransferAsync(id);
                if (transfer == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Không tìm thấy chuyển lương với ID {id}",
                        StatusCode = 404
                    });
                }

                return Ok(new ApiResponse<PayrollBankTransferDto>
                {
                    Success = true,
                    Message = "Xử lý chuyển lương thành công",
                    Data = transfer,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý chuyển lương ID {Id}", id);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Không thể xử lý chuyển lương",
                    Errors = ex.Message,
                    StatusCode = 400
                });
            }
        }

        /// <summary>
        /// Lấy danh sách chuyển lương theo payroll ID
        /// </summary>
        /// <param name="payrollId">ID payroll</param>
        /// <returns>Danh sách chuyển lương</returns>
        [HttpGet("payroll/{payrollId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT,FINANCE_MANAGER")]
        public async Task<IActionResult> GetByPayrollId(int payrollId)
        {
            try
            {
                var transfers = await _service.GetByPayrollIdAsync(payrollId);
                return Ok(new ApiResponse<IEnumerable<PayrollBankTransferDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách chuyển lương theo payroll thành công",
                    Data = transfers,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chuyển lương theo payroll ID {PayrollId}", payrollId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi lấy danh sách chuyển lương",
                    Errors = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Lấy thống kê tổng hợp của một lần chuyển lương
        /// </summary>
        /// <param name="id">ID chuyển lương</param>
        /// <returns>Thống kê chuyển lương</returns>
        [HttpGet("{id}/summary")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT,FINANCE_MANAGER,DIRECTOR")]
        public async Task<IActionResult> GetSummary(int id)
        {
            try
            {
                var summary = await _service.GetTransferSummaryAsync(id);
                return Ok(new ApiResponse<BankTransferSummaryDto>
                {
                    Success = true,
                    Message = "Lấy thống kê chuyển lương thành công",
                    Data = summary,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thống kê chuyển lương ID {Id}", id);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                });
            }
        }
    }
}