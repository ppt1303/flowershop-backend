using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;
using FlowerShop.Common;
using Microsoft.AspNetCore.Authorization;

namespace FlowerShop.Controllers.User
{
    [Route("api/reviews")]
    [ApiController]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly FlowerContext _context;

        public ReviewController(FlowerContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (userId == null)
                return Unauthorized(new { message = "Vui lòng đăng nhập để đánh giá" });

            if (request.ProductId == null || request.Rating == null)
                return BadRequest(new { message = "Thiếu thông tin sản phẩm hoặc số sao" });

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest(new { message = "Số sao phải từ 1 đến 5" });

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
                return NotFound(new { message = "Sản phẩm không tồn tại" });

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == request.ProductId && r.UserId == userId.Value);

            if (review == null)
            {
                review = new Review
                {
                    ProductId = request.ProductId,
                    UserId = userId.Value
                };
                _context.Reviews.Add(review);
            }

            review.Rating = request.Rating;
            review.Comment = request.Comment;
            review.CreatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            product.Rating = await _context.Reviews
                .Where(r => r.ProductId == request.ProductId)
                .AverageAsync(r => (decimal?)r.Rating) ?? 0;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đánh giá thành công" });
        }
    }

    public class SubmitReviewRequest
    {
        public int? ProductId { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
    }
}
