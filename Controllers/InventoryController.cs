using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Data;
using Sourav_Enterprise.DTO.InventoryDto;
using Sourav_Enterprise.Models;

namespace Sourav_Enterprise.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class InventoryController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		public InventoryController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpPost]
		public async Task<IActionResult> Add([FromBody] InventoryDto request)
		{
			var productExists = await _context.Products.AnyAsync(p => p.ProductID == request.ProductID);
			if (!productExists) return NotFound("Product not found.");

			var alreadyExists = await _context.Inventories.AnyAsync(i => i.ProductID == request.ProductID);
			if (alreadyExists) return Conflict("Inventory already initialized for this product.");

			var inventory = new Inventory
			{
				ProductID = request.ProductID,
				QuantityInStock = request.QuantityInStock,
				ExpenseID = request.ExpenseID,
				LastUpdated = DateTime.UtcNow
			};

			_context.Inventories.Add(inventory);
			await _context.SaveChangesAsync();
			return Ok(new { message = "Inventory created.", inventory });
		}

		[HttpPut("update/{productId}")]
		public async Task<IActionResult> UpdateStock(int productId, [FromBody] int newQuantity)
		{
			var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductID == productId);
			if (inventory == null) return NotFound("Inventory not found.");

			inventory.QuantityInStock = newQuantity;
			inventory.LastUpdated = DateTime.UtcNow;
			await _context.SaveChangesAsync();

			return Ok(new { message = "Stock updated.", inventory });
		}

		[HttpPut("decrease/{productId}")]
		public async Task<IActionResult> DecreaseStock(int productId, [FromBody] int quantity)
		{
			var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductID == productId);
			if (inventory == null) return NotFound("Inventory not found.");
			if (inventory.QuantityInStock < quantity) return BadRequest("Insufficient stock.");

			inventory.QuantityInStock -= quantity;
			inventory.LastUpdated = DateTime.UtcNow;
			await _context.SaveChangesAsync();

			return Ok(new { message = "Stock decremented.", inventory });
		}

		[HttpGet("{productId}")]
		public async Task<IActionResult> Get(int productId)
		{
			var inventory = await _context.Inventories
				.Include(i => i.Product)
				.FirstOrDefaultAsync(i => i.ProductID == productId);

			return inventory == null
				? NotFound("Inventory not found.")
				: Ok(inventory);
		}

		[HttpGet("low-stock/{threshold}")]
		public async Task<IActionResult> GetLowStock(int threshold)
		{
			var lowStock = await _context.Inventories
				.Include(i => i.Product)
				.Where(i => i.QuantityInStock <= threshold)
				.OrderBy(i => i.QuantityInStock)
				.ToListAsync();

			return Ok(lowStock);
		}

		[HttpDelete("{productId}")]
		public async Task<IActionResult> Delete(int productId)
		{
			var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductID == productId);
			if (inventory == null) return NotFound("Inventory not found.");

			_context.Inventories.Remove(inventory);
			await _context.SaveChangesAsync();
			return Ok(new { message = "Inventory deleted." });
		}
	}
}
