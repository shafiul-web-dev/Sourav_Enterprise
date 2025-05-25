using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class Payment
	{
		[Key]
		public int PaymentID { get; set; }

		[Required]
		public int OrderID { get; set; }

		[Required]
		[Range(0.01, double.MaxValue)]
		public decimal Amount { get; set; }

		[Required]
		[MaxLength(50)]
		public string PaymentMethod { get; set; } // Validate allowed values in application logic

		public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

		[ForeignKey("OrderID")]
		public Order Order { get; set; }
	}
}
