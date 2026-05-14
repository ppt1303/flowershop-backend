using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;
using Microsoft.AspNetCore.Authorization;

namespace FlowerShop.Controllers.Admin
{
    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize(Roles = "Admin")] 
    public class DashboardController : ControllerBase
    {
        private readonly FlowerContext _context;

        public DashboardController(FlowerContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var totalOrders = await _context.Orders.CountAsync();
            var todayOrders = await _context.Orders.CountAsync(o => o.OrderDate >= today);
            var totalProducts = await _context.Products.CountAsync();
            var totalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Hoàn thành")
                .SumAsync(o => o.TotalAmount ?? 0);

            var todayRevenue = await _context.Orders
                .Where(o => o.Status == "Hoàn thành" && o.OrderDate >= today)
                .SumAsync(o => o.TotalAmount ?? 0);

            var monthRevenue = await _context.Orders
                .Where(o => o.Status == "Hoàn thành" && o.OrderDate >= startOfMonth)
                .SumAsync(o => o.TotalAmount ?? 0);

            return Ok(new
            {
                totalOrders,
                todayOrders,
                monthRevenue,
                todayRevenue,
                totalProducts,
                totalCustomers,
                totalRevenue
            });
        }

        [HttpGet("recent-orders")]
        public async Task<IActionResult> GetRecentOrders()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new {
                    o.OrderId,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    CustomerName = o.ReceiverName ?? (o.User != null ? o.User.FullName : "")
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChart()
        {
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Today.AddMonths(-i))
                .Select(d => new { d.Year, d.Month })
                .Reverse();

            var data = new List<object>();

            foreach (var m in last6Months)
            {
                var revenue = await _context.Orders
                    .Where(o => o.Status == "Hoàn thành" &&
                                o.OrderDate.HasValue &&
                                o.OrderDate.Value.Year == m.Year &&
                                o.OrderDate.Value.Month == m.Month)
                    .SumAsync(o => o.TotalAmount ?? 0);

                data.Add(new { month = $"{m.Month}/{m.Year}", revenue });
            }

            return Ok(data);
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts()
        {
            var topProducts = await _context.Products
                .AsNoTracking()
                .OrderByDescending(p => p.SoldQuantity) 
                .Take(10)
                .Select(p => new {
                    p.ProductId,
                    p.ProductName,
                    p.Price,
                    p.SoldQuantity,
                    p.ImageUrl
                })
                .ToListAsync();

            return Ok(topProducts);
        }
    }
}
