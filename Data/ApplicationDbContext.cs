using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Models;
using Sourav_Enterprise.Models.ViewModels;
using System;

namespace Sourav_Enterprise.Data
{
	public class ApplicationDbContext : DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

		// Define DbSets for each entity
		public DbSet<User> Users { get; set; }
		public DbSet<UserAddress> UserAddresses { get; set; }
		public DbSet<AdminUser> AdminUsers { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<Order> Orders { get; set; }
		public DbSet<OrderItem> OrderItems { get; set; }
		public DbSet<Payment> Payments { get; set; }
		public DbSet<Expense> Expenses { get; set; }
		public DbSet<Review> Reviews { get; set; }
		public DbSet<Cart> Carts { get; set; }
		public DbSet<Wishlist> Wishlists { get; set; }
		public DbSet<Shipping> Shippings { get; set; }
		public DbSet<Inventory> Inventories { get; set; }
		public DbSet<Coupon> Coupons { get; set; }

		public DbSet<BestSellingCategoryViewModel> BestSellingCategoryViewModel { get; set; }
		public DbSet<CategoryProfitabilityViewModel> CategoryProfitabilityViewModel { get; set; }
		public DbSet<CategoryDemandForecastViewModel> CategoryDemandForecastViewModel { get; set; }
		public DbSet<AverageOrderValueViewModel> AverageOrderValueViewModel { get; set; }
		public DbSet<CustomerCategoryPreferenceViewModel> CustomerCategoryPreferenceViewModel { get; set; }









		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<BestSellingCategoryViewModel>().HasNoKey();

			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<CategoryProfitabilityViewModel>().HasNoKey();

			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<CategoryDemandForecastViewModel>().HasNoKey();

			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<AverageOrderValueViewModel>().HasNoKey();

			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<CustomerCategoryPreferenceViewModel>().HasNoKey();




			// Apply Foreign Key Constraints & Delete Behavior
			modelBuilder.Entity<UserAddress>()
				.HasOne(ua => ua.User)
				.WithMany()
				.HasForeignKey(ua => ua.UserID)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Product>()
				.HasOne(p => p.Category)
				.WithMany()
				.HasForeignKey(p => p.CategoryID)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Order>()
				.HasOne(o => o.User)
				.WithMany()
				.HasForeignKey(o => o.UserID)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Order>()
				.HasOne(o => o.UserAddress)
				.WithMany()
				.HasForeignKey(o => o.UserAddressID)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<OrderItem>()
				.HasOne(oi => oi.Order)
				.WithMany()
				.HasForeignKey(oi => oi.OrderID)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<OrderItem>()
				.HasOne(oi => oi.Product)
				.WithMany()
				.HasForeignKey(oi => oi.ProductID)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Payment>()
				.HasOne(p => p.Order)
				.WithMany()
				.HasForeignKey(p => p.OrderID)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Review>()
				.HasOne(r => r.User)
				.WithMany()
				.HasForeignKey(r => r.UserID)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Review>()
				.HasOne(r => r.Product)
				.WithMany()
				.HasForeignKey(r => r.ProductID)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Expense>()
				.HasOne(e => e.AdminUser)
				.WithMany()
				.HasForeignKey(e => e.AdminID)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Expense>()
				.HasOne(e => e.Order)
				.WithMany()
				.HasForeignKey(e => e.OrderID)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<Expense>()
				.HasOne(e => e.Product)
				.WithMany()
				.HasForeignKey(e => e.ProductID)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<Inventory>()
				.HasOne(i => i.Product)
				.WithMany()
				.HasForeignKey(i => i.ProductID)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Inventory>()
				.HasOne(i => i.Expense)
				.WithMany()
				.HasForeignKey(i => i.ExpenseID)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<Shipping>()
				.HasOne(s => s.Order)
				.WithMany()
				.HasForeignKey(s => s.OrderID)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Shipping>()
				.HasOne(s => s.UserAddress)
				.WithMany()
				.HasForeignKey(s => s.UserAddressID)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Shipping>()
				.HasOne(s => s.Expense)
				.WithMany()
				.HasForeignKey(s => s.ExpenseID)
				.OnDelete(DeleteBehavior.SetNull);

			// Unique Index for Coupon Code
			modelBuilder.Entity<Coupon>()
				.HasIndex(c => c.Code)
				.IsUnique();
		}

	}
}
