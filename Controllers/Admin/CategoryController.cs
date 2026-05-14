using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;
using FlowerShop.Common;
using Microsoft.AspNetCore.Authorization;

namespace FlowerShop.Controllers.Admin
{
    [Route("api/admin/categories")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class CategoryController : ControllerBase
    {
        private readonly FlowerContext _context;
        private readonly IWebHostEnvironment _env;

        public CategoryController(FlowerContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] bool? isActive,
            [FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            (page, limit) = PagingHelper.Normalize(page, limit, defaultLimit: 20);

            var q = _context.Categories.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(search))
                q = q.Where(c => c.CategoryName.Contains(search));
            if (isActive.HasValue)
                q = q.Where(c => c.IsActive == isActive);

            var total = await q.CountAsync();
            var items = await q.OrderBy(c => c.SortOrder)
                .Skip((page - 1) * limit).Take(limit).ToListAsync();

            return Ok(new { total, items });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.CategoryId == id);
            if (c == null) return NotFound();
            return Ok(c);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Category category)
        {
            var error = ValidateCategory(category);
            if (error != null) return BadRequest(new { message = error });

            category.CategoryName = category.CategoryName.Trim();
            category.CreatedDate = DateTime.Now;
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Category data)
        {
            var c = await _context.Categories.FindAsync(id);
            if (c == null) return NotFound();

            var error = ValidateCategory(data);
            if (error != null) return BadRequest(new { message = error });

            c.CategoryName = data.CategoryName.Trim();
            c.Description = data.Description;
            c.SortOrder = data.SortOrder;
            c.IsActive = data.IsActive;

            await _context.SaveChangesAsync();
            return Ok(c);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            var c = await _context.Categories.FindAsync(id);
            if (c == null) return NotFound();
            c.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã ẩn danh mục" });
        }

        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> Toggle(int id)
        {
            var c = await _context.Categories.FindAsync(id);
            if (c == null) return NotFound();
            c.IsActive = !c.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { id = c.CategoryId, isActive = c.IsActive });
        }

        [HttpPost("{id}/image")]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            if (file == null) return BadRequest();
            var c = await _context.Categories.FindAsync(id);
            if (c == null) return NotFound();

            var folder = Path.Combine(_env.WebRootPath, "uploads/categories");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
            await file.CopyToAsync(stream);

            c.ImageUrl = "/uploads/categories/" + fileName;
            await _context.SaveChangesAsync();
            return Ok(new { imageUrl = c.ImageUrl });
        }

        private static string? ValidateCategory(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.CategoryName))
                return "Tên danh mục không được để trống";

            return null;
        }
    }
}
