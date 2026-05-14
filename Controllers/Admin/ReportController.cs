using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;
using Microsoft.AspNetCore.Authorization;

namespace FlowerShop.Controllers.Admin
{
    [Route("api/admin/reports")] 
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReportController : ControllerBase
    {
        private readonly FlowerContext _context;

        public ReportController(FlowerContext context)
        {
            _context = context;
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] int year, [FromQuery] int? month)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Hoàn thành" && o.OrderDate.HasValue && o.OrderDate.Value.Year == year);

            if (month.HasValue)
            {
                query = query.Where(o => o.OrderDate!.Value.Month == month.Value);

                var dailyDataRaw = await query
                    .GroupBy(o => o.OrderDate!.Value.Day)
                    .Select(g => new {
                        Day = g.Key,
                        Value = g.Sum(o => o.TotalAmount ?? 0)
                    })
                    .OrderBy(x => x.Day)
                    .ToListAsync();
                var dailyData = dailyDataRaw.Select(x => new {
                    label = $"Ngày {x.Day}",
                    value = x.Value
                });

                return Ok(dailyData);
            }
            else
            {
                var monthlyDataRaw = await query
                    .GroupBy(o => o.OrderDate!.Value.Month)
                    .Select(g => new {
                        Month = g.Key,
                        Value = g.Sum(o => o.TotalAmount ?? 0)
                    })
                    .OrderBy(x => x.Month)
                    .ToListAsync();
                var monthlyData = monthlyDataRaw.Select(x => new {
                    label = $"Tháng {x.Month}",
                    value = x.Value
                });

                return Ok(monthlyData);
            }
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProductsReport([FromQuery] int limit = 10)
        {
            if (limit < 1) limit = 10;
            if (limit > 100) limit = 100;

            var topProducts = await _context.Products
                .AsNoTracking()
                .OrderByDescending(p => p.SoldQuantity)
                .Take(limit)
                .Select(p => new {
                    name = p.ProductName,
                    sold = p.SoldQuantity,
                    revenue = _context.OrderDetails
                        .Where(od => od.ProductId == p.ProductId && od.Order != null && od.Order.Status == "Hoàn thành")
                        .Sum(od => od.Subtotal ?? 0)
                })
                .ToListAsync();

            return Ok(topProducts);
        }

        [HttpGet("order-stats")]
        public async Task<IActionResult> GetOrderStats()
        {
            var stats = await _context.Orders
                .AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new {
                    status = g.Key ?? "Khác",
                    count = g.Count()
                })
                .ToListAsync();

            return Ok(stats);
        }
    }
}
