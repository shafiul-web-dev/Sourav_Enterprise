using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.DTO.InventoryDto
{
	public class InventoryDto
	{
		[Required]
		public int ProductID { get; set; }

		[Required]
		[Range(0, int.MaxValue)]
		public int QuantityInStock { get; set; }

		public int? ExpenseID { get; set; }
	}
}
