using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class Wishlist
	{
		[Key]
		public int WishlistID { get; set; }

		[Required]
		public int UserID { get; set; }

		[Required]
		public int ProductID { get; set; }

		public DateTime AddedAt { get; set; } = DateTime.UtcNow;

		[ForeignKey("UserID")]
		public User User { get; set; }

		[ForeignKey("ProductID")]
		public Product Product { get; set; }
	}
}
