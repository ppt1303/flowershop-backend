using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;

namespace FlowerShop.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly FlowerContext _context;
        public CategoriesController(FlowerContext context) { _context = context; }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.SortOrder)
                .Select(c => new
                {
                    id = c.CategoryId,
                    name = c.CategoryName,
                    imageUrl = c.ImageUrl,
                    emoji = c.Emoji,            
                    color = c.Color             
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound(new { message = "Không tìm thấy danh mục" });
            return Ok(category);
        }
    }
}
