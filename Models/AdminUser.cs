using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class AdminUser
	{
		[Key]
		public int AdminID { get; set; }

		[Required]
		[MaxLength(255)]
		public string Name { get; set; }

		[Required]
		[EmailAddress]
		[MaxLength(255)]
		public string Email { get; set; }

		[Required]
		[MaxLength(50)]
		public string Role { get; set; } // Enum or validation in the application layer

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[Range(0, double.MaxValue)]
		public decimal TotalExpenses { get; set; } = 0;
	}
}
