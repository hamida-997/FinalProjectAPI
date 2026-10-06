using FinalProjectAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(c => c.Description)
                    .HasMaxLength(500);

                entity.Property(c => c.IsActive)
                    .HasDefaultValue(true);

                entity.Property(c => c.CreatedAt)
                    .IsRequired();

                entity.HasIndex(c => c.Name)
                    .IsUnique();
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.Description)
                    .HasMaxLength(1000);

                entity.Property(p => p.Price)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(p => p.StockQuantity)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(p => p.IsAvailable)
                    .HasDefaultValue(true);

                entity.Property(p => p.CreatedAt)
                    .IsRequired();

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => p.CategoryId);
                entity.HasIndex(p => p.Name);
            });

            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.HasKey(pi => pi.Id);

                entity.Property(pi => pi.ImageUrl)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(pi => pi.IsPrimary)
                    .HasDefaultValue(false);

                entity.Property(pi => pi.CreatedAt)
                    .IsRequired();

                entity.HasOne(pi => pi.Product)
                    .WithMany(p => p.ProductImages)
                    .HasForeignKey(pi => pi.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(pi => pi.ProductId);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.Property(o => o.CustomerName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(o => o.CustomerEmail)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(o => o.CustomerPhone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(o => o.ShippingAddress)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(o => o.TotalAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(o => o.Status)
                    .IsRequired()
                    .HasConversion<int>();

                entity.Property(o => o.OrderDate)
                    .IsRequired();

                entity.Property(o => o.Notes)
                    .HasMaxLength(1000);

                entity.HasIndex(o => o.OrderDate);
                entity.HasIndex(o => o.CustomerEmail);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);

                entity.Property(oi => oi.ProductName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(oi => oi.Quantity)
                    .IsRequired();

                entity.Property(oi => oi.UnitPrice)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(oi => oi.TotalPrice)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                    .WithMany()
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(oi => oi.OrderId);
                entity.HasIndex(oi => oi.ProductId);
            });
        }
    }
}
