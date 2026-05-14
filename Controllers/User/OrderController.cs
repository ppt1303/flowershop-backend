using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;
using FlowerShop.Common;
using Microsoft.AspNetCore.Authorization;

namespace FlowerShop.Controllers.Api
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderDto
    {
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? ReceiverAddress { get; set; }
        public string? Note { get; set; }
        public string? PaymentMethod { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    [Route("api/orders")] 
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly FlowerContext _context;

        public OrderController(FlowerContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (userId == null) return Unauthorized();

            var error = ValidateOrder(dto);
            if (error != null) return BadRequest(new { message = error });

            var productIds = dto.Items.Select(x => x.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId) && p.IsActive == true)
                .ToListAsync();

            if (products.Count != productIds.Count)
                return BadRequest(new { message = "Sản phẩm không tồn tại hoặc đã ngừng bán" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orderResult = CreateOrderDetails(dto.Items, products);
                if (orderResult.Error != null) return BadRequest(new { message = orderResult.Error });

                var order = CreateOrder(dto, userId.Value, orderResult.TotalAmount);

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var detail in orderResult.Details)
                    detail.OrderId = order.OrderId;

                _context.OrderDetails.AddRange(orderResult.Details);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi khi tạo đơn hàng", detail = ex.Message });
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (userId == null) return Unauthorized();

            var myOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .Select(order => new MyOrderDto
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    CustomerName = order.User != null ? order.User.FullName : null,
                    ReceiverName = order.ReceiverName,
                    ReceiverPhone = order.ReceiverPhone,
                    ReceiverAddress = order.ReceiverAddress,
                    PaymentMethod = order.PaymentMethod,
                    Note = order.Note,
                    ItemsCount = order.OrderDetails.Count,
                    Items = order.OrderDetails.Select(detail => new MyOrderItemDto
                    {
                        OrderDetailId = detail.OrderDetailId,
                        ProductId = detail.ProductId,
                        ProductName = detail.Product != null ? detail.Product.ProductName : "",
                        ImageUrl = detail.Product != null ? detail.Product.ImageUrl : null,
                        Quantity = detail.Quantity,
                        Price = detail.UnitPrice,
                        Subtotal = detail.Subtotal
                    }).ToList()
                })
                .ToListAsync();

            return Ok(myOrders);
        }

        private static string? ValidateOrder(CreateOrderDto dto)
        {
            if (dto.Items.Count == 0)
                return "Đơn hàng phải có sản phẩm";
            if (dto.Items.Any(x => x.Quantity <= 0))
                return "Số lượng sản phẩm không hợp lệ";

            return null;
        }

        private static Order CreateOrder(CreateOrderDto dto, int userId, decimal totalAmount)
        {
            return new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                ReceiverName = dto.ReceiverName?.Trim(),
                ReceiverPhone = dto.ReceiverPhone?.Trim(),
                ReceiverAddress = dto.ReceiverAddress?.Trim(),
                Note = dto.Note?.Trim(),
                PaymentMethod = dto.PaymentMethod?.Trim(),
                Status = "Chờ xử lý",
                TotalAmount = totalAmount
            };
        }

        private static OrderDetailsResult CreateOrderDetails(List<OrderItemDto> items, List<Product> products)
        {
            var result = new OrderDetailsResult();

            foreach (var item in items)
            {
                var product = products.First(p => p.ProductId == item.ProductId);
                var unitPrice = product.DiscountPrice ?? product.Price;

                if (product.StockQuantity.HasValue && product.StockQuantity < item.Quantity)
                {
                    result.Error = $"Sản phẩm {product.ProductName} không đủ hàng";
                    return result;
                }

                result.Details.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    Subtotal = unitPrice * item.Quantity
                });

                result.TotalAmount += unitPrice * item.Quantity;
                product.StockQuantity = product.StockQuantity.HasValue ? product.StockQuantity - item.Quantity : null;
                product.SoldQuantity = (product.SoldQuantity ?? 0) + item.Quantity;
            }

            return result;
        }
    }

    public class OrderDetailsResult
    {
        public string? Error { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderDetail> Details { get; set; } = new();
    }

    public class MyOrderDto
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Status { get; set; }
        public string? CustomerName { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? ReceiverAddress { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Note { get; set; }
        public int ItemsCount { get; set; }
        public List<MyOrderItemDto> Items { get; set; } = new();
    }

    public class MyOrderItemDto
    {
        public int OrderDetailId { get; set; }
        public int? ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal? Subtotal { get; set; }
    }
}
