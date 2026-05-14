namespace FlowerShop.Data;

public partial class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string? ImageUrl { get; set; }
    public int? CategoryId { get; set; }
    public int? StockQuantity { get; set; }
    public int? SoldQuantity { get; set; }
    public decimal? Rating { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsFeatured { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public virtual Category? Category { get; set; }
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
