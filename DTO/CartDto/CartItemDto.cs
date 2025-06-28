namespace Sourav_Enterprise.DTO.CartDto
{
	public class CartItemDto
	{	
			public int CartID { get; set; }
			public int ProductID { get; set; }
			public string ProductName { get; set; }
			public decimal Price { get; set; }
			public int Quantity { get; set; }
			public decimal Subtotal => Price * Quantity;

	}
}
