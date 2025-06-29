using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.DTO.ExpenseDto
{
	public class ExpenseDto
	{
		[Required]
		public int AdminID { get; set; }

		public int? OrderID { get; set; }
		public int? ProductID { get; set; }
		public int? ShippingID { get; set; }
		public int? InventoryID { get; set; }

		[Required]
		[MaxLength(100)]
		public string Category { get; set; }

		[Required]
		[Range(0.01, double.MaxValue)]
		public decimal Amount { get; set; }

		[MaxLength(500)]
		public string Description { get; set; }

		public DateTime? ExpenseDate { get; set; }
	}
}
