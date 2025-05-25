using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.Models
{
	public class Category
	{
		[Key]
		public int CategoryID { get; set; }

		[Required]
		[MaxLength(100)]
		public string Name { get; set; }

		[MaxLength(500)]
		public string Description { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
