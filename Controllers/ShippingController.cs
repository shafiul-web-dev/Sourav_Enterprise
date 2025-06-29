using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Data;
using Sourav_Enterprise.DTO.ShippingDto;
using Sourav_Enterprise.Models;

namespace Sourav_Enterprise.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ShippingController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public ShippingController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpPost]
		public async Task<IActionResult> CreateShipping([FromBody] CreateShippingDto request)
		{
			var order = await _context.Orders.FindAsync(request.OrderID);
			if (order == null) return NotFound("Order not found.");

			var address = await _context.UserAddresses.FindAsync(request.UserAddressID);
			if (address == null) return NotFound("Shipping address not found.");

			var shipping = new Shipping
			{
				OrderID = request.OrderID,
				UserAddressID = request.UserAddressID,
				Status = request.Status,
				ShippingDate = DateTime.UtcNow
			};

			_context.Shippings.Add(shipping);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Shipping created.", shipping });
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetShippingById(int id)
		{
			var shipping = await _context.Shippings
				.Include(s => s.Order)
				.Include(s => s.UserAddress)
				.FirstOrDefaultAsync(s => s.ShippingID == id);

			if (shipping == null) return NotFound("Shipping record not found.");

			return Ok(shipping);
		}

		[HttpGet("by-order/{orderId}")]
		public async Task<IActionResult> GetByOrder(int orderId)
		{
			var shipping = await _context.Shippings
				.Include(s => s.UserAddress)
				.FirstOrDefaultAsync(s => s.OrderID == orderId);

			if (shipping == null) return NotFound("No shipping record found for this order.");

			return Ok(shipping);
		}

		[HttpPut("update-status/{id}")]
		public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
		{
			var shipping = await _context.Shippings.FindAsync(id);
			if (shipping == null) return NotFound("Shipping record not found.");

			shipping.Status = status;
			await _context.SaveChangesAsync();

			return Ok(new { message = $"Status updated to '{status}'." });
		}

		[HttpGet("all")]
		public async Task<IActionResult> GetAll()
		{
			var shipments = await _context.Shippings
				.Include(s => s.Order)
				.Include(s => s.UserAddress)
				.OrderByDescending(s => s.ShippingDate)
				.ToListAsync();

			return Ok(shipments);
		}
	}
}
