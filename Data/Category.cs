using System;
using System.Collections.Generic;

namespace FlowerShop.Data;

public partial class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Emoji { get; set; }                
    public string? Color { get; set; }               
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
