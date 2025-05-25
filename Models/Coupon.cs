using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class Coupon
	{
		[Key]
		public int CouponID { get; set; }

		[Required]
		[MaxLength(50)]
		public string Code { get; set; } // Ensure uniqueness via Fluent API or EF Core Index attribute

		[Required]
		[Range(0.01, double.MaxValue, ErrorMessage = "Discount must be greater than 0.")]
		public decimal Discount { get; set; }

		[Required]
		[DataType(DataType.Date)]
		public DateTime ExpiryDate { get; set; }
	}
}
