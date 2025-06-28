using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Data;
using Sourav_Enterprise.DTO.CartDto;
using Sourav_Enterprise.Models;

namespace Sourav_Enterprise.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CartController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		public CartController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpPost("add")]
		public async Task<IActionResult> AddToCart([FromBody] CartRequestModel model)
		{
			var existingItem = await _context.Carts
				.FirstOrDefaultAsync(c => c.UserID == model.UserID && c.ProductID == model.ProductID);

			if (existingItem != null)
			{
				existingItem.Quantity += model.Quantity;
				existingItem.AddedAt = DateTime.UtcNow;
			}
			else
			{
				var cartItem = new Cart
				{
					UserID = model.UserID,
					ProductID = model.ProductID,
					Quantity = model.Quantity,
					AddedAt = DateTime.UtcNow
				};

				_context.Carts.Add(cartItem);
			}

			await _context.SaveChangesAsync();
			return Ok(new { message = "Item added to cart successfully." });
		}

		[HttpGet("user/{userId}")]
		public async Task<IActionResult> GetCartByUser(int userId)
		{
			var cartItems = await _context.Carts
				.Where(c => c.UserID == userId)
				.Include(c => c.Product)
				.Select(c => new CartItemDto
				{
					CartID = c.CartID,
					ProductID = c.ProductID,
					ProductName = c.Product.Name,
					Price = c.Product.Price,
					Quantity = c.Quantity
				})
				.ToListAsync();

			return Ok(cartItems);
		}

		[HttpPut("update/{cartId}")]
		public async Task<IActionResult> UpdateQuantity(int cartId, [FromBody] CartUpdateRequest request)
		{
			if (request.Quantity < 1)
				return BadRequest("Quantity must be at least 1.");

			var cartItem = await _context.Carts.FindAsync(cartId);

			if (cartItem == null)
				return NotFound("Cart item not found.");

			cartItem.Quantity = request.Quantity;
			cartItem.AddedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			return Ok(new { message = "Cart item updated successfully." });
		}

		[HttpDelete("remove/{cartId}")]
		public async Task<IActionResult> RemoveFromCart(int cartId)
		{
			var cartItem = await _context.Carts.FindAsync(cartId);

			if (cartItem == null)
				return NotFound("Cart item not found.");

			_context.Carts.Remove(cartItem);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Item removed from cart successfully." });
		}

		[HttpDelete("empty/{userId}")]
		public async Task<IActionResult> EmptyCart(int userId)
		{
			var cartItems = await _context.Carts
				.Where(c => c.UserID == userId)
				.ToListAsync();

			if (!cartItems.Any())
				return NotFound("Cart is already empty.");

			_context.Carts.RemoveRange(cartItems);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Cart emptied successfully." });
		}

		[HttpGet("user/{userId}/count")]
		public async Task<IActionResult> GetCartCount(int userId)
		{
			var count = await _context.Carts
				.Where(c => c.UserID == userId)
				.SumAsync(c => c.Quantity);

			return Ok(new { count });
		}

		[HttpGet("all")]
		public async Task<IActionResult> GetAllCarts()
		{
			var allCarts = await _context.Carts
				.Include(c => c.Product)
				.Include(c => c.User)
				.ToListAsync();

			return Ok(allCarts);
		}
	}


}
