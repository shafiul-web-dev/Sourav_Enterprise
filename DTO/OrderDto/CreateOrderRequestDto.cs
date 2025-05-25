namespace Sourav_Enterprise.DTO.OrderDto
{
	namespace Sourav_Enterprise.DTO
	{
		public class CreateOrderRequestDto
		{
			public int UserID { get; set; } // ✅ User placing the order
			public int AddressID { get; set; } // ✅ Shipping address
			public List<OrderItemDto> OrderItems { get; set; } // ✅ Products & Quantity
		}
	}
}
