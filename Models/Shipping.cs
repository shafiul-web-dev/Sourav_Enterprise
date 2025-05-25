using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class Shipping
	{
		[Key]
		public int ShippingID { get; set; }

		[Required]
		public int OrderID { get; set; }

		[Required]
		public int UserAddressID { get; set; }

		// Nullable foreign key to Expenses.
		public int? ExpenseID { get; set; }

		[Required]
		[MaxLength(20)]
		public string Status { get; set; } // Validate allowed values: "Pending", "Shipped", "Delivered"

		public DateTime ShippingDate { get; set; } = DateTime.UtcNow;

		[ForeignKey("OrderID")]
		public Order Order { get; set; }

		[ForeignKey("UserAddressID")]
		public UserAddress UserAddress { get; set; }

		[ForeignKey("ExpenseID")]
		public Expense Expense { get; set; }
	}
}
