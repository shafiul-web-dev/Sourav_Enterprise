using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Data;
using Sourav_Enterprise.DTO;
using Sourav_Enterprise.DTO.OrderDto.Sourav_Enterprise.DTO;
using Sourav_Enterprise.Models;

namespace Sourav_Enterprise.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
	public class OrdersController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public OrdersController(ApplicationDbContext context)
		{
			_context = context;
		}

		// ✅ Create Order (POST)  
		[HttpPost]
		[Route("CreateOrder")]
		public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto orderDto)
		{
			if (orderDto == null) return BadRequest("Invalid request body.");

			if (!_context.Users.Any(u => u.UserID == orderDto.UserID))
				return BadRequest("Invalid User.");

			if (!_context.UserAddresses.Any(ua => ua.AddressID == orderDto.AddressID && ua.UserID == orderDto.UserID))
				return BadRequest("Invalid Address: Address does not belong to user.");

			foreach (var item in orderDto.OrderItems)
			{
				var product = await _context.Products.FindAsync(item.ProductID);
				if (product == null || product.Stock < item.Quantity)
					return BadRequest($"Product {item.ProductID} is unavailable or out of stock.");
			}

			// ✅ Step 1: Create & Save Order First
			var order = new Order
			{
				UserID = orderDto.UserID,
				UserAddressID = orderDto.AddressID,
				Status = "Pending",
				OrderDate = DateTime.UtcNow
			};

			_context.Orders.Add(order);
			await _context.SaveChangesAsync(); // ✅ Ensure OrderID is generated

			// ✅ Step 2: Now Add OrderItems After Order is Saved
			foreach (var item in orderDto.OrderItems)
			{
				var orderItem = new OrderItem
				{
					OrderID = order.OrderID, // ✅ Correct OrderID
					ProductID = item.ProductID,
					Quantity = item.Quantity
				};
				_context.OrderItems.Add(orderItem);
			}

			await _context.SaveChangesAsync(); // ✅ Save OrderItems after Order exists

			return Ok(new { message = "Order Created Successfully!", OrderID = order.OrderID });
		}

		//✅ Get All Orders(GET)
		[HttpGet]
		[Route("GetAllOrders")]
		public async Task<IActionResult> GetOrders()
		{
			var orders = await _context.Orders
				.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.Product)
				.ToListAsync();
			return Ok(orders);
		}

		// ✅ Get Order by ID (GET)  
		[HttpGet("{orderId}")]
		public async Task<IActionResult> GetOrderById(int orderId)
		{
			var order = await _context.Orders
				.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.Product)
				.FirstOrDefaultAsync(o => o.OrderID == orderId);

			if (order == null)
				return NotFound($"Order ID {orderId} not found.");

			return Ok(order);
		}

		// ✅ Update Order Status (PUT)  
		[HttpPut("{orderId}/status/{newStatus}")]
		public async Task<IActionResult> UpdateOrderStatus(int orderId, string newStatus)
		{
			var order = await _context.Orders.FindAsync(orderId);
			if (order == null)
				return NotFound($"Order ID {orderId} not found.");

			if (order.Status == "Pending" && newStatus == "Processing")
				order.Status = "Processing";
			else if (order.Status == "Processing" && newStatus == "Shipped")
				order.Status = "Shipped";
			else if (order.Status == "Shipped" && newStatus == "Delivered")
				order.Status = "Delivered";
			else
				return BadRequest("Invalid status update.");

			await _context.SaveChangesAsync();
			return Ok(order);
		}

		// ✅ Handle Payment (POST)  
		[HttpPost("{orderId}/payment/{amountPaid}")]
		public async Task<IActionResult> HandlePayment(int orderId, decimal amountPaid)
		{
			var order = await _context.Orders.FindAsync(orderId);
			if (order == null)
				return NotFound($"Order ID {orderId} not found.");

			if (amountPaid < order.TotalAmount)
				return BadRequest("Insufficient payment.");

			order.Status = "Processing";
			var expense = new Expense { Amount = amountPaid, OrderID = order.OrderID };
			_context.Expenses.Add(expense);
			await _context.SaveChangesAsync();

			return Ok(order);
		}
		//// ✅ Cancel Order (DELETE)  
		[HttpDelete("CancelOrder/{orderId}")]
		public async Task<IActionResult> CancelOrder(int orderId)
		{
			var order = await _context.Orders.FindAsync(orderId);
			if (order == null)
				return NotFound($"Order ID {orderId} not found.");

			if (order.Status == "Shipped" || order.Status == "Delivered")
				return BadRequest("Order cannot be canceled at this stage.");

			order.Status = "Canceled";
			await _context.SaveChangesAsync();

			return Ok(order);
		}

		//	// ✅ Get Orders by User (GET)  
		[HttpGet("user/{userId}")]
		//[Route("GetOrdersByUser")]
		public async Task<IActionResult> GetOrdersByUser(int userId)
		{
			var orders = await _context.Orders
				.Where(o => o.UserID == userId)
				.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.Product)
				.ToListAsync();

			if (!orders.Any())
				return NotFound($"No orders found for User ID {userId}");

			return Ok(orders);
		}

		// ✅ Get Orders by Status (GET)  
		[HttpGet("status/{status}")]
		public async Task<IActionResult> GetOrdersByStatus(string status)
		{
			var orders = await _context.Orders
				.Where(o => o.Status.ToLower() == status.ToLower())
				.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.Product)
				.ToListAsync();

			if (!orders.Any())
				return NotFound($"No orders found with status {status}");

			return Ok(orders);
		}


		[HttpGet("top-customers")]
		public async Task<IActionResult> GetTopCustomers()
		{
			var topCustomers = await _context.Users
				.GroupJoin(_context.Orders, user => user.UserID, order => order.UserID,
					(user, orders) => new
					{
						UserID = user.UserID,
						UserName = user.Name,
						TotalSpent = orders.Sum(o => o.TotalAmount) // ✅ Calculates total spending
					})
				.OrderByDescending(x => x.TotalSpent)
				.Take(5) // ✅ Gets Top 5 Customers
				.ToListAsync();

			return Ok(topCustomers);
		}
	}

}  

