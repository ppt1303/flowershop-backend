using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;
using FlowerShop.Common;
using Microsoft.AspNetCore.Authorization;

namespace FlowerShop.Controllers.Admin
{
    [Route("api/admin/contacts")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ContactController : ControllerBase
    {
        private readonly FlowerContext _context;

        public ContactController(FlowerContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? isRead, [FromQuery] string? search,
            [FromQuery] int page = 1, [FromQuery] int limit = 15)
        {
            (page, limit) = PagingHelper.Normalize(page, limit, defaultLimit: 15);

            var q = _context.Contacts.AsNoTracking().AsQueryable();

            if (isRead.HasValue)
                q = q.Where(c => c.IsRead == isRead);
            if (!string.IsNullOrEmpty(search))
                q = q.Where(c =>
                    (c.FullName ?? "").Contains(search)
                    || (c.Email ?? "").Contains(search)
                    || (c.Subject ?? "").Contains(search));

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * limit).Take(limit).ToListAsync();

            return Ok(new { total, items });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _context.Contacts.FindAsync(id);
            if (c == null) return NotFound();
            if (c.IsRead == false) { c.IsRead = true; await _context.SaveChangesAsync(); }
            return Ok(c);
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var c = await _context.Contacts.FindAsync(id);
            if (c == null) return NotFound();
            c.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            var c = await _context.Contacts.FindAsync(id);
            if (c == null) return NotFound();
            _context.Contacts.Remove(c);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa thành công" });
        }
    }
}
