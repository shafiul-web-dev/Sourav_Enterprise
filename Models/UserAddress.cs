using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class UserAddress
	{
		[Key]
		public int AddressID { get; set; }

		[Required]
		public int UserID { get; set; }

		[Required]
		[MaxLength(255)]
		public string Address { get; set; }

		[Required]
		[MaxLength(100)]
		public string City { get; set; }

		[Required]
		[MaxLength(20)]
		public string PostalCode { get; set; }

		[Required]
		[MaxLength(100)]
		public string Country { get; set; }

		[Required]
		[MaxLength(50)]
		public string AddressType { get; set; } // Validation can be enforced in the application layer

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[ForeignKey("UserID")]
		public User User { get; set; }
	}
}
