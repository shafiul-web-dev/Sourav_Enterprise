using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mono.TextTemplating;
using Sourav_Enterprise.Data;
using Sourav_Enterprise.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Sourav_Enterprise.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UsersController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public UsersController(ApplicationDbContext context)
		{
			_context = context;
		}

		#region User Management

		[HttpGet]
		public async Task<ActionResult<IEnumerable<User>>> GetAllUsers(int page = 1, int pageSize = 5)
		{
			int totalRecords = await _context.Users.CountAsync();
			int totalPages = (int)Math.Ceiling((decimal)totalRecords / pageSize);

			var allUsers = await _context.Users
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return Ok(new
			{
				Page = page,
				PageSize = pageSize,
				TotalRecords = totalRecords,
				TotalPages = totalPages,
				Users = allUsers
			});
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<User>> GetUserById(int id)
		{
			var user = await _context.Users.FindAsync(id);
			if (user == null)
			{
				return NotFound();
			}
			return Ok(user);
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> PutUser(int id, User user)
		{
			if (id != user.UserID)
			{
				return BadRequest();
			}

			_context.Entry(user).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!UserExists(id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}
			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteUser(int id)
		{
			var user = await _context.Users.FindAsync(id);
			if (user == null)
			{
				return NotFound();
			}

			_context.Users.Remove(user);
			await _context.SaveChangesAsync();
			return NoContent();
		}

		#endregion

		#region Authentication

		[HttpPost]
		[Route("register")]
		public async Task<IActionResult> RegisterUser([FromBody] User model)
		{
			if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.PasswordHash))
				return BadRequest("UserName, Email, and Password are required.");

			if (!model.Email.Contains("@") || !model.Email.Contains("."))
				return BadRequest("Invalid email format.");

			if (model.PasswordHash.Length < 8)
				return BadRequest("Password must be at least 8 characters long.");

			var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email);
			if (existingUser != null)
				return Conflict();

			model.PasswordHash = HashPassword(model.PasswordHash);

			await _context.Users.AddAsync(model);
			await _context.SaveChangesAsync();

			return Ok("User registered successfully.");
		}

		[HttpPost]
		[Route("login")]
		public async Task<IActionResult> LoginUser([FromBody] LoginRequest model)
		{
			var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
			if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
				return Unauthorized();

			var token = GenerateJwtToken(user);
			return Ok(new { Token = token });
		}

		[Authorize]
		[HttpGet]
		[Route("profile")]
		public async Task<IActionResult> GetUserProfile()
		{
			var userEmail = User.Identity.Name;
			var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
			return Ok(new { UserName = user.Name, Email = user.Email });
		}

		#endregion

		#region Customer Insights

		[HttpGet]
		[Route("CustomerLifeTimeValue")]
		public async Task<IActionResult> GetCustomerLifetimeValue(int page= 1, int pageSize = 5)
		{

			int totalRecors = await _context.Users.CountAsync();

			int totalPages = (int)Math.Ceiling((double)totalRecors / pageSize);

			var clvData = await _context.Users.GroupJoin(
				_context.Orders,
				user => user.UserID,
				order => order.UserID,
				(user, orders) => new
				{
					UserID = user.UserID,
					Name = user.Name,
					Email = user.Email,
					TotalOrders = orders.Any() ? orders.Count() : 0, // ✅ Handles empty collections
					LifeTimeValue = orders.Any() ? orders.Sum(o => (decimal?)o.TotalAmount) ?? 0 : 0, // ✅ Prevents null sums
					AvgOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0 // ✅ Prevents division by zero

				}).Skip((page - 1) * pageSize)
				  .Take(pageSize)
				  .OrderByDescending(u => u.LifeTimeValue).ToListAsync();

			return Ok(new
			{
				Page = page,
				PageSize = pageSize,
				TotalRecords = totalRecors,
				TotalPages = totalPages,
				CustomerLifetimeValue = clvData
			});
		}

		[HttpGet]
		[Route("VipCustomers")]
		public async Task<IActionResult> GetVipCustomers()
		{

			var customerSpendingTiers = await _context.Users.GroupJoin(
				_context.Orders,
				user => user.UserID,
				order => order.UserID,
				(user, orders) => new
				{
					UserID = user.UserID,
					Name = user.Name,
					Email = user.Email,
					LifetimeSpend = orders.Any() ? orders.Sum(o => (decimal?)o.TotalAmount) ?? 0 : 0,
					CustomerTier = orders.Any() ?
						(orders.Sum(o => (decimal?)o.TotalAmount) > 5000 ? "Premium" :
						orders.Sum(o => (decimal?)o.TotalAmount) >= 1000 && orders.Sum(o => (decimal?)o.TotalAmount) <= 5000 ? "Gold" :
						orders.Sum(o => (decimal?)o.TotalAmount) >= 500 && orders.Sum(o => (decimal?)o.TotalAmount) <= 1000 ? "Silver" : "Standard")
						: "No_Orders" 
				}).OrderByDescending(u => u.LifetimeSpend).ToListAsync();

			return Ok(customerSpendingTiers);
		}

		[HttpGet("high-frequency-buyers")]
		public async Task<IActionResult> GetHighFrequencyBuyers()
		{

			var highFrequencyBuyers = await _context.Users
				.Where(user => _context.Orders.Count(order => order.UserID == user.UserID) > 3)
				.GroupJoin(
					_context.Orders,
					user => user.UserID,
					order => order.UserID,
					(user, orders) => new
					{
						UserID = user.UserID,
						Name = user.Name,
						TotalOrders = orders.Count() 
					}
				)
				.OrderByDescending(u => u.TotalOrders)
				.ToListAsync();

			return Ok(highFrequencyBuyers);
		}

		[HttpGet("customer-types")]
		public async Task<IActionResult> GetCustomerTypes()
		{
			var customerTypes = await _context.Users.GroupJoin(
				_context.Orders,
				user => user.UserID,
				order => order.UserID,
				(user, orders) => new
				{
					UserID = user.UserID,
					Name = user.Name,
					CustomerType = orders.Count() == 1 ? "New Customer" :
						   orders.Count() > 1 ? "Returning Customer" : "No Orders"
				})
				.OrderByDescending(u => u.CustomerType)
				.ToListAsync();

			return Ok(customerTypes);
		}

		[HttpGet("fraud-detection")]
		public async Task<IActionResult> GetFraudCustomers()
		{
			var fraudCustomers = await _context.Users
			.Where(user => _context.Orders.Count(o => o.UserID == user.UserID && o.Status == "Cancelled") > 3)
			.GroupJoin(
				_context.Orders,
				user => user.UserID,
				order => order.UserID,
				(user, orders) => new
				{
					UserID = user.UserID,
					Name = user.Name,
					Email = user.Email,
					CanceledOrders = orders.Count(o => o.Status == "Cancelled") 
				}
			)
			.OrderByDescending(u => u.CanceledOrders)
			.ToListAsync();

			return Ok(fraudCustomers);
		}

		[HttpGet("best-customers")]
		public async Task<IActionResult> GetBestCustomers()
		{
			var bestCustomers = await _context.Users
				.Where(user => _context.Orders.Any(order => order.UserID == user.UserID))
				.GroupJoin(
					_context.Orders,
					user => user.UserID,
					order => order.UserID,
					(user, orders) => new
					{
						UserID = user.UserID,
						Name = user.Name,
						Email = user.Email,
						OrdersCount = orders.Count(),
						TotalSpend = orders.Sum(o => o.TotalAmount)
					}
				)
				.OrderByDescending(u => u.OrdersCount)
				.ThenByDescending(u => u.TotalSpend)
				.ToListAsync();

			
			var rankedCustomers = bestCustomers.Select((customer, index) => new
			{
				customer.UserID,
				customer.Name,
				customer.Email,
				customer.OrdersCount,
				customer.TotalSpend,
				CustomerRank = index + 1
			}).ToList();

			return Ok(rankedCustomers);
		}

		[HttpGet("wishlist-items")]
		public async Task<IActionResult> GetUserWishlistItems()
		{
			var wishlistItems = await _context.Users
				.Join(_context.Wishlists, user => user.UserID, wishlist => wishlist.UserID,
					(user, wishlist) => new { user, wishlist })
				.Join(_context.Products, uw => uw.wishlist.ProductID, product => product.ProductID,
					(uw, product) => new { uw.user, Product = product, uw.wishlist })
				.GroupBy(x => new { x.user.UserID, x.user.Name, ProductName = x.Product.Name })
				.OrderBy(g => g.Key.UserID)
				.Select(g => new
				{
					UserID = g.Key.UserID,
					UserName = g.Key.Name,
					ItemName = g.Key.ProductName, 
					TotalWishList = g.Count()
				})
				.ToListAsync();

			return Ok(wishlistItems);
		}

		[HttpGet("users-without-orders")]
		public async Task<IActionResult> GetUsersWithoutOrders()
		{
			var usersWithoutOrders = await _context.Users
				.GroupJoin(
					_context.Orders,
					user => user.UserID,
					order => order.UserID,
					(user, orders) => new { user, orders }
				)
				.SelectMany(u => u.orders.DefaultIfEmpty(), (u, order) => new { u.user, order })
				.Where(u => u.order == null)
				.Select(u => new
				{
					UserID = u.user.UserID,
					UserName = u.user.Name
				}).ToListAsync();
			return Ok(usersWithoutOrders);
		}

		[HttpGet("user-order-counts")]
		public async Task<IActionResult> GetUserOrderCounts()
		{

			var userOrderCounts = await _context.Orders
				.Join(_context.Users, order => order.UserID, user => user.UserID,
					(order, user) => new { order, user })
				.GroupBy(x => new { x.user.UserID, x.user.Name })
				.Select(g => new
				{
					UserID = g.Key.UserID,
					UserName = g.Key.Name,
					TotalOrders = g.Count() 
				})
				.OrderByDescending(x => x.TotalOrders)
				.ToListAsync();

			return Ok(userOrderCounts);
		}

		[HttpGet("inactive-customers")]
		public async Task<IActionResult> GetInactiveCustomers()
		{
			var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

			var inactiveCustomers = await _context.Users
				.GroupJoin(_context.Orders, user => user.UserID, order => order.UserID,
					(user, orders) => new
					{
						UserID = user.UserID,
						UserName = user.Name,
						LastOrderDate = orders.Any() ? orders.Max(o => o.OrderDate) : (DateTime?)null
					})
				.Where(c => c.LastOrderDate == null || c.LastOrderDate < sixMonthsAgo)
				.OrderBy(c => c.UserID).ThenByDescending(c => c.LastOrderDate)
				.ToListAsync();

			return Ok(inactiveCustomers);
		}

		[HttpGet("top-users-orders")]
		public async Task<IActionResult> GetTopUsersByOrderValue()
		{
			var topUsersByOrderValue = await _context.Users
				.GroupJoin(_context.Orders, user => user.UserID, order => order.UserID,
					(user, orders) => new
					{
						UserID = user.UserID,
						UserName = user.Name,
						MaxOrdersValue = orders.Any() ? orders.Max(o => o.TotalAmount) : 0
					})
				.OrderByDescending(x => x.MaxOrdersValue)
				.Take(5) 
				.ToListAsync();

			return Ok(topUsersByOrderValue);
		}

		[HttpGet("abandoned-cart-users")]
		public async Task<IActionResult> GetAbandonedCartUsers()
		{
			var abandonedCartUsers = await _context.Users
				.Join(_context.Carts, user => user.UserID, cart => cart.UserID,
					(user, cart) => new { user, cart })
				.Join(_context.Products, uc => uc.cart.ProductID, product => product.ProductID,
					(uc, product) => new { uc.user, uc.cart, product })
				.GroupJoin(_context.Orders, ucp => ucp.user.UserID, order => order.UserID,
					(ucp, orders) => new
					{
						ucp.user.Name,
						ucp.cart.ProductID,
						ProductName = ucp.product.Name,
						ucp.cart.Quantity,
						HasOrder = orders.Any()
					})
				.Where(x => !x.HasOrder)
				.ToListAsync();
			return Ok(abandonedCartUsers);
		}

		[HttpGet("users-with-pending-orders")]
		public async Task<IActionResult> GetUsersWithPendingOrders()
		{
			var pendingOrders = await _context.Users
				.Join(_context.Orders, user => user.UserID, order => order.UserID,
					(user, order) => new { user, order })
				.Where(x => x.order.Status.ToLower() == "pending")
				.Select(x => new
				{
					UserID = x.user.UserID,
					OrderID = x.order.OrderID,
					Status = x.order.Status
				})
				.ToListAsync();

			return Ok(pendingOrders);
		}

		[HttpGet("user-orders/{userId}")]
		public async Task<IActionResult> GetUserOrderDetailsById(int userId)
		{
			var userOrderDetails = await _context.Orders
				.Where(user => user.UserID == userId)
				.GroupJoin(
				_context.OrderItems.Include(oi => oi.Product),
				order => order.OrderID,
				orderItem => orderItem.OrderID,
				(order, orderItem) => new
				{
					orderID = order.OrderID,
					TotalAmount = order.TotalAmount,
					Status = order.Status,
					OrderDate = order.OrderDate,
					Products = orderItem.Select(item => new
					{
						item.ProductID,
						ProductName = item.Product.Name,
						item.Quantity,
						item.Price
					}).ToList()

				}).ToListAsync();

			if (!userOrderDetails.Any()) return NotFound($"Order is not Found Under {userId}");
			return Ok(userOrderDetails);

		}

		#endregion

		#region Helper Methods

		private bool UserExists(int id)
		{
			return _context.Users.Any(e => e.UserID == id);
		}

		private string GenerateJwtToken(User user)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.UTF8.GetBytes("ThisIsAVerySecureAndLongSecretKeyForJWT!");

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[]
				{
					new Claim(ClaimTypes.Name, user.Name),
					new Claim(ClaimTypes.Email, user.Email)
				}),
				Expires = DateTime.UtcNow.AddHours(1),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		private bool VerifyPassword(string inputPassword, string hashedPassword)
		{
			return BCrypt.Net.BCrypt.Verify(inputPassword, hashedPassword);
		}

		private string HashPassword(string password)
		{
			return BCrypt.Net.BCrypt.HashPassword(password);
		}

		#endregion
	}
}
