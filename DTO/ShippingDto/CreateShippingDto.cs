using System.ComponentModel.DataAnnotations;

namespace Sourav_Enterprise.DTO.ShippingDto
{
	public class CreateShippingDto
	{
		[Required]
		public int OrderID { get; set; }

		[Required]
		public int UserAddressID { get; set; }

		[MaxLength(20)]
		public string Status { get; set; } = "Pending";

	}
}
