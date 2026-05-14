using Microsoft.EntityFrameworkCore;

namespace FlowerShop.Data;

public partial class FlowerContext : DbContext
{
    public FlowerContext() { }
    public FlowerContext(DbContextOptions<FlowerContext> options) : base(options) { }

    public DbSet<Banner> Banners { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasKey(e => e.BannerId);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Subtitle).HasMaxLength(500);
            entity.Property(e => e.CtaText).HasMaxLength(100);
            entity.Property(e => e.BgColor).HasMaxLength(20);
            entity.Property(e => e.ImageUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.LinkUrl).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Emoji).HasMaxLength(10);
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.ContactId);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Cho xu ly");
            entity.Property(e => e.ReceiverName).HasMaxLength(100);
            entity.Property(e => e.ReceiverPhone).HasMaxLength(15);
            entity.Property(e => e.ReceiverAddress).HasMaxLength(500);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.HasOne(d => d.User).WithMany(p => p.Orders).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails).HasForeignKey(d => d.OrderId);
            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails).HasForeignKey(d => d.ProductId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ImageUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Rating).HasColumnType("decimal(3,1)").HasDefaultValue(0m);
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);
            entity.Property(e => e.SoldQuantity).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsFeatured).HasDefaultValue(false);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasOne(d => d.Category).WithMany(p => p.Products).HasForeignKey(d => d.CategoryId);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ImageUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsMain).HasDefaultValue(false);
            entity.HasOne(d => d.Product).WithMany(p => p.Images).HasForeignKey(d => d.ProductId);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId);
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.HasOne(d => d.Product).WithMany(p => p.Reviews).HasForeignKey(d => d.ProductId);
            entity.HasOne(d => d.User).WithMany(p => p.Reviews).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Avatar).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("Customer");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
