using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;

namespace FlowerShop.Controllers.User
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly FlowerContext _context;

        public ProductController(FlowerContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] string? priceRange = null,
            [FromQuery] string? rating = null,
            [FromQuery] string? sort = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = BuildProductQuery(category, search, priceRange, rating, sort);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(product => new ProductListItemDto
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Sale = product.DiscountPrice,
                    ImageUrl = product.Images
                        .OrderByDescending(image => image.IsMain)
                        .ThenBy(image => image.Id)
                        .Select(image => image.ImageUrl)
                        .FirstOrDefault() ?? product.ImageUrl,
                    CategoryId = product.CategoryId,
                    Rating = product.Reviews.Any(review => review.Rating.HasValue)
                        ? Math.Round(product.Reviews
                            .Where(review => review.Rating.HasValue)
                            .Average(review => (double)review.Rating!.Value), 1)
                        : 0,
                    ReviewCount = product.Reviews.Count,
                    SoldQuantity = product.SoldQuantity,
                    StockQuantity = product.StockQuantity,
                    IsFeatured = product.IsFeatured,
                    CreatedDate = product.CreatedDate,
                    IsNew = IsNew(product)
                })
                .ToListAsync();

            return Ok(new { totalItems, totalPages, items });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductDetail(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm" });

            return Ok(ToProductDetail(product));
        }

        private IQueryable<Product> BuildProductQuery(string? category, string? search, string? priceRange, string? rating, string? sort)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(product => product.Images)
                .Include(product => product.Reviews)
                .Where(product => product.IsActive == true);

            query = FilterByCategory(query, category);
            query = FilterBySearch(query, search);
            query = FilterByPrice(query, priceRange);
            query = FilterByRating(query, rating);

            return SortProducts(query, sort);
        }

        private static IQueryable<Product> FilterByCategory(IQueryable<Product> query, string? category)
        {
            if (!int.TryParse(category, out var categoryId)) return query;
            return query.Where(product => product.CategoryId == categoryId);
        }

        private static IQueryable<Product> FilterBySearch(IQueryable<Product> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search)) return query;

            var keyword = search.Trim().ToLower();
            return query.Where(product => product.ProductName.ToLower().Contains(keyword));
        }

        private static IQueryable<Product> FilterByPrice(IQueryable<Product> query, string? priceRange)
        {
            if (string.IsNullOrWhiteSpace(priceRange)) return query;

            var parts = priceRange.Split('-');
            if (parts.Length != 2) return query;
            if (!decimal.TryParse(parts[0], out var minPrice)) return query;
            if (!decimal.TryParse(parts[1], out var maxPrice)) return query;

            return query.Where(product =>
                (product.DiscountPrice ?? product.Price) >= minPrice &&
                (product.DiscountPrice ?? product.Price) <= maxPrice);
        }

        private static IQueryable<Product> FilterByRating(IQueryable<Product> query, string? rating)
        {
            if (!int.TryParse(rating, out var minRating)) return query;

            return query.Where(product =>
                product.Reviews.Any(review => review.Rating.HasValue) &&
                product.Reviews
                    .Where(review => review.Rating.HasValue)
                    .Average(review => (double)review.Rating!.Value) >= minRating);
        }

        private static IQueryable<Product> SortProducts(IQueryable<Product> query, string? sort)
        {
            return sort switch
            {
                "price_asc" => query.OrderBy(product => product.DiscountPrice ?? product.Price),
                "price_desc" => query.OrderByDescending(product => product.DiscountPrice ?? product.Price),
                "sold" => query.OrderByDescending(product => product.SoldQuantity),
                _ => query.OrderByDescending(product => product.CreatedDate)
            };
        }

        private static ProductDetailDto ToProductDetail(Product product)
        {
            return new ProductDetailDto
            {
                ProductId = product.ProductId,
                Id = product.ProductId,
                Name = product.ProductName,
                Desc = product.Description,
                Price = product.Price,
                Sale = product.DiscountPrice,
                ImageUrl = GetMainImage(product),
                Images = GetImages(product),
                Cat = product.CategoryId,
                CategoryName = product.Category?.CategoryName,
                Rating = product.Rating ?? 0,
                SoldQuantity = product.SoldQuantity,
                StockQuantity = product.StockQuantity,
                IsFeatured = product.IsFeatured,
                IsNew = IsNew(product),
                Reviews = product.Reviews.Select(review => new ProductReviewDto
                {
                    Id = review.ReviewId,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedDate,
                    UserName = review.User?.FullName ?? "Ẩn danh"
                }).OrderByDescending(review => review.CreatedAt).ToList()
            };
        }

        private static bool IsNew(Product product)
        {
            return product.CreatedDate.HasValue && product.CreatedDate.Value.AddDays(7) >= DateTime.Now;
        }

        private static string? GetMainImage(Product product)
        {
            return product.Images
                .OrderByDescending(i => i.IsMain)
                .ThenBy(i => i.Id)
                .Select(i => i.ImageUrl)
                .FirstOrDefault() ?? product.ImageUrl;
        }

        private static List<ProductImageDto> GetImages(Product product)
        {
            return product.Images
                .OrderByDescending(i => i.IsMain)
                .ThenBy(i => i.Id)
                .Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsMain = i.IsMain
                })
                .ToList();
        }

    }

    public class ProductListItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public decimal? Sale { get; set; }
        public string? ImageUrl { get; set; }
        public int? CategoryId { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public int? SoldQuantity { get; set; }
        public int? StockQuantity { get; set; }
        public bool? IsFeatured { get; set; }
        public DateTime? CreatedDate { get; set; }
        public bool IsNew { get; set; }
    }

    public class ProductDetailDto
    {
        public int ProductId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Desc { get; set; }
        public decimal Price { get; set; }
        public decimal? Sale { get; set; }
        public string? ImageUrl { get; set; }
        public List<ProductImageDto> Images { get; set; } = new();
        public int? Cat { get; set; }
        public string? CategoryName { get; set; }
        public decimal Rating { get; set; }
        public int? SoldQuantity { get; set; }
        public int? StockQuantity { get; set; }
        public bool? IsFeatured { get; set; }
        public bool IsNew { get; set; }
        public List<ProductReviewDto> Reviews { get; set; } = new();
    }

    public class ProductReviewDto
    {
        public int Id { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string UserName { get; set; } = "";
    }

    public class ProductImageDto
    {
        public int Id { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsMain { get; set; }
    }
}
