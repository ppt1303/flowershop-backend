using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;
using FlowerShop.Common;
using Microsoft.AspNetCore.Authorization;

namespace FlowerShop.Controllers.Admin
{
    [Route("api/admin/products")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ProductController : ControllerBase
    {
        private readonly FlowerContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductController(FlowerContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProductParams f)
        {
            var paging = PagingHelper.Normalize(f.Page, f.Limit);
            var q = _context.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Images).AsQueryable();

            q = ApplyFilters(q, f);
            q = ApplySort(q, f.SortBy);

            var total = await q.CountAsync();
            var items = await q.Skip((paging.Page - 1) * paging.Limit).Take(paging.Limit).ToListAsync();
            return Ok(new { total, items });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var p = await _context.Products.AsNoTracking().Include(x => x.Category).Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.ProductId == id);
            if (p == null) return NotFound();
            return Ok(p);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            var error = ValidateProduct(product);
            if (error != null) return BadRequest(new { message = error });

            product.ProductName = product.ProductName.Trim();
            product.CreatedDate = DateTime.Now;
            product.IsActive = product.IsActive ?? true;
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Product data)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();

            var error = ValidateProduct(data);
            if (error != null) return BadRequest(new { message = error });

            UpdateProductData(p, data);

            await _context.SaveChangesAsync();
            return Ok(p);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();

            if (await _context.OrderDetails.AnyAsync(od => od.ProductId == id))
            {
                p.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Sản phẩm có đơn hàng, đã ẩn." });
            }

            var images = await _context.ProductImages.Where(i => i.ProductId == id).ToListAsync();
            foreach (var img in images)
            {
                DeleteFile(img.ImageUrl);
            }
            DeleteFile(p.ImageUrl);

            _context.ProductImages.RemoveRange(images);
            _context.Products.Remove(p);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa thành công" });
        }

        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> Toggle(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();
            p.IsActive = !p.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { id = p.ProductId, isActive = p.IsActive });
        }

        [HttpPost("{id}/images")]
        public async Task<IActionResult> UploadImages(int id, [FromForm] List<IFormFile> files, [FromForm] int mainIndex)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();
            if (files == null || files.Count == 0) return BadRequest("Không có file");

            var folder = Path.Combine(_env.WebRootPath, "uploads/products");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var existingImages = await _context.ProductImages.Where(x => x.ProductId == id).ToListAsync();
            foreach (var existing in existingImages) existing.IsMain = false;

            for (int i = 0; i < files.Count; i++)
            {
                var imageUrl = await SaveImage(files[i], folder);

                var img = new ProductImage
                {
                    ProductId = id,
                    ImageUrl = imageUrl,
                    IsMain = (i == mainIndex)
                };
                _context.ProductImages.Add(img);

                if (i == mainIndex) p.ImageUrl = img.ImageUrl;
            }

            await _context.SaveChangesAsync();
            var images = await _context.ProductImages.Where(x => x.ProductId == id).ToListAsync();
            return Ok(images);
        }

        [HttpDelete("{id}/images/{imageId}")]
        public async Task<IActionResult> DeleteImage(int id, int imageId)
        {
            var img = await _context.ProductImages.FindAsync(imageId);
            if (img == null || img.ProductId != id) return NotFound();

            DeleteFile(img.ImageUrl);

            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa ảnh thành công" });
        }

        [HttpPatch("{id}/images/{imageId}/set-main")]
        public async Task<IActionResult> SetMainImage(int id, int imageId)
        {
            var images = await _context.ProductImages.Where(x => x.ProductId == id).ToListAsync();
            foreach (var img in images) img.IsMain = (img.Id == imageId);

            var main = images.FirstOrDefault(x => x.Id == imageId);
            if (main != null)
            {
                var p = await _context.Products.FindAsync(id);
                if (p != null) p.ImageUrl = main.ImageUrl;
            }

            await _context.SaveChangesAsync();
            return Ok(images);
        }

        private void DeleteFile(string? url)
        {
            if (string.IsNullOrEmpty(url) || url.StartsWith("http")) return;
            var path = Path.Combine(_env.WebRootPath, url.TrimStart('/'));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        private static IQueryable<Product> ApplyFilters(IQueryable<Product> q, ProductParams f)
        {
            if (!string.IsNullOrEmpty(f.Search))
                q = q.Where(p => p.ProductName.Contains(f.Search));
            if (f.CategoryId.HasValue)
                q = q.Where(p => p.CategoryId == f.CategoryId);
            if (f.IsActive.HasValue)
                q = q.Where(p => p.IsActive == f.IsActive);
            if (f.IsFeatured.HasValue)
                q = q.Where(p => p.IsFeatured == f.IsFeatured);
            if (f.MinPrice.HasValue)
                q = q.Where(p => p.Price >= f.MinPrice);
            if (f.MaxPrice.HasValue)
                q = q.Where(p => p.Price <= f.MaxPrice);

            return q;
        }

        private static IQueryable<Product> ApplySort(IQueryable<Product> q, string? sortBy)
        {
            return sortBy switch
            {
                "price_asc" => q.OrderBy(p => p.Price),
                "price_desc" => q.OrderByDescending(p => p.Price),
                "sold" => q.OrderByDescending(p => p.SoldQuantity),
                _ => q.OrderByDescending(p => p.CreatedDate)
            };
        }

        private static void UpdateProductData(Product product, Product data)
        {
            product.ProductName = data.ProductName.Trim();
            product.Description = data.Description;
            product.Price = data.Price;
            product.DiscountPrice = data.DiscountPrice;
            product.StockQuantity = data.StockQuantity;
            product.CategoryId = data.CategoryId;
            product.IsActive = data.IsActive;
            product.IsFeatured = data.IsFeatured;
            product.ImageUrl = data.ImageUrl;
            product.UpdatedDate = DateTime.Now;
        }

        private static async Task<string> SaveImage(IFormFile file, string folder)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var path = Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return "/uploads/products/" + fileName;
        }

        private static string? ValidateProduct(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.ProductName))
                return "Tên sản phẩm không được để trống";
            if (product.Price < 0)
                return "Giá sản phẩm không hợp lệ";
            if (product.DiscountPrice.HasValue && product.DiscountPrice < 0)
                return "Giá giảm không hợp lệ";
            if (product.DiscountPrice.HasValue && product.DiscountPrice > product.Price)
                return "Giá giảm không được lớn hơn giá gốc";
            if (product.StockQuantity.HasValue && product.StockQuantity < 0)
                return "Số lượng tồn kho không hợp lệ";

            return null;
        }
    }

    public class ProductParams
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsFeatured { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; }
    }
}
