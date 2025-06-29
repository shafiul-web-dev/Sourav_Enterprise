using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.DTO.ReviewDto
{
	public class ReviewDto
	{
		[Required]
		public int UserID { get; set; }

		[Required]
		public int ProductID { get; set; }

		[Required]
		[Range(1, 5)]
		public int Rating { get; set; }

		[MaxLength(1000)]
		public string Comment { get; set; }
	}
}
