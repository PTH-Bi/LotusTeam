using LotusTeam.Models;
using LotusTeam.Service;
using Microsoft.AspNetCore.Mvc;

namespace LotusTeam.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyInfoController : ControllerBase
    {
        private readonly ICompanyInfoService _service;

        public CompanyInfoController(ICompanyInfoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CompanyInfo model)
        {
            return Ok(await _service.CreateAsync(model));
        }

        [HttpPut]
        public async Task<IActionResult> Update(CompanyInfo model)
        {
            var result = await _service.UpdateAsync(model);
            if (!result) return NotFound();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok();
        }
    }
}
