using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerShop.Data;

namespace FlowerShop.Controllers.User
{
    [Route("api/banners")]
    [ApiController]
    public class BannerController : ControllerBase
    {
        private readonly FlowerContext _context;
        public BannerController(FlowerContext context) 
        { 
            _context = context; 
        }

        [HttpGet]
        public async Task<IActionResult> GetBanners()
        {
            var banners = await _context.Banners
                .AsNoTracking()
                .Where(b => b.IsActive == true)
                .OrderBy(b => b.SortOrder)
                .Select(b => new
                {
                    id = b.BannerId,
                    title = b.Title,
                    sub = b.Subtitle,        
                    cta = b.CtaText,          
                    bg = b.BgColor,            
                    imageUrl = b.ImageUrl,
                    linkUrl = b.LinkUrl
                })
                .ToListAsync();

            return Ok(banners);
        }
    }
}
