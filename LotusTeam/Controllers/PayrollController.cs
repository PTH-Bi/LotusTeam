using LotusTeam.Service;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("api/payroll")]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _service;

        public PayrollController(IPayrollService service)
        {
            _service = service;
        }

        // ======================================================
        // 1. TÍNH LƯƠNG THEO KỲ
        // POST: api/payroll/calculate?payPeriod=2025-01-01
        // ======================================================
        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromQuery] DateTime payPeriod)
        {
            if (payPeriod == default)
                return BadRequest("PayPeriod không hợp lệ.");

            try
            {
                var result = await _service.CalculatePayrollAsync(payPeriod);
                return Ok(new
                {
                    Message = "Tính lương theo kỳ thành công",
                    TotalEmployee = result.Count,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // ======================================================
        // 2. TÍNH LƯƠNG HÀNG LOẠT (THEO DANH SÁCH NHÂN VIÊN)
        // POST: api/payroll/calculate-bulk?payPeriod=2025-01-01
        // Body: [1,2,3]
        // ======================================================
        [HttpPost("calculate-bulk")]
        public async Task<IActionResult> CalculateBulk(
            [FromQuery] DateTime payPeriod,
            [FromBody] List<int> employeeIds)
        {
            if (payPeriod == default)
                return BadRequest("PayPeriod không hợp lệ.");

            if (employeeIds == null || !employeeIds.Any())
                return BadRequest("Danh sách EmployeeIds rỗng.");

            try
            {
                var result = await _service.CalculatePayrollBulkAsync(payPeriod, employeeIds);
                return Ok(new
                {
                    Message = "Tính lương hàng loạt thành công",
                    TotalEmployee = result.Count,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // ======================================================
        // 3. XEM BẢNG LƯƠNG THEO NHÂN VIÊN
        // GET: api/payroll/employee/5
        // ======================================================
        [HttpGet("employee/{employeeId:int}")]
        public async Task<IActionResult> GetByEmployee(int employeeId)
        {
            if (employeeId <= 0)
                return BadRequest("EmployeeId không hợp lệ.");

            var result = await _service.GetPayrollByEmployeeAsync(employeeId);

            return Ok(new
            {
                EmployeeId = employeeId,
                TotalPayroll = result.Count,
                Data = result
            });
        }

        // ======================================================
        // 4. XEM CHI TIẾT BẢNG LƯƠNG
        // GET: api/payroll/10
        // ======================================================
        [HttpGet("{payrollId:int}")]
        public async Task<IActionResult> GetDetail(int payrollId)
        {
            if (payrollId <= 0)
                return BadRequest("PayrollId không hợp lệ.");

            var payroll = await _service.GetPayrollDetailAsync(payrollId);

            if (payroll == null)
                return NotFound("Không tìm thấy bảng lương.");

            return Ok(payroll);
        }

        // ======================================================
        // 5. LẤY DANH SÁCH TẤT CẢ BẢNG LƯƠNG (CÓ FILTER)
        // GET: api/payroll/all?month=4&year=2026&page=1&pageSize=10
        // ======================================================
        [HttpGet("all")]
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
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // ======================================================
        // 5b. XEM BẢNG LƯƠNG DẠNG FLAT (NHƯ TRONG ẢNH)
        // GET: api/payroll/flat?payPeriod=2026-04-01
        // ======================================================
        [HttpGet("flat")]
        public async Task<IActionResult> GetFlatPayroll([FromQuery] DateTime payPeriod)
        {
            if (payPeriod == default)
                return BadRequest("PayPeriod không hợp lệ.");

            try
            {
                var result = await _service.GetPayrollFlatAsync(payPeriod);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // ======================================================
        // 6. PHÊ DUYỆT BẢNG LƯƠNG THEO KỲ
        // POST: api/payroll/approve?payPeriod=2025-01-01
        // ======================================================
        [HttpPost("approve")]
        public async Task<IActionResult> Approve([FromQuery] DateTime payPeriod)
        {
            if (payPeriod == default)
                return BadRequest("PayPeriod không hợp lệ.");

            var success = await _service.ApprovePayrollAsync(payPeriod);

            if (!success)
                return NotFound("Không có bảng lương nào để phê duyệt.");

            return Ok(new
            {
                Message = "Phê duyệt bảng lương thành công",
                PayPeriod = payPeriod.ToString("yyyy-MM")
            });
        }

        // ======================================================
        // 7. TẠO SNAPSHOT THUẾ TNCN
        // POST: api/payroll/tax-snapshot/10
        // ======================================================
        [HttpPost("tax-snapshot/{payrollId:int}")]
        public async Task<IActionResult> SnapshotTax(int payrollId)
        {
            if (payrollId <= 0)
                return BadRequest("PayrollId không hợp lệ.");

            try
            {
                var snapshot = await _service.CreateTaxSnapshotAsync(payrollId);
                return Ok(new
                {
                    Message = "Snapshot thuế thành công",
                    Data = snapshot
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // ======================================================
        // 8. LỊCH SỬ TRẢ LƯƠNG NHÂN VIÊN
        // GET: api/payroll/history/5
        // ======================================================
        [HttpGet("history/{employeeId:int}")]
        public async Task<IActionResult> History(int employeeId)
        {
            if (employeeId <= 0)
                return BadRequest("EmployeeId không hợp lệ.");

            var result = await _service.GetPayrollHistoryAsync(employeeId);

            return Ok(new
            {
                EmployeeId = employeeId,
                TotalPaid = result.Count,
                Data = result
            });
        }

        // ======================================================
        // 9. QUẢN LÝ PHỤ CẤP
        // ======================================================

        // POST: api/payroll/allowance
        [HttpPost("allowance")]
        public async Task<IActionResult> AddAllowance(
            [FromQuery] int employeeId,
            [FromQuery] DateTime month,
            [FromQuery] string name,
            [FromQuery] decimal amount,
            [FromQuery] string? note = null)
        {
            if (employeeId <= 0) return BadRequest("EmployeeId không hợp lệ");
            if (string.IsNullOrEmpty(name)) return BadRequest("Tên phụ cấp không được để trống");
            if (amount <= 0) return BadRequest("Số tiền phải lớn hơn 0");

            try
            {
                var result = await _service.AddAllowanceAsync(employeeId, month, name, amount, note);
                return Ok(new
                {
                    Message = "Thêm phụ cấp thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("allowance/{employeeId}")]
        public async Task<IActionResult> GetAllowances(int employeeId, [FromQuery] DateTime month)
        {
            if (employeeId <= 0) return BadRequest("EmployeeId không hợp lệ");

            var result = await _service.GetAllowancesByMonthAsync(employeeId, month);
            return Ok(result);
        }

        // ======================================================
        // 10. QUẢN LÝ THƯỞNG
        // ======================================================

        [HttpPost("bonus")]
        public async Task<IActionResult> AddBonus(
            [FromQuery] int employeeId,
            [FromQuery] DateTime month,
            [FromQuery] string name,
            [FromQuery] decimal amount,
            [FromQuery] string? reason = null)
        {
            if (employeeId <= 0) return BadRequest("EmployeeId không hợp lệ");
            if (string.IsNullOrEmpty(name)) return BadRequest("Tên thưởng không được để trống");
            if (amount <= 0) return BadRequest("Số tiền phải lớn hơn 0");

            try
            {
                var result = await _service.AddBonusAsync(employeeId, month, name, amount, reason);
                return Ok(new
                {
                    Message = "Thêm thưởng thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("bonus/{employeeId}")]
        public async Task<IActionResult> GetBonuses(int employeeId, [FromQuery] DateTime month)
        {
            if (employeeId <= 0) return BadRequest("EmployeeId không hợp lệ");

            var result = await _service.GetBonusesByMonthAsync(employeeId, month);
            return Ok(result);
        }

        // ======================================================
        // 11. QUẢN LÝ KHẤU TRỪ
        // ======================================================

        [HttpPost("deduction")]
        public async Task<IActionResult> AddDeduction(
            [FromQuery] int employeeId,
            [FromQuery] DateTime month,
            [FromQuery] string name,
            [FromQuery] decimal amount,
            [FromQuery] string? note = null)
        {
            if (employeeId <= 0) return BadRequest("EmployeeId không hợp lệ");
            if (string.IsNullOrEmpty(name)) return BadRequest("Tên khấu trừ không được để trống");
            if (amount <= 0) return BadRequest("Số tiền phải lớn hơn 0");

            try
            {
                var result = await _service.AddDeductionAsync(employeeId, month, name, amount, note);
                return Ok(new
                {
                    Message = "Thêm khấu trừ thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("deduction/{employeeId}")]
        public async Task<IActionResult> GetDeductions(int employeeId, [FromQuery] DateTime month)
        {
            if (employeeId <= 0) return BadRequest("EmployeeId không hợp lệ");

            var result = await _service.GetDeductionsByMonthAsync(employeeId, month);
            return Ok(result);
        }

        // ======================================================
        // 12. QUẢN LÝ NGƯỜI PHỤ THUỘC
        // ======================================================

        [HttpPost("dependent")]
        public async Task<IActionResult> AddDependent(
            [FromQuery] int employeeId,
            [FromQuery] string fullName,
            [FromQuery] string relationship,
            [FromQuery] DateTime birthDate,
            [FromQuery] string? identityNumber = null)
        {
            if (employeeId <= 0) return BadRequest("EmployeeId không hợp lệ");
            if (string.IsNullOrEmpty(fullName)) return BadRequest("Tên người phụ thuộc không được để trống");
            if (string.IsNullOrEmpty(relationship)) return BadRequest("Quan hệ không được để trống");

            try
            {
                var result = await _service.AddDependentAsync(employeeId, fullName, relationship, birthDate, identityNumber);
                return Ok(new
                {
                    Message = "Thêm người phụ thuộc thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("dependent/{employeeId}")]
        public async Task<IActionResult> GetDependents(int employeeId, [FromQuery] bool onlyActive = true)
        {
            if (employeeId <= 0) return BadRequest("EmployeeId không hợp lệ");

            var result = await _service.GetDependentsByEmployeeAsync(employeeId, onlyActive);
            return Ok(result);
        }

        [HttpPut("dependent/{dependentId}/deactivate")]
        public async Task<IActionResult> DeactivateDependent(int dependentId, [FromQuery] DateTime? endDate = null)
        {
            if (dependentId <= 0) return BadRequest("DependentId không hợp lệ");

            var result = await _service.DeactivateDependentAsync(dependentId, endDate);
            if (!result) return NotFound("Không tìm thấy người phụ thuộc");

            return Ok(new { Message = "Đã ngưng phụ cấp cho người phụ thuộc" });
        }

        // ======================================================
        // 13. PHỤ CẤP THÂN NHÂN
        // ======================================================

        [HttpPost("dependent-allowance/calculate")]
        public async Task<IActionResult> CalculateDependentAllowance(
            [FromQuery] int employeeId,
            [FromQuery] DateTime month,
            [FromQuery] decimal amountPerDependent = 500000)
        {
            if (employeeId <= 0) return BadRequest("EmployeeId không hợp lệ");

            try
            {
                var result = await _service.CalculateDependentAllowanceAsync(employeeId, month, amountPerDependent);
                if (result == null)
                    return Ok(new { Message = "Không có người phụ thuộc nào đang active", Data = (object?)null });

                return Ok(new
                {
                    Message = "Tính phụ cấp thân nhân thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("dependent-allowance/calculate-all")]
        public async Task<IActionResult> CalculateAllDependentAllowances(
            [FromQuery] DateTime month,
            [FromQuery] decimal amountPerDependent = 500000)
        {
            try
            {
                var result = await _service.CreateDependentAllowancesForAllAsync(month, amountPerDependent);
                return Ok(new
                {
                    Message = "Tính phụ cấp thân nhân cho tất cả nhân viên thành công",
                    TotalEmployees = result.Count,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("dependent-allowance/{employeeId}")]
        public async Task<IActionResult> GetDependentAllowance(int employeeId, [FromQuery] DateTime month)
        {
            if (employeeId <= 0) return BadRequest("EmployeeId không hợp lệ");

            var result = await _service.GetDependentAllowanceByMonthAsync(employeeId, month);
            if (result == null)
                return Ok(new { Message = "Không có phụ cấp thân nhân cho tháng này", Data = (object?)null });

            return Ok(result);
        }
    }
}