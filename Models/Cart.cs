using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class Cart
	{
		[Key]
		public int CartID { get; set; }

		[Required]
		public int UserID { get; set; }

		[Required]
		public int ProductID { get; set; }

		[Required]
		[Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
		public int Quantity { get; set; }

		public DateTime AddedAt { get; set; } = DateTime.UtcNow;

		[ForeignKey("UserID")]
		public User User { get; set; }

		[ForeignKey("ProductID")]
		public Product Product { get; set; }
	}
}
