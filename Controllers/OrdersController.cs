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
			.GroupJoin(
				_context.OrderItems.Include(oi => oi.Product), // ✅ Ensure Products are loaded
				order => order.OrderID, // ✅ Match OrderID
				orderItem => orderItem.OrderID, // ✅ Match OrderItem's OrderID
				(order, orderItems) => new
				{
					order.OrderID,
					order.UserID,
					order.UserAddressID,
					order.OrderDate,
					order.Status,
					order.TotalAmount,
					OrderItems = orderItems.Select(oi => new
					{
						oi.ProductID,
						oi.Quantity,
						Product = new
						{
							oi.Product.ProductID,
							oi.Product.Name,
							oi.Product.Price
						}
					}).ToList()
				}
			)
			.ToListAsync();
			return Ok(orders);
		}

		// ✅ Get Order by ID (GET)  
		[HttpGet("{orderId}")]
		public async Task<IActionResult> GetOrderById(int orderId)
		{
			var order = await _context.Orders
				.Where(o => o.OrderID == orderId) // ✅ Filter for specific order
				.GroupJoin(
					_context.OrderItems.Include(oi => oi.Product), // ✅ Ensure Product details are loaded
					order => order.OrderID, // ✅ Match OrderID
					orderItem => orderItem.OrderID, // ✅ Match OrderItem's OrderID
					(order, orderItems) => new
					{
						order.OrderID,
						order.UserID,
						order.UserAddressID,
						order.OrderDate,
						order.Status,
						order.TotalAmount,
						OrderItems = orderItems.Select(oi => new
						{
							oi.ProductID,
							oi.Quantity,
							Product = new
							{
								oi.Product.ProductID,
								oi.Product.Name,
								oi.Product.Price
							}
						}).ToList()
					}
				)
				.FirstOrDefaultAsync(); // ✅ Get only one order

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
		public async Task<IActionResult> GetOrdersByUser(int userId)
		{
			var orders = await _context.Orders
				.Where(o => o.UserID == userId) // ✅ Filter orders by user ID
				.GroupJoin(
					_context.OrderItems.Include(oi => oi.Product), // ✅ Ensure Product details are loaded
					order => order.OrderID, // ✅ Match OrderID
					orderItem => orderItem.OrderID, // ✅ Match OrderItem's OrderID
					(order, orderItems) => new
					{
						order.OrderID,
						order.UserID,
						order.UserAddressID,
						order.OrderDate,
						order.Status,
						order.TotalAmount,
						OrderItems = orderItems.Select(oi => new
						{
							oi.ProductID,
							oi.Quantity,
							Product = new
							{
								oi.Product.ProductID,
								oi.Product.Name,
								oi.Product.Price
							}
						}).ToList()
					}
				)
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
				.Where(o => o.Status.ToLower() == status.ToLower()) // ✅ Filter orders by status
				.GroupJoin(
					_context.OrderItems.Include(oi => oi.Product), // ✅ Ensure Product details are loaded
					order => order.OrderID, // ✅ Match OrderID
					orderItem => orderItem.OrderID, // ✅ Match OrderItem's OrderID
					(order, orderItems) => new
					{
						order.OrderID,
						order.UserID,
						order.UserAddressID,
						order.OrderDate,
						order.Status,
						order.TotalAmount,
						OrderItems = orderItems.Select(oi => new
						{
							oi.ProductID,
							oi.Quantity,
							Product = new
							{
								oi.Product.ProductID,
								oi.Product.Name,
								oi.Product.Price
							}
						}).ToList()
					}
				)
				.ToListAsync();

			if (!orders.Any())
				return NotFound($"No orders found with status {status}");

			return Ok(orders);
		}

		//Get Top Customers
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

		//Get High Order Values 
		[HttpGet("high-value-orders")]
		public async Task<IActionResult> GetHighValueOrders()
		{
			// Calculate the average total amount
			var averageTotalAmount = await _context.Orders.AverageAsync(o => o.TotalAmount);

			// Fetch high-value orders using Method Syntax
			var highValueOrders = await _context.Orders
				.Join(_context.Users,
					  order => order.UserID, // Match Order's UserID
					  user => user.UserID, // Match User's UserID
					  (order, user) => new
					  {
						  OrderID = order.OrderID,
						  UserName = user.Name,
						  TotalAmount = order.TotalAmount
					  })
				.Where(order => order.TotalAmount > averageTotalAmount) // Filter orders above average
				.OrderByDescending(order => order.TotalAmount) // Sort by highest TotalAmount
				.ToListAsync();

			if (!highValueOrders.Any())
				return NotFound("No high-value orders found.");

			return Ok(highValueOrders);
		}

		//WHich customer order more than 5
		[HttpGet("repeat-customers")]
		public async Task<IActionResult> GetRepeatCustomers()
		{
			var repeatCustomers = await _context.Orders
				.Join(_context.Users,
					  order => order.UserID, // Match Order's UserID
					  user => user.UserID, // Match User's UserID
					  (order, user) => new
					  {
						  UserName = user.Name,
						  Email = user.Email,
						  OrderID = order.OrderID
					  })
				.GroupBy(order => new { order.UserName, order.Email })
				.Where(group => group.Count() > 5) // Filter customers with > 5 orders
				.OrderByDescending(group => group.Count()) // Sort by highest order count
				.Select(group => new
				{
					UserName = group.Key.UserName,
					Email = group.Key.Email,
					TotalOrders = group.Count()
				})
				.ToListAsync();

			if (!repeatCustomers.Any())
				return NotFound("No repeat customers found.");

			return Ok(repeatCustomers);
		}

		//Peak Time of Orders
		[HttpGet("peak-shopping-hours")]
		public async Task<IActionResult> GetPeakShoppingHours()
		{
			var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

			var peakHours = await _context.Orders
				.Where(order => order.OrderDate >= oneMonthAgo) // Filter orders from the last month
				.GroupBy(order => order.OrderDate.Hour) // Group by hour (24-hour format)
				.OrderByDescending(group => group.Count()) // Sort by highest order count
				.Select(group => new
				{
					Hour = group.Key + ":00", // Format hour as "HH:00"
					TotalOrderCount = group.Count()
				})
				.ToListAsync();

			if (!peakHours.Any())
				return NotFound("No order data found for peak hours.");

			return Ok(peakHours);
		}

		//Which month has most Orders
		[HttpGet("seasonal-order-trends")]
		public async Task<IActionResult> GetSeasonalOrderTrends()
		{
			var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

			var seasonalTrends = await _context.Orders
				.Where(order => order.OrderDate >= sixMonthsAgo) // Filter orders from the last 6 months
				.GroupBy(order => new { order.OrderDate.Year, order.OrderDate.Month }) // Group by year and month
				.OrderByDescending(group => group.Count()) // Sort by highest order count
				.Select(group => new
				{
					Month = $"{group.Key.Year}-{group.Key.Month:00}", // Format as "yyyy-MM"
					TotalOrders = group.Count()
				})
				.ToListAsync();

			if (!seasonalTrends.Any())
				return NotFound("No seasonal order data found.");

			return Ok(seasonalTrends);
		}

		//Revenue Per Month
		[HttpGet("monthly-revenue")]
		public async Task<IActionResult> GetMonthlyRevenue()
		{
			var monthlyRevenue = await _context.Orders
				.GroupBy(order => new { order.OrderDate.Year, order.OrderDate.Month }) // Group by Year-Month
				.OrderByDescending(group => group.Key.Year) // Sort by latest year first
				.ThenByDescending(group => group.Key.Month) // Sort by latest month first
				.Select(group => new
				{
					Month = $"{group.Key.Year}-{group.Key.Month:00}", // Format as "yyyy-MM"
					MonthlyRevenue = group.Sum(order => order.TotalAmount) // Sum total revenue per month
				})
				.ToListAsync();

			if (!monthlyRevenue.Any())
				return NotFound("No revenue data found.");

			return Ok(monthlyRevenue);
		}

		//Most Order Customers
		[HttpGet("loyal-customers")]
		public async Task<IActionResult> GetLoyalCustomers()
		{
			var loyalCustomers = await _context.Orders
				.GroupBy(order => new { order.User.Name, order.User.Email }) // Group by Customer Name & Email
				.OrderByDescending(group => group.Count()) // Sort by highest order count
				.Select(group => new
				{
					Name = group.Key.Name,
					Email = group.Key.Email,
					TotalOrders = group.Count()
				})
				.Take(5) // Select top 5 customers
				.ToListAsync();

			if (!loyalCustomers.Any())
				return NotFound("No loyal customers found.");

			return Ok(loyalCustomers);
		}

        //Full Details of ORDER 
		[HttpGet("full-order-report")]
		public async Task<IActionResult> GetFullOrderReport()
		{
			var fullOrderReport = await _context.Orders
				.Join(_context.Users,
					  order => order.UserID, // Match Order's UserID
					  user => user.UserID, // Match User's UserID
					  (order, user) => new
					  {
						  OrderID = order.OrderID,
						  Customer = user.Name,
						  Email = user.Email
					  })
				.Join(_context.OrderItems,
					  order => order.OrderID, // Match OrderID in Orders
					  orderItem => orderItem.OrderID, // Match OrderID in OrderItems
					  (order, orderItem) => new
					  {
						  order.OrderID,
						  order.Customer,
						  order.Email,
						  ProductID = orderItem.ProductID,
						  Quantity = orderItem.Quantity,
						  Price = orderItem.Price
					  })
				.Join(_context.Products,
					  orderItem => orderItem.ProductID, // Match ProductID in OrderItems
					  product => product.ProductID, // Match ProductID in Products
					  (orderItem, product) => new
					  {
						  orderItem.OrderID,
						  orderItem.Customer,
						  orderItem.Email,
						  Product = product.Name,
						  orderItem.Quantity,
						  orderItem.Price
					  })
				.Join(_context.Payments,
					  order => order.OrderID, // Match OrderID in Orders
					  payment => payment.OrderID, // Match OrderID in Payments
					  (order, payment) => new
					  {
						  order.OrderID,
						  order.Customer,
						  order.Email,
						  order.Product,
						  order.Quantity,
						  order.Price,
						  PaymentAmount = payment.Amount
					  })
				.Join(_context.Shippings,
					  order => order.OrderID, // Match OrderID in Orders
					  shipping => shipping.OrderID, // Match OrderID in Shipping
					  (order, shipping) => new
					  {
						  order.OrderID,
						  order.Customer,
						  order.Email,
						  order.Product,
						  order.Quantity,
						  order.Price,
						  order.PaymentAmount,
						  ShippingStatus = shipping.Status,
						  ShippingDate = shipping.ShippingDate
					  })
				.OrderBy(order => order.OrderID) // Sort by OrderID (ascending)
				.ToListAsync();

			if (!fullOrderReport.Any())
				return NotFound("No order data found.");

			return Ok(fullOrderReport);
		}

		//Pending Orders
		[HttpGet("pending-orders")]
		public async Task<IActionResult> GetPendingOrders()
		{
			var pendingOrders = await _context.Orders
				.Where(order => order.Status == "pending") // Filter orders with status 'pending'
				.OrderBy(order => order.OrderID) // Sort by OrderID (ascending)
				.ToListAsync();

			if (!pendingOrders.Any())
				return NotFound("No pending orders found.");

			return Ok(pendingOrders);
		}

		//Order of Last 30 Days 
		[HttpGet("orders-last-30-days")]
		public async Task<IActionResult> GetOrdersLast30Days()
		{
			var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

			var recentOrders = await _context.Orders
				.Where(order => order.OrderDate >= thirtyDaysAgo) // Filter orders from the last 30 days
				.GroupBy(order => order.OrderID) // Group by OrderID
				.OrderByDescending(group => group.Count()) // Sort by highest order count
				.Select(group => new
				{
					OrderID = group.Key,
					TotalOrders = group.Count()
				})
				.ToListAsync();

			if (!recentOrders.Any())
				return NotFound("No orders found in the last 30 days.");

			return Ok(recentOrders);
		}

		[HttpGet("recent-orders")]
		public async Task<IActionResult> GetRecentOrders()
		{
			var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

			var recentOrders = await _context.Orders
				.Where(order => order.OrderDate >= sevenDaysAgo) // Filter orders from the last 7 days
				.OrderByDescending(order => order.OrderID) // Sort by latest OrderID
				.Select(order => new
				{
					OrderID = order.OrderID,
					OrderDate = order.OrderDate,
					Status = order.Status,
					TotalAmount = order.TotalAmount
				})
				.ToListAsync();

			if (!recentOrders.Any())
				return NotFound("No recent orders found.");

			return Ok(recentOrders);
		}

		[HttpGet("cancelled-orders")]
		public async Task<IActionResult> GetCancelledOrdersAfterPayment()
		{
			var cancelledOrders = await _context.Orders
				.Join(_context.Payments,
					  order => order.OrderID, // Match OrderID in Orders
					  payment => payment.OrderID, // Match OrderID in Payments
					  (order, payment) => new
					  {
						  OrderID = order.OrderID,
						  Customer = order.User.Name, // Navigation property for User
						  PaymentMethod = payment.PaymentMethod,
						  Status = order.Status,
						  Amount = payment.Amount
					  })
				.Where(order => order.Status == "Cancelled") // Filter cancelled orders
				.OrderByDescending(order => order.Amount) // Sort by highest payment amount
				.ToListAsync();

			if (!cancelledOrders.Any())
				return NotFound("No cancelled orders found.");

			return Ok(cancelledOrders);
		}

		// 🔹 Update Stock on Order Placement
		[HttpPut("update-stock/{productId}/{quantity}")]  // 🔹 Explicit HTTP method
		public async Task<IActionResult> UpdateStockOnOrder(int productId, int quantity)
		{
			var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductID == productId);
			if (inventory == null || inventory.QuantityInStock < quantity)
				return BadRequest("Insufficient stock!");

			inventory.QuantityInStock -= quantity;
			inventory.LastUpdated = DateTime.UtcNow;

			_context.Inventories.Update(inventory);
			await _context.SaveChangesAsync();

			return Ok("Stock updated successfully.");
		}

		[HttpPost("place-order")]
		public async Task<IActionResult> PlaceOrder(Order order)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();

			try
			{
				foreach (var item in order.OrderItems)
				{
					// Fetch inventory directly
					var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductID == item.ProductID);
					if (inventory == null || inventory.QuantityInStock < item.Quantity)
						return BadRequest($"Insufficient stock for Product {item.ProductID}");

					// Deduct stock quantity
					inventory.QuantityInStock -= item.Quantity;
					inventory.LastUpdated = DateTime.UtcNow;
					_context.Inventories.Update(inventory);
				}

				_context.Orders.Add(order);
				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				return Ok("Order placed successfully, stock updated.");
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return StatusCode(500, $"An error occurred: {ex.Message}");
			}
		}

	}

}  

