using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;
using FlowerShop.Common;
using Microsoft.AspNetCore.Authorization;

namespace FlowerShop.Controllers.Admin
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly FlowerContext _context;

        public UserController(FlowerContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] UserSearchParams f)
        {
            var paging = PagingHelper.Normalize(f.Page, f.Limit);
            var q = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(f.Search))
                q = q.Where(u =>
                    u.FullName.Contains(f.Search)
                    || u.Email.Contains(f.Search)
                    || (u.Phone ?? "").Contains(f.Search));
            if (!string.IsNullOrEmpty(f.Role))
                q = q.Where(u => u.Role == f.Role);
            if (f.IsActive.HasValue)
                q = q.Where(u => u.IsActive == f.IsActive);

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(u => u.CreatedDate)
                .Skip((paging.Page - 1) * paging.Limit).Take(paging.Limit).ToListAsync();

            return Ok(new { total, items });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var u = await _context.Users.AsNoTracking().Include(x => x.Orders).FirstOrDefaultAsync(x => x.UserId == id);
            if (u == null) return NotFound();
            u.PasswordHash = "";
            return Ok(u);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FlowerShop.Data.User data)
        {
            var u = await _context.Users.FindAsync(id);
            if (u == null) return NotFound();
            u.FullName = data.FullName;
            u.Phone = data.Phone;
            u.Address = data.Address;
            u.Role = data.Role;
            u.IsActive = data.IsActive;
            await _context.SaveChangesAsync();
            return Ok(u);
        }

        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> Toggle(int id)
        {
            var u = await _context.Users.FindAsync(id);
            if (u == null) return NotFound();
            u.IsActive = !u.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { id = u.UserId, isActive = u.IsActive });
        }
    }

    public class UserSearchParams
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
        public string? Search { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
    }
}
