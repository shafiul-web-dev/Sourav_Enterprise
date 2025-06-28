using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Data;
using Sourav_Enterprise.Models;

namespace Sourav_Enterprise.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PaymentController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public PaymentController(ApplicationDbContext context)
		{
			_context = context;
		}

		// 1️⃣ Create a payment entry
		[HttpPost("process")]
		public async Task<IActionResult> ProcessPayment([FromBody] Payment request)
		{
			var order = await _context.Orders.FindAsync(request.OrderID);
			if (order == null) return NotFound("Order not found.");

			var payment = new Payment
			{
				OrderID = request.OrderID,
				Amount = request.Amount,
				PaymentMethod = request.PaymentMethod,
				PaymentDate = DateTime.UtcNow
			};

			_context.Payments.Add(payment);

			order.Status = "Paid";

			await _context.SaveChangesAsync();

			return Ok(new { message = "Payment processed successfully." });
		}

		[HttpGet("{paymentId}")]
		public async Task<IActionResult> GetPaymentById(int paymentId)
		{
			var payment = await _context.Payments
				.Include(p => p.Order)
				.FirstOrDefaultAsync(p => p.PaymentID == paymentId);

			if (payment == null) return NotFound("Payment not found.");

			return Ok(payment);
		}

		[HttpGet("by-order/{orderId}")]
		public async Task<IActionResult> GetPaymentByOrderId(int orderId)
		{
			var payment = await _context.Payments
				.FirstOrDefaultAsync(p => p.OrderID == orderId);

			if (payment == null) return NotFound("No payment found for this order.");

			return Ok(payment);
		}
		
		[HttpDelete("{paymentId}")]
		public async Task<IActionResult> DeletePayment(int paymentId)
		{
			var payment = await _context.Payments.FindAsync(paymentId);
			if (payment == null) return NotFound("Payment not found.");

			_context.Payments.Remove(payment);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Payment deleted." });
		}
		[HttpGet("all")]
		public async Task<IActionResult> GetAllPayments()
		{
			var payments = await _context.Payments
				.Include(p => p.Order)
				.OrderByDescending(p => p.PaymentDate)
				.ToListAsync();

			return Ok(payments);
		}
		[HttpGet("by-user/{userId}")]
		public async Task<IActionResult> GetPaymentsByUser(int userId)
		{
			var payments = await _context.Payments
				.Include(p => p.Order)
				.Where(p => p.Order.UserID == userId)
				.OrderByDescending(p => p.PaymentDate)
				.ToListAsync();

			return Ok(payments);
		}
		[HttpGet("revenue/daily")]
		public async Task<IActionResult> GetDailyRevenue()
		{
			var today = DateTime.UtcNow.Date;

			var total = await _context.Payments
				.Where(p => p.PaymentDate.Date == today)
				.SumAsync(p => p.Amount);

			return Ok(new { date = today, totalRevenue = total });
		}

	}
}
