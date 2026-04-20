using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LotusTeam.Data;
using LotusTeam.Models;

namespace LotusTeam.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GendersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GendersController(AppDbContext context)
        {
            _context = context;
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Gender>>> GetAll()
        {
            return await _context.Genders.ToListAsync();
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<Gender>> GetById(byte id)
        {
            var gender = await _context.Genders.FindAsync(id);

            if (gender == null)
                return NotFound(new { message = "Gender not found" });

            return gender;
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<ActionResult<Gender>> Create(Gender gender)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(gender.GenderCode) ||
                string.IsNullOrWhiteSpace(gender.GenderName))
            {
                return BadRequest(new { message = "GenderCode và GenderName không được để trống" });
            }

            // Check duplicate code
            var exists = await _context.Genders
                .AnyAsync(x => x.GenderCode == gender.GenderCode);

            if (exists)
            {
                return BadRequest(new { message = "GenderCode đã tồn tại" });
            }

            _context.Genders.Add(gender);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = gender.GenderID }, gender);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(byte id, Gender gender)
        {
            if (id != gender.GenderID)
                return BadRequest(new { message = "ID không khớp" });

            var existing = await _context.Genders.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Gender không tồn tại" });

            // Update fields
            existing.GenderCode = gender.GenderCode;
            existing.GenderName = gender.GenderName;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thành công" });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(byte id)
        {
            var gender = await _context.Genders.FindAsync(id);

            if (gender == null)
                return NotFound(new { message = "Gender không tồn tại" });

            _context.Genders.Remove(gender);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa thành công" });
        }
    }
}