using System;
using System.Collections.Generic;

namespace FlowerShop.Data;

public partial class Banner
{
    public int BannerId { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }             
    public string? CtaText { get; set; }              
    public string? BgColor { get; set; }              
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
}
