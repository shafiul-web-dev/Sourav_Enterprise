namespace Sourav_Enterprise.Models
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.ComponentModel.DataAnnotations.Schema;

	public class Review
	{
		[Key]
		public int ReviewID { get; set; }

		[Required]
		public int UserID { get; set; }

		[Required]
		public int ProductID { get; set; }

		[Required]
		[Range(1, 5)]
		public int Rating { get; set; }

		[MaxLength(1000)]
		public string Comment { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[ForeignKey("UserID")]
		public User User { get; set; }

		[ForeignKey("ProductID")]
		public Product Product { get; set; }
	}
}
