using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Data;
using Sourav_Enterprise.DTO.ExpenseDto;
using Sourav_Enterprise.Models;

namespace Sourav_Enterprise.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ExpensesController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		public ExpensesController(ApplicationDbContext context) => _context = context;


		[HttpPost]
		public async Task<IActionResult> AddExpense([FromBody] ExpenseDto dto)
		{
			var expense = new Expense
			{
				AdminID = dto.AdminID,
				OrderID = dto.OrderID,
				ProductID = dto.ProductID,
				ShippingID = dto.ShippingID,
				InventoryID = dto.InventoryID,
				Category = dto.Category,
				Amount = dto.Amount,
				Description = dto.Description,
				ExpenseDate = dto.ExpenseDate ?? DateTime.UtcNow
			};

			_context.Expenses.Add(expense);
			await _context.SaveChangesAsync();
			return Ok(new { message = "Expense recorded.", expense });
		}

		[HttpGet]
		public async Task<IActionResult> GetAll(
			[FromQuery] string category,
			[FromQuery] int? adminId,
			[FromQuery] DateTime? from,
			[FromQuery] DateTime? to)
		{
			var query = _context.Expenses
				.Include(e => e.AdminUser)
				.Include(e => e.Product)
				.Include(e => e.Order)
				.AsQueryable();

			if (!string.IsNullOrEmpty(category))
				query = query.Where(e => e.Category.ToLower() == category.ToLower());

			if (adminId.HasValue)
				query = query.Where(e => e.AdminID == adminId.Value);

			if (from.HasValue)
				query = query.Where(e => e.ExpenseDate >= from.Value);

			if (to.HasValue)
				query = query.Where(e => e.ExpenseDate <= to.Value);

			var expenses = await query.OrderByDescending(e => e.ExpenseDate).ToListAsync();
			return Ok(expenses);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			var expense = await _context.Expenses
				.Include(e => e.AdminUser)
				.Include(e => e.Product)
				.Include(e => e.Order)
				.FirstOrDefaultAsync(e => e.ExpenseID == id);

			return expense == null ? NotFound("Expense not found.") : Ok(expense);
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateExpense(int id, [FromBody] ExpenseDto dto)
		{
			var expense = await _context.Expenses.FindAsync(id);
			if (expense == null) return NotFound("Expense not found.");

			expense.AdminID = dto.AdminID;
			expense.OrderID = dto.OrderID;
			expense.ProductID = dto.ProductID;
			expense.ShippingID = dto.ShippingID;
			expense.InventoryID = dto.InventoryID;
			expense.Category = dto.Category;
			expense.Amount = dto.Amount;
			expense.Description = dto.Description;
			expense.ExpenseDate = dto.ExpenseDate ?? expense.ExpenseDate;

			await _context.SaveChangesAsync();
			return Ok(new { message = "Expense updated.", expense });
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteExpense(int id)
		{
			var expense = await _context.Expenses.FindAsync(id);
			if (expense == null) return NotFound("Expense not found.");

			_context.Expenses.Remove(expense);
			await _context.SaveChangesAsync();
			return Ok(new { message = "Expense deleted." });
		}

		[HttpGet("summary")]
		public async Task<IActionResult> GetSummary([FromQuery] int? month, int? year)
		{
			var now = DateTime.UtcNow;
			var targetMonth = month ?? now.Month;
			var targetYear = year ?? now.Year;

			var summary = await _context.Expenses
				.Where(e => e.ExpenseDate.Month == targetMonth && e.ExpenseDate.Year == targetYear)
				.GroupBy(e => e.Category)
				.Select(g => new
				{
					Category = g.Key,
					Total = g.Sum(e => e.Amount)
				})
				.ToListAsync();

			var overall = summary.Sum(s => s.Total);

			return Ok(new
			{
				Month = targetMonth,
				Year = targetYear,
				Total = overall,
				Breakdown = summary
			});
		}
		[HttpGet("report/category-breakdown")]
		public async Task<IActionResult> GetBreakdownByCategory()
		{
			var breakdown = await _context.Expenses
				.GroupBy(e => e.Category)
				.Select(g => new
				{
					Category = g.Key,
					TotalTransactions = g.Count(),
					TotalSpent = g.Sum(e => e.Amount)
				})
				.OrderByDescending(g => g.TotalSpent)
				.ToListAsync();

			return Ok(breakdown);
		}
		[HttpGet("report/largest-by-category")]
		public async Task<IActionResult> GetLargestExpenseByCategory()
		{
			var data = await _context.Expenses
				.GroupBy(e => e.Category)
				.Select(g => new
				{
					Category = g.Key,
					LargestTransaction = g.Max(e => e.Amount),
					TotalSpent = g.Sum(e => e.Amount)
				})
				.OrderByDescending(g => g.LargestTransaction)
				.ToListAsync();

			return Ok(data);
		}
		[HttpGet("report/monthly-trend")]
		public async Task<IActionResult> GetMonthlyTrend()
		{
			var monthlyTrend = await _context.Expenses
				.GroupBy(e => e.ExpenseDate.ToString("yyyy-MM"))
				.Select(g => new
				{
					Month = g.Key,
					TotalSpent = g.Sum(e => e.Amount)
				})
				.OrderByDescending(g => g.TotalSpent)
				.ToListAsync();

			return Ok(monthlyTrend);
		}
		[HttpGet("report/inventory-profit")]
		public async Task<IActionResult> GetInventoryProfitability()
		{
			var report = await (
				from e in _context.Expenses
				join i in _context.Inventories on e.ExpenseID equals i.ExpenseID
				join p in _context.Products on i.ProductID equals p.ProductID
				join oi in _context.OrderItems on p.ProductID equals oi.ProductID
				where e.Category == "Inventory"
				select new
				{
					Cost = e.Amount,
					Revenue = oi.Quantity * oi.Price
				}
			).ToListAsync();

			var totalCost = report.Sum(x => x.Cost);
			var totalRevenue = report.Sum(x => x.Revenue);
			var profit = totalRevenue - totalCost;

			return Ok(new
			{
				TotalInventoryCost = totalCost,
				TotalRevenueGenerated = totalRevenue,
				ProfitFromInventory = profit
			});
		}
	}
}
