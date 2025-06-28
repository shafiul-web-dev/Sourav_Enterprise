using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.DTO.PaymentDto
{
	public class ProcessPaymentDto
	{
		[Required]
		public int OrderID { get; set; }

		[Required]
		[Range(0.01, double.MaxValue)]
		public decimal Amount { get; set; }

		[Required]
		public string PaymentMethod { get; set; }

	}
}
