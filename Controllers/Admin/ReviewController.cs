using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;
using FlowerShop.Common;
using Microsoft.AspNetCore.Authorization;

namespace FlowerShop.Controllers.Admin
{
    [Route("api/admin/reviews")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReviewController : ControllerBase
    {
        private readonly FlowerContext _context;

        public ReviewController(FlowerContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ReviewSearchParams f)
        {
            var paging = PagingHelper.Normalize(f.Page, f.Limit);
            var q = _context.Reviews.AsNoTracking().Include(r => r.Product).Include(r => r.User).AsQueryable();

            if (f.ProductId.HasValue)
                q = q.Where(r => r.ProductId == f.ProductId);
            if (f.UserId.HasValue)
                q = q.Where(r => r.UserId == f.UserId);
            if (f.MinRating.HasValue)
                q = q.Where(r => r.Rating >= f.MinRating);
            if (f.MaxRating.HasValue)
                q = q.Where(r => r.Rating <= f.MaxRating);
            if (!string.IsNullOrEmpty(f.Search))
                q = q.Where(r =>
                    (r.Product != null && r.Product.ProductName.Contains(f.Search))
                    || (r.User != null && r.User.FullName.Contains(f.Search)));

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(r => r.CreatedDate)
                .Skip((paging.Page - 1) * paging.Limit).Take(paging.Limit)
                .Select(r => new {
                    r.ReviewId, r.ProductId, r.UserId, r.Rating, r.Comment, r.CreatedDate,
                    productName = r.Product != null ? r.Product.ProductName : "",
                    userName = r.User != null ? r.User.FullName : ""
                }).ToListAsync();

            return Ok(new { total, items });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            var r = await _context.Reviews.FindAsync(id);
            if (r == null) return NotFound();
            _context.Reviews.Remove(r);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa thành công" });
        }
    }

    public class ReviewSearchParams
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
        public int? ProductId { get; set; }
        public int? UserId { get; set; }
        public int? MinRating { get; set; }
        public int? MaxRating { get; set; }
        public string? Search { get; set; }
    }
}
