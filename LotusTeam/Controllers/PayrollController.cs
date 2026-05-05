using LotusTeam.DTOs;
using LotusTeam.Models;
using LotusTeam.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/payroll")]
    [Authorize] // Yêu cầu xác thực cho toàn bộ controller
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _service;
        private readonly ILogger<PayrollController> _logger;

        public PayrollController(IPayrollService service, ILogger<PayrollController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ======================================================
        // 1. TÍNH LƯƠNG THEO KỲ
        // POST: api/payroll/calculate?payPeriod=2025-01-01
        // ======================================================

        /// <summary>
        /// Tính lương cho tất cả nhân viên theo kỳ lương
        /// </summary>
        /// <param name="payPeriod">Kỳ lương (ngày đầu tháng)</param>
        /// <returns>Danh sách bảng lương đã tính</returns>
        [HttpPost("calculate")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT")]
        public async Task<IActionResult> Calculate([FromQuery] DateTime payPeriod)
        {
            if (payPeriod == default)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "PayPeriod không hợp lệ",
                    StatusCode = 400
                });
            }

            try
            {
                var result = await _service.CalculatePayrollAsync(payPeriod);
                return Ok(new ApiResponse<List<Payrolls>>
                {
                    Success = true,
                    Message = "Tính lương theo kỳ thành công",
                    Data = result,
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = result.Count,
                        Page = 1,
                        PageSize = result.Count,
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính lương theo kỳ {PayPeriod}", payPeriod);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi tính lương",
                    Errors = ex.Message,
                    StatusCode = 400
                });
            }
        }

        // ======================================================
        // 2. TÍNH LƯƠNG HÀNG LOẠT (THEO DANH SÁCH NHÂN VIÊN)
        // POST: api/payroll/calculate-bulk?payPeriod=2025-01-01
        // Body: [1,2,3]
        // ======================================================

        /// <summary>
        /// Tính lương cho danh sách nhân viên được chọn
        /// </summary>
        /// <param name="payPeriod">Kỳ lương</param>
        /// <param name="employeeIds">Danh sách ID nhân viên</param>
        /// <returns>Danh sách bảng lương</returns>
        [HttpPost("calculate-bulk")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT")]
        public async Task<IActionResult> CalculateBulk(
            [FromQuery] DateTime payPeriod,
            [FromBody] List<int> employeeIds)
        {
            if (payPeriod == default)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "PayPeriod không hợp lệ",
                    StatusCode = 400
                });
            }

            if (employeeIds == null || !employeeIds.Any())
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Danh sách EmployeeIds không được để trống",
                    StatusCode = 400
                });
            }

            try
            {
                var result = await _service.CalculatePayrollBulkAsync(payPeriod, employeeIds);
                return Ok(new ApiResponse<List<Payrolls>>
                {
                    Success = true,
                    Message = "Tính lương hàng loạt thành công",
                    Data = result,
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = result.Count,
                        Page = 1,
                        PageSize = result.Count,
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính lương hàng loạt cho kỳ {PayPeriod}", payPeriod);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi tính lương hàng loạt",
                    Errors = ex.Message,
                    StatusCode = 400
                });
            }
        }

        // ======================================================
        // 3. XEM BẢNG LƯƠNG THEO NHÂN VIÊN
        // GET: api/payroll/employee/5
        // ======================================================

        /// <summary>
        /// Xem bảng lương của một nhân viên
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Danh sách bảng lương của nhân viên</returns>
        [HttpGet("employee/{employeeId:int}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,ACCOUNTANT,EMPLOYEE")]
        public async Task<IActionResult> GetByEmployee(int employeeId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "EmployeeId không hợp lệ",
                    StatusCode = 400
                });
            }

            // Kiểm tra quyền: nhân viên chỉ xem được lương của mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "ACCOUNTANT" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            var result = await _service.GetPayrollByEmployeeAsync(employeeId);

            return Ok(new ApiResponse<List<Payrolls>>
            {
                Success = true,
                Message = "Lấy bảng lương theo nhân viên thành công",
                Data = result,
                StatusCode = 200,
                Pagination = new PaginationInfo
                {
                    TotalCount = result.Count,
                    Page = 1,
                    PageSize = result.Count,
                }
            });
        }

        // ======================================================
        // 4. XEM CHI TIẾT BẢNG LƯƠNG
        // GET: api/payroll/10
        // ======================================================

        /// <summary>
        /// Xem chi tiết một bảng lương
        /// </summary>
        /// <param name="payrollId">ID bảng lương</param>
        /// <returns>Chi tiết bảng lương</returns>
        [HttpGet("{payrollId:int}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,ACCOUNTANT,EMPLOYEE")]
        public async Task<IActionResult> GetDetail(int payrollId)
        {
            if (payrollId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "PayrollId không hợp lệ",
                    StatusCode = 400
                });
            }

            var payroll = await _service.GetPayrollDetailAsync(payrollId);

            if (payroll == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Không tìm thấy bảng lương",
                    StatusCode = 404
                });
            }

            // Kiểm tra quyền: nhân viên chỉ xem được lương của mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "ACCOUNTANT" };

            if (!allowedRoles.Contains(currentUserRole) && payroll.EmployeeID != currentUserId)
            {
                return Forbid();
            }

            return Ok(new ApiResponse<Payrolls>
            {
                Success = true,
                Message = "Lấy chi tiết bảng lương thành công",
                Data = payroll,
                StatusCode = 200
            });
        }

        // ======================================================
        // 5. LẤY DANH SÁCH TẤT CẢ BẢNG LƯƠNG (CÓ FILTER)
        // GET: api/payroll/all?month=4&year=2026&page=1&pageSize=10
        // ======================================================

        /// <summary>
        /// Lấy danh sách tất cả bảng lương (có phân trang và lọc)
        /// </summary>
        /// <param name="month">Tháng (1-12)</param>
        /// <param name="year">Năm</param>
        /// <param name="page">Trang số</param>
        /// <param name="pageSize">Số bản ghi mỗi trang</param>
        /// <returns>Danh sách bảng lương</returns>
        [HttpGet("all")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT,FINANCE_MANAGER,DIRECTOR")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? month,
            [FromQuery] int? year,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            try
            {
                var result = await _service.GetAllPayrollsAsync(month, year, page, pageSize);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Lấy danh sách bảng lương thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách bảng lương");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi lấy danh sách bảng lương",
                    Errors = ex.Message,
                    StatusCode = 400
                });
            }
        }

        // ======================================================
        // 5b. XEM BẢNG LƯƠNG DẠNG FLAT (NHƯ TRONG ẢNH)
        // GET: api/payroll/flat?payPeriod=2026-04-01
        // ======================================================

        /// <summary>
        /// Xem bảng lương dạng phẳng (flat view) cho báo cáo
        /// </summary>
        /// <param name="payPeriod">Kỳ lương</param>
        /// <returns>Bảng lương dạng flat</returns>
        [HttpGet("flat")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT,FINANCE_MANAGER,DIRECTOR")]
        public async Task<IActionResult> GetFlatPayroll([FromQuery] DateTime payPeriod)
        {
            if (payPeriod == default)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "PayPeriod không hợp lệ",
                    StatusCode = 400
                });
            }

            try
            {
                var result = await _service.GetPayrollFlatAsync(payPeriod);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Lấy bảng lương flat thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy bảng lương flat cho kỳ {PayPeriod}", payPeriod);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi lấy bảng lương flat",
                    Errors = ex.Message,
                    StatusCode = 400
                });
            }
        }

        // ======================================================
        // 6. PHÊ DUYỆT BẢNG LƯƠNG THEO KỲ
        // POST: api/payroll/approve?payPeriod=2025-01-01
        // ======================================================

        /// <summary>
        /// Phê duyệt bảng lương theo kỳ
        /// </summary>
        /// <param name="payPeriod">Kỳ lương</param>
        /// <returns>Kết quả phê duyệt</returns>
        [HttpPost("approve")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,ACCOUNTANT")]
        public async Task<IActionResult> Approve([FromQuery] DateTime payPeriod)
        {
            if (payPeriod == default)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "PayPeriod không hợp lệ",
                    StatusCode = 400
                });
            }

            var success = await _service.ApprovePayrollAsync(payPeriod);

            if (!success)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Không có bảng lương nào để phê duyệt",
                    StatusCode = 404
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Phê duyệt bảng lương thành công",
                Data = new { PayPeriod = payPeriod.ToString("yyyy-MM") },
                StatusCode = 200
            });
        }

        // ======================================================
        // 7. TẠO SNAPSHOT THUẾ TNCN
        // POST: api/payroll/tax-snapshot/10
        // ======================================================

        /// <summary>
        /// Tạo snapshot thuế TNCN cho một bảng lương
        /// </summary>
        /// <param name="payrollId">ID bảng lương</param>
        /// <returns>Snapshot thuế</returns>
        [HttpPost("tax-snapshot/{payrollId:int}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,ACCOUNTANT")]
        public async Task<IActionResult> SnapshotTax(int payrollId)
        {
            if (payrollId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "PayrollId không hợp lệ",
                    StatusCode = 400
                });
            }

            try
            {
                var snapshot = await _service.CreateTaxSnapshotAsync(payrollId);
                return Ok(new ApiResponse<PayrollTaxSnapshot>
                {
                    Success = true,
                    Message = "Tạo snapshot thuế thành công",
                    Data = snapshot,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo snapshot thuế cho payroll {PayrollId}", payrollId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Lỗi khi tạo snapshot thuế",
                    Errors = ex.Message,
                    StatusCode = 400
                });
            }
        }

        // ======================================================
        // 8. LỊCH SỬ TRẢ LƯƠNG NHÂN VIÊN
        // GET: api/payroll/history/5
        // ======================================================

        /// <summary>
        /// Lấy lịch sử trả lương của nhân viên
        /// </summary>
        /// <param name="employeeId">ID nhân viên</param>
        /// <returns>Lịch sử lương</returns>
        [HttpGet("history/{employeeId:int}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,ACCOUNTANT,EMPLOYEE")]
        public async Task<IActionResult> History(int employeeId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "EmployeeId không hợp lệ",
                    StatusCode = 400
                });
            }

            // Kiểm tra quyền: nhân viên chỉ xem được lịch sử của mình
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "ACCOUNTANT" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            var result = await _service.GetPayrollHistoryAsync(employeeId);

            return Ok(new ApiResponse<List<Payrolls>>
            {
                Success = true,
                Message = "Lấy lịch sử lương thành công",
                Data = result,
                StatusCode = 200
            });
        }

        // ======================================================
        // 9. QUẢN LÝ PHỤ CẤP
        // ======================================================

        /// <summary>
        /// Thêm phụ cấp cho nhân viên
        /// </summary>
        [HttpPost("allowance")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT")]
        public async Task<IActionResult> AddAllowance(
            [FromQuery] int employeeId,
            [FromQuery] DateTime month,
            [FromQuery] string name,
            [FromQuery] decimal amount,
            [FromQuery] string? note = null)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "EmployeeId không hợp lệ", StatusCode = 400 });
            }
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Tên phụ cấp không được để trống", StatusCode = 400 });
            }
            if (amount <= 0)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Số tiền phải lớn hơn 0", StatusCode = 400 });
            }

            try
            {
                var result = await _service.AddAllowanceAsync(employeeId, month, name, amount, note);
                return Ok(new ApiResponse<Allowances>
                {
                    Success = true,
                    Message = "Thêm phụ cấp thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, StatusCode = 400 });
            }
        }

        /// <summary>
        /// Lấy danh sách phụ cấp của nhân viên theo tháng
        /// </summary>
        [HttpGet("allowance/{employeeId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,ACCOUNTANT,EMPLOYEE")]
        public async Task<IActionResult> GetAllowances(int employeeId, [FromQuery] DateTime month)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "EmployeeId không hợp lệ", StatusCode = 400 });
            }

            // Kiểm tra quyền
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "ACCOUNTANT" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            var result = await _service.GetAllowancesByMonthAsync(employeeId, month);
            return Ok(new ApiResponse<List<Allowances>>
            {
                Success = true,
                Message = "Lấy danh sách phụ cấp thành công",
                Data = result,
                StatusCode = 200
            });
        }

        // ======================================================
        // 10. QUẢN LÝ THƯỞNG
        // ======================================================

        /// <summary>
        /// Thêm thưởng cho nhân viên
        /// </summary>
        [HttpPost("bonus")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT")]
        public async Task<IActionResult> AddBonus(
            [FromQuery] int employeeId,
            [FromQuery] DateTime month,
            [FromQuery] string name,
            [FromQuery] decimal amount,
            [FromQuery] string? reason = null)
        {
            if (employeeId <= 0) return BadRequest(new ApiResponse<object> { Success = false, Message = "EmployeeId không hợp lệ", StatusCode = 400 });
            if (string.IsNullOrEmpty(name)) return BadRequest(new ApiResponse<object> { Success = false, Message = "Tên thưởng không được để trống", StatusCode = 400 });
            if (amount <= 0) return BadRequest(new ApiResponse<object> { Success = false, Message = "Số tiền phải lớn hơn 0", StatusCode = 400 });

            try
            {
                var result = await _service.AddBonusAsync(employeeId, month, name, amount, reason);
                return Ok(new ApiResponse<Bonus>
                {
                    Success = true,
                    Message = "Thêm thưởng thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, StatusCode = 400 });
            }
        }

        /// <summary>
        /// Lấy danh sách thưởng của nhân viên theo tháng
        /// </summary>
        [HttpGet("bonus/{employeeId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,ACCOUNTANT,EMPLOYEE")]
        public async Task<IActionResult> GetBonuses(int employeeId, [FromQuery] DateTime month)
        {
            if (employeeId <= 0) return BadRequest(new ApiResponse<object> { Success = false, Message = "EmployeeId không hợp lệ", StatusCode = 400 });

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "ACCOUNTANT" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            var result = await _service.GetBonusesByMonthAsync(employeeId, month);
            return Ok(new ApiResponse<List<Bonus>>
            {
                Success = true,
                Message = "Lấy danh sách thưởng thành công",
                Data = result,
                StatusCode = 200
            });
        }

        // ======================================================
        // 11. QUẢN LÝ KHẤU TRỪ
        // ======================================================

        /// <summary>
        /// Thêm khấu trừ cho nhân viên
        /// </summary>
        [HttpPost("deduction")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT")]
        public async Task<IActionResult> AddDeduction(
            [FromQuery] int employeeId,
            [FromQuery] DateTime month,
            [FromQuery] string name,
            [FromQuery] decimal amount,
            [FromQuery] string? note = null)
        {
            if (employeeId <= 0) return BadRequest(new ApiResponse<object> { Success = false, Message = "EmployeeId không hợp lệ", StatusCode = 400 });
            if (string.IsNullOrEmpty(name)) return BadRequest(new ApiResponse<object> { Success = false, Message = "Tên khấu trừ không được để trống", StatusCode = 400 });
            if (amount <= 0) return BadRequest(new ApiResponse<object> { Success = false, Message = "Số tiền phải lớn hơn 0", StatusCode = 400 });

            try
            {
                var result = await _service.AddDeductionAsync(employeeId, month, name, amount, note);
                return Ok(new ApiResponse<Deduction>
                {
                    Success = true,
                    Message = "Thêm khấu trừ thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, StatusCode = 400 });
            }
        }

        /// <summary>
        /// Lấy danh sách khấu trừ của nhân viên theo tháng
        /// </summary>
        [HttpGet("deduction/{employeeId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,ACCOUNTANT,EMPLOYEE")]
        public async Task<IActionResult> GetDeductions(int employeeId, [FromQuery] DateTime month)
        {
            if (employeeId <= 0) return BadRequest(new ApiResponse<object> { Success = false, Message = "EmployeeId không hợp lệ", StatusCode = 400 });

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "ACCOUNTANT" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            var result = await _service.GetDeductionsByMonthAsync(employeeId, month);
            return Ok(new ApiResponse<List<Deduction>>
            {
                Success = true,
                Message = "Lấy danh sách khấu trừ thành công",
                Data = result,
                StatusCode = 200
            });
        }

        // ======================================================
        // 12. QUẢN LÝ NGƯỜI PHỤ THUỘC
        // ======================================================

        /// <summary>
        /// Thêm người phụ thuộc cho nhân viên
        /// </summary>
        [HttpPost("dependent")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<IActionResult> AddDependent(
            [FromQuery] int employeeId,
            [FromQuery] string fullName,
            [FromQuery] string relationship,
            [FromQuery] DateTime birthDate,
            [FromQuery] string? identityNumber = null)
        {
            if (employeeId <= 0) return BadRequest(new ApiResponse<object> { Success = false, Message = "EmployeeId không hợp lệ", StatusCode = 400 });
            if (string.IsNullOrEmpty(fullName)) return BadRequest(new ApiResponse<object> { Success = false, Message = "Tên người phụ thuộc không được để trống", StatusCode = 400 });
            if (string.IsNullOrEmpty(relationship)) return BadRequest(new ApiResponse<object> { Success = false, Message = "Quan hệ không được để trống", StatusCode = 400 });

            try
            {
                var result = await _service.AddDependentAsync(employeeId, fullName, relationship, birthDate, identityNumber);
                return Ok(new ApiResponse<Dependent>
                {
                    Success = true,
                    Message = "Thêm người phụ thuộc thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, StatusCode = 400 });
            }
        }

        /// <summary>
        /// Lấy danh sách người phụ thuộc của nhân viên
        /// </summary>
        [HttpGet("dependent/{employeeId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,EMPLOYEE")]
        public async Task<IActionResult> GetDependents(int employeeId, [FromQuery] bool onlyActive = true)
        {
            if (employeeId <= 0) return BadRequest(new ApiResponse<object> { Success = false, Message = "EmployeeId không hợp lệ", StatusCode = 400 });

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            var result = await _service.GetDependentsByEmployeeAsync(employeeId, onlyActive);
            return Ok(new ApiResponse<List<Dependent>>
            {
                Success = true,
                Message = "Lấy danh sách người phụ thuộc thành công",
                Data = result,
                StatusCode = 200
            });
        }

        /// <summary>
        /// Ngưng phụ cấp cho người phụ thuộc
        /// </summary>
        [HttpPut("dependent/{dependentId}/deactivate")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF")]
        public async Task<IActionResult> DeactivateDependent(int dependentId, [FromQuery] DateTime? endDate = null)
        {
            if (dependentId <= 0) return BadRequest(new ApiResponse<object> { Success = false, Message = "DependentId không hợp lệ", StatusCode = 400 });

            var result = await _service.DeactivateDependentAsync(dependentId, endDate);
            if (!result) return NotFound(new ApiResponse<object> { Success = false, Message = "Không tìm thấy người phụ thuộc", StatusCode = 404 });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Đã ngưng phụ cấp cho người phụ thuộc",
                StatusCode = 200
            });
        }

        // ======================================================
        // 13. PHỤ CẤP THÂN NHÂN
        // ======================================================

        /// <summary>
        /// Tính phụ cấp thân nhân cho một nhân viên
        /// </summary>
        [HttpPost("dependent-allowance/calculate")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT")]
        public async Task<IActionResult> CalculateDependentAllowance(
            [FromQuery] int employeeId,
            [FromQuery] DateTime month,
            [FromQuery] decimal amountPerDependent = 500000)
        {
            if (employeeId <= 0) return BadRequest(new ApiResponse<object> { Success = false, Message = "EmployeeId không hợp lệ", StatusCode = 400 });

            try
            {
                var result = await _service.CalculateDependentAllowanceAsync(employeeId, month, amountPerDependent);
                return Ok(new ApiResponse<DependentAllowance?>
                {
                    Success = true,
                    Message = result == null ? "Không có người phụ thuộc nào đang active" : "Tính phụ cấp thân nhân thành công",
                    Data = result,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, StatusCode = 400 });
            }
        }

        /// <summary>
        /// Tính phụ cấp thân nhân cho tất cả nhân viên
        /// </summary>
        [HttpPost("dependent-allowance/calculate-all")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,ACCOUNTANT")]
        public async Task<IActionResult> CalculateAllDependentAllowances(
            [FromQuery] DateTime month,
            [FromQuery] decimal amountPerDependent = 500000)
        {
            try
            {
                var result = await _service.CreateDependentAllowancesForAllAsync(month, amountPerDependent);
                return Ok(new ApiResponse<List<DependentAllowance>>
                {
                    Success = true,
                    Message = "Tính phụ cấp thân nhân cho tất cả nhân viên thành công",
                    Data = result,
                    StatusCode = 200,
                    Pagination = new PaginationInfo
                    {
                        TotalCount = result.Count,
                        Page = 1,
                        PageSize = result.Count,
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, StatusCode = 400 });
            }
        }

        /// <summary>
        /// Lấy phụ cấp thân nhân của nhân viên theo tháng
        /// </summary>
        [HttpGet("dependent-allowance/{employeeId}")]
        [Authorize(Roles = "SUPER_ADMIN,ADMIN,HR_MANAGER,HR_STAFF,MANAGER,ACCOUNTANT,EMPLOYEE")]
        public async Task<IActionResult> GetDependentAllowance(int employeeId, [FromQuery] DateTime month)
        {
            if (employeeId <= 0) return BadRequest(new ApiResponse<object> { Success = false, Message = "EmployeeId không hợp lệ", StatusCode = 400 });

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "HR_STAFF", "MANAGER", "ACCOUNTANT" };

            if (!allowedRoles.Contains(currentUserRole) && currentUserId != employeeId)
            {
                return Forbid();
            }

            var result = await _service.GetDependentAllowanceByMonthAsync(employeeId, month);
            return Ok(new ApiResponse<DependentAllowance?>
            {
                Success = true,
                Message = result == null ? "Không có phụ cấp thân nhân cho tháng này" : "Lấy phụ cấp thân nhân thành công",
                Data = result,
                StatusCode = 200
            });
        }
    }
}