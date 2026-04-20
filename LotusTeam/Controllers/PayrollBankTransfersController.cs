// Controllers/PayrollBankTransfersController.cs
using LotusTeam.Data;
using LotusTeam.DTOs;
using LotusTeam.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ADMIN,HR")]
    public class PayrollBankTransfersController : ControllerBase
    {
        private readonly IPayrollBankTransferService _service;

        public PayrollBankTransfersController(IPayrollBankTransferService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PayrollBankTransferDto>>> GetAll()
        {
            var transfers = await _service.GetAllAsync();
            return Ok(transfers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PayrollBankTransferDetailDto>> GetById(int id)
        {
            var transfer = await _service.GetTransferDetailAsync(id);
            if (transfer == null)
                return NotFound($"Không tìm thấy transfer với ID {id}");

            return Ok(transfer);
        }

        [HttpPost]
        public async Task<ActionResult<PayrollBankTransferDto>> Create(CreatePayrollBankTransferDto createDto)
        {
            try
            {
                var transfer = await _service.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = transfer.TransferID }, transfer);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PayrollBankTransferDto>> Update(int id, UpdatePayrollBankTransferDto updateDto)
        {
            var transfer = await _service.UpdateAsync(id, updateDto);
            if (transfer == null)
                return NotFound($"Không tìm thấy transfer với ID {id}");

            return Ok(transfer);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                    return NotFound($"Không tìm thấy transfer với ID {id}");

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/process")]
        public async Task<ActionResult<PayrollBankTransferDto>> ProcessTransfer(int id)
        {
            try
            {
                var transfer = await _service.ProcessTransferAsync(id);
                if (transfer == null)
                    return NotFound($"Không tìm thấy transfer với ID {id}");

                return Ok(transfer);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("payroll/{payrollId}")]
        public async Task<ActionResult<IEnumerable<PayrollBankTransferDto>>> GetByPayrollId(int payrollId)
        {
            var transfers = await _service.GetByPayrollIdAsync(payrollId);
            return Ok(transfers);
        }

        [HttpGet("{id}/summary")]
        public async Task<ActionResult<BankTransferSummaryDto>> GetSummary(int id)
        {
            try
            {
                var summary = await _service.GetTransferSummaryAsync(id);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}