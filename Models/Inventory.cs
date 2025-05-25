using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class Inventory
	{
		[Key]
		public int InventoryID { get; set; }

		[Required]
		public int ProductID { get; set; }

		[Required]
		[Range(0, int.MaxValue, ErrorMessage = "QuantityInStock must be greater than or equal to 0.")]
		public int QuantityInStock { get; set; }

		public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

		public int? ExpenseID { get; set; }

		[ForeignKey("ProductID")]
		public Product Product { get; set; }

		[ForeignKey("ExpenseID")]
		public Expense Expense { get; set; }
	}
}
