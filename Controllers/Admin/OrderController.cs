using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;
using FlowerShop.Common;
using Microsoft.AspNetCore.Authorization;

namespace FlowerShop.Controllers.Admin
{
    [Route("api/admin/orders")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class OrderController : ControllerBase
    {
        private readonly FlowerContext _context;

        public OrderController(FlowerContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] OrderSearchParams f)
        {
            var paging = PagingHelper.Normalize(f.Page, f.Limit);
            var q = BuildOrderQuery(f);

            var total = await q.CountAsync();
            var totalPages = (int)Math.Ceiling((double)total / paging.Limit);
            var items = await q.OrderByDescending(o => o.OrderDate)
                .Skip((paging.Page - 1) * paging.Limit)
                .Take(paging.Limit)
                .Select(order => new AdminOrderListItemDto
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    TotalPrice = order.TotalAmount,
                    Total = order.TotalAmount,
                    Status = order.Status,
                    CustomerName = order.User != null ? order.User.FullName : null,
                    UserName = order.User != null ? order.User.FullName : null,
                    ReceiverName = order.ReceiverName,
                    ReceiverPhone = order.ReceiverPhone,
                    ReceiverAddress = order.ReceiverAddress,
                    ShippingAddress = order.ReceiverAddress,
                    Address = order.ReceiverAddress,
                    PaymentMethod = order.PaymentMethod,
                    Note = order.Note
                })
                .ToListAsync();

            return Ok(new { total, totalItems = total, totalPages, items });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound();

            return Ok(new AdminOrderDetailDto
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                TotalPrice = order.TotalAmount,
                Total = order.TotalAmount,
                Status = order.Status,
                CustomerName = order.User?.FullName,
                UserName = order.User?.FullName,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                ReceiverAddress = order.ReceiverAddress,
                ShippingAddress = order.ReceiverAddress,
                Address = order.ReceiverAddress,
                PaymentMethod = order.PaymentMethod,
                Note = order.Note,
                OrderDetails = order.OrderDetails.Select(detail => new AdminOrderItemDto
                {
                    OrderDetailId = detail.OrderDetailId,
                    ProductId = detail.ProductId,
                    ProductName = detail.Product?.ProductName,
                    ImageUrl = detail.Product?.ImageUrl,
                    Quantity = detail.Quantity,
                    Price = detail.UnitPrice,
                    UnitPrice = detail.UnitPrice,
                    Subtotal = detail.Subtotal
                }).ToList()
            });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusUpdateDto data)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            order.Status = data.Status;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, status = order.Status });
        }

        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelRequestDto data)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound();

            order.Status = "Đã hủy";
            order.Note = string.IsNullOrEmpty(order.Note)
                ? "Lý do hủy: " + data.Reason
                : order.Note + " | Lý do hủy: " + data.Reason;

            foreach (var detail in order.OrderDetails)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product != null)
                {
                    product.StockQuantity = (product.StockQuantity ?? 0) + detail.Quantity;
                    product.SoldQuantity = (product.SoldQuantity ?? 0) - detail.Quantity;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        private IQueryable<Order> BuildOrderQuery(OrderSearchParams f)
        {
            var query = _context.Orders.AsNoTracking().Include(order => order.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(f.Status))
                query = query.Where(order => order.Status == f.Status);

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var keyword = f.Search.Trim();
                query = query.Where(order =>
                    (order.ReceiverName ?? "").Contains(keyword) ||
                    (order.ReceiverPhone ?? "").Contains(keyword));
            }

            if (f.FromDate.HasValue)
                query = query.Where(order => order.OrderDate >= f.FromDate);

            if (f.ToDate.HasValue)
                query = query.Where(order => order.OrderDate <= f.ToDate);

            return FilterByPayment(query, f.PaymentMethod);
        }

        private static IQueryable<Order> FilterByPayment(IQueryable<Order> query, string? paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod)) return query;

            var payment = NormalizePaymentMethod(paymentMethod);
            return payment == "cod"
                ? query.Where(order => order.PaymentMethod != null && order.PaymentMethod.ToLower() == "cod")
                : query.Where(order => order.PaymentMethod != null && order.PaymentMethod.ToLower() != "cod");
        }

        private static string NormalizePaymentMethod(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            var normalized = value.Trim().ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");

            return normalized == "cod" ? "cod" : "payment";
        }
    }

    public class OrderSearchParams
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
        public int PageSize
        {
            get => Limit;
            set => Limit = value;
        }
        public string? Status { get; set; }
        public string? Search { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? DateFrom
        {
            get => FromDate;
            set => FromDate = value;
        }
        public DateTime? ToDate { get; set; }
        public DateTime? DateTo
        {
            get => ToDate;
            set => ToDate = value;
        }
        public string? PaymentMethod { get; set; }
    }

    public class StatusUpdateDto { public string Status { get; set; } = ""; }
    public class CancelRequestDto { public string Reason { get; set; } = ""; }

    public class AdminOrderListItemDto
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? Total { get; set; }
        public string? Status { get; set; }
        public string? CustomerName { get; set; }
        public string? UserName { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? ReceiverAddress { get; set; }
        public string? ShippingAddress { get; set; }
        public string? Address { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Note { get; set; }
    }

    public class AdminOrderDetailDto : AdminOrderListItemDto
    {
        public List<AdminOrderItemDto> OrderDetails { get; set; } = new();
    }

    public class AdminOrderItemDto
    {
        public int OrderDetailId { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? Subtotal { get; set; }
    }
}
