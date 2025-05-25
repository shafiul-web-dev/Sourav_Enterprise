using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class Expense
	{
		[Key]
		public int ExpenseID { get; set; }

		[Required]
		public int AdminID { get; set; }

		public int? OrderID { get; set; }

		public int? ProductID { get; set; }

		public int? ShippingID { get; set; }

		public int? InventoryID { get; set; }

		[Required]
		[MaxLength(100)]
		public string Category { get; set; } // Validate category in application logic

		[Required]
		[Range(0.01, double.MaxValue)]
		public decimal Amount { get; set; }

		public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

		[MaxLength(500)]
		public string Description { get; set; }

		[ForeignKey("AdminID")]
		public AdminUser AdminUser { get; set; }

		[ForeignKey("OrderID")]
		public Order Order { get; set; }

		[ForeignKey("ProductID")]
		public Product Product { get; set; }
	}
}
