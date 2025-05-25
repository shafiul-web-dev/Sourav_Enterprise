using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class Order
	{
		[Key]
		public int OrderID { get; set; }

		[Required]
		public int UserID { get; set; }

		[Required]
		public int UserAddressID { get; set; }

		public int? ExpenseID { get; set; } // New Expense Reference

		public DateTime OrderDate { get; set; } = DateTime.UtcNow;

		[Required]
		[MaxLength(20)]
		public string Status { get; set; }

		[Required]
		[Range(0.01, double.MaxValue)]
		public decimal TotalAmount { get; set; }

		[ForeignKey("UserID")]
		public User User { get; set; }

		[ForeignKey("UserAddressID")]
		public UserAddress UserAddress { get; set; }

		// Add this inside your Order class
		public List<OrderItem> OrderItems { get; set; } // ✅ Enables navigation

		[ForeignKey("ExpenseID")]
		public Expense Expense { get; set; } // Navigation Property
	}
}
