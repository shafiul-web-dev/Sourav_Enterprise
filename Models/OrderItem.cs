using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class OrderItem
	{
		[Key]
		public int OrderItemID { get; set; }

		[Required]
		public int OrderID { get; set; }

		[Required]
		public int ProductID { get; set; }

		[Required]
		[Range(1, int.MaxValue)]
		public int Quantity { get; set; }

		[Required]
		[Range(0.01, double.MaxValue)]
		public decimal Price { get; set; }

		[ForeignKey("OrderID")]
		public Order Order { get; set; }

		[ForeignKey("ProductID")]
		public Product Product { get; set; }
	}
}
