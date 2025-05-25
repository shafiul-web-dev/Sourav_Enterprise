using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class Product
	{
		[Key]
		public int ProductID { get; set; }

		[Required]
		[MaxLength(255)]
		public string Name { get; set; }

		[MaxLength(1000)]
		public string Description { get; set; }

		[Required]
		[Range(0.01, double.MaxValue)]
		public decimal Price { get; set; }

		[Required]
		[Range(0, int.MaxValue)]
		public int Stock { get; set; }

		public int? CategoryID { get; set; }

		public int? ExpenseID { get; set; } // New Expense Reference

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public List<OrderItem> OrderItems { get; set; } // ✅ Enables product-to-order lookup

		[ForeignKey("CategoryID")]
		public Category Category { get; set; }

		[ForeignKey("ExpenseID")]
		public Expense Expense { get; set; } // Navigation Property
	}
}
