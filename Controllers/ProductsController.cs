using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Sourav_Enterprise.Data;
using Sourav_Enterprise.Models;

[Route("api/products")]
[ApiController]
public class ProductController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public ProductController(ApplicationDbContext context)
	{
		_context = context;
	}

	#region // 🔹 Business Logic for Products
	[HttpGet]
	public async Task<IActionResult> GetProducts(
	string? search, string? sortBy, bool? ascending = true, int page = 1, int pageSize = 10)
	{
		var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

		if (!string.IsNullOrEmpty(search))
			productsQuery = productsQuery.Where(p => p.Name.Contains(search));


		if (sortBy == "rating")
		{
			var productsWithRatings = await _context.Products
				.Select(p => new
				{
					Product = p,
					AverageRating = _context.Reviews.Where(r => r.ProductID == p.ProductID).Any() ?
									_context.Reviews.Where(r => r.ProductID == p.ProductID).Average(r => r.Rating) : 0
				})
				.ToListAsync();

			productsWithRatings = ascending == true
				? productsWithRatings.OrderBy(p => p.AverageRating).ToList()
				: productsWithRatings.OrderByDescending(p => p.AverageRating).ToList();

			return Ok(new { TotalRecords = productsWithRatings.Count, Products = productsWithRatings });
		}
		else
		{
			productsQuery = sortBy switch
			{
				"price" => ascending == true ? productsQuery.OrderBy(p => p.Price) : productsQuery.OrderByDescending(p => p.Price),
				"name" => ascending == true ? productsQuery.OrderBy(p => p.Name) : productsQuery.OrderByDescending(p => p.Name),
				_ => productsQuery.OrderBy(p => p.ProductID)
			};
		}

		var totalRecords = await productsQuery.CountAsync();
		var products = await productsQuery
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

		return Ok(new { TotalRecords = totalRecords, Products = products });
	}

	[HttpGet("filter")]
	public async Task<IActionResult> FilterProducts(int? categoryId, decimal? minPrice, decimal? maxPrice, bool? inStock)
	{
		var query = _context.Products.AsQueryable();

		if (categoryId.HasValue)
			query = query.Where(p => p.CategoryID == categoryId);

		if (minPrice.HasValue)
			query = query.Where(p => p.Price >= minPrice);

		if (maxPrice.HasValue)
			query = query.Where(p => p.Price <= maxPrice);


		if (inStock.HasValue)
			query = inStock.Value
				? query.Where(p => _context.Inventories.Any(i => i.ProductID == p.ProductID && i.QuantityInStock > 0))
				: query.Where(p => _context.Inventories.Any(i => i.ProductID == p.ProductID && i.QuantityInStock == 0));

		var products = await query.ToListAsync();
		return Ok(products);
	}


	[HttpGet("{id}")]
	public async Task<IActionResult> GetProductById(int id)
	{
		var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductID == id);
		if (product == null) return NotFound("Product not found.");

		var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductID == id);
		int availableStock = inventory != null ? inventory.QuantityInStock : 0;
		string stockStatus = availableStock > 0 ? "In Stock" : "Out of Stock";

		var reviews = await _context.Reviews.Where(r => r.ProductID == id).ToListAsync();
		double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

		return Ok(new { product, AvailableStock = availableStock, StockStatus = stockStatus, AverageRating = avgRating });
	}


	[HttpPost]
	public async Task<IActionResult> CreateProduct(Product product)
	{
		_context.Products.Add(product);
		await _context.SaveChangesAsync();
		return CreatedAtAction(nameof(GetProductById), new { id = product.ProductID }, product);
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> UpdateProduct(int id, Product updatedProduct)
	{
		var product = await _context.Products.FindAsync(id);
		if (product == null)
			return NotFound("Product not found.");

		product.Name = updatedProduct.Name;
		product.Description = updatedProduct.Description;
		product.Price = updatedProduct.Price;
		product.Stock = updatedProduct.Stock;
		product.CategoryID = updatedProduct.CategoryID;

		_context.Products.Update(product);
		await _context.SaveChangesAsync();
		return Ok("Product updated successfully.");
	}

	[HttpPut("update-stock/{productId}")]
	public async Task<IActionResult> UpdateStock(int productId, int newStock)
	{
		var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductID == productId);
		if (inventory == null) return NotFound("Product not found in inventory.");

		inventory.QuantityInStock = newStock;
		inventory.LastUpdated = DateTime.UtcNow;

		_context.Inventories.Update(inventory);
		await _context.SaveChangesAsync();

		return Ok($"Stock updated successfully for Product {productId}");
	}

	[HttpPut("restock/{productId}")]
	public async Task<IActionResult> RestockProduct(int productId, int newStock)
	{
		var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductID == productId);
		if (inventory == null) return NotFound("Product not found in inventory.");

		inventory.QuantityInStock += newStock; 
		inventory.LastUpdated = DateTime.UtcNow;

		_context.Inventories.Update(inventory);
		await _context.SaveChangesAsync();

		return Ok($"Product {productId} restocked successfully. New Quantity: {inventory.QuantityInStock}");
	}


	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteProduct(int id)
	{
		var product = await _context.Products.FindAsync(id);
		if (product == null)
			return NotFound("Product not found.");

		_context.Products.Remove(product);
		await _context.SaveChangesAsync();
		return Ok("Product deleted successfully.");
	}

	[HttpPost("{productId}/reviews")]
	public async Task<IActionResult> AddReview(int productId, Review review)
	{
		if (!_context.Products.Any(p => p.ProductID == productId))
			return NotFound("Product not found.");

		review.ProductID = productId;
		_context.Reviews.Add(review);
		await _context.SaveChangesAsync();

		return Ok("Review added successfully.");
	}

	[HttpGet("{productId}/reviews")]
	public async Task<IActionResult> GetProductReviews(int productId)
	{
		var reviews = await _context.Reviews.Where(r => r.ProductID == productId).ToListAsync();
		if (!reviews.Any()) return NotFound("No reviews for this product.");

		return Ok(reviews);
	}

	[HttpGet("{productId}/rating")]
	public async Task<IActionResult> GetAverageRating(int productId)
	{
		var reviews = await _context.Reviews.Where(r => r.ProductID == productId).ToListAsync();
		if (!reviews.Any()) return Ok(new { ProductID = productId, AverageRating = "No reviews yet." });

		double avgRating = reviews.Average(r => r.Rating);
		return Ok(new { ProductID = productId, AverageRating = avgRating });
	}

	#endregion // 🔹 Business Logic for Products

	#region // Query from Database
	//Best-selling Products
	[HttpGet("best-sellers")]
	public async Task<IActionResult> GetBestSellingProducts()
	{
		var bestSellers = await _context.OrderItems
			.GroupBy(o => o.Product.Name)
			.Select(g => new
			{
				ProductName = g.Key,
				TotalRevenue = g.Sum(o => o.Price * o.Quantity)
			})
			.OrderByDescending(p => p.TotalRevenue)
			.Take(5)
			.ToListAsync();

		return Ok(bestSellers);
	}

	// Products with Low Stock
	[HttpGet("low-stock")]
	public async Task<IActionResult> GetLowStockProducts()
	{
		var lowStockProducts = await _context.Inventories
			.Join(_context.Products,
				inventory => inventory.ProductID,
				product => product.ProductID,
				(inventory, product) => new
				{
					ProductName = product.Name,
					QuantityInStock = inventory.QuantityInStock,
					TotalSold = _context.OrderItems.Where(o => o.ProductID == product.ProductID)
												   .Sum(o => (int?)o.Quantity) ?? 0
				})
			.Where(p => p.QuantityInStock < p.TotalSold)
			.Select(p => new
			{
				ProductName = p.ProductName,
				QuantityInStock = p.QuantityInStock,
				TotalSold = p.TotalSold,
				StockStatus = p.QuantityInStock < 5 ? "Critical Low Stock" :
							  p.QuantityInStock >= 5 && p.QuantityInStock <= 15 ? "Low Stock" : "Sufficient Stock"
			})
			.OrderBy(p => p.QuantityInStock)
			.ToListAsync();

		return Ok(lowStockProducts);
	}

	//revenue-analysis per product
	[HttpGet("revenue-analysis")]
	public async Task<IActionResult> GetProductRevenueAnalysis()
	{
		var revenueData = await _context.OrderItems
			.GroupBy(o => o.Product.Name)
			.Select(g => new
			{
				ProductName = g.Key,
				TotalSold = g.Sum(o => o.Quantity),
				TotalRevenue = g.Sum(o => o.Price * o.Quantity)
			})
			.OrderByDescending(p => p.TotalRevenue)
			.ToListAsync();

		return Ok(revenueData);
	}

	//User Purchase History
	[HttpGet("user-orders/{userId}")]
	public async Task<IActionResult> GetUserPurchaseHistory(int userId)
	{
		var purchaseHistory = await _context.Orders
	        .Where(o => o.UserID == userId)
	        .GroupJoin(
	    	_context.OrderItems,
		    order => order.OrderID,
	    	orderItem => orderItem.OrderID,
		    (order, orderItems) => new
		    {
		    	UserID = order.UserID,
			    UserName = order.User.Name,
		    	OrderID = order.OrderID,
		     	OrderDate = order.OrderDate,
		    	Products = orderItems.Select(oi => new
		     	{
			     	ProductName = oi.Product.Name,
			    	Quantity = oi.Quantity,
			    	TotalSpent = oi.Price * oi.Quantity
		     	}).ToList()
	    	})
	        .OrderByDescending(o => o.OrderDate)
	        .ToListAsync();
		return Ok(purchaseHistory);
	}

	//Most Reviewed Products
	[HttpGet("most-reviewed")]
	public async Task<IActionResult> GetMostReviewedProducts()
	{
		var mostReviewedProducts = await _context.Reviews
			.GroupBy(r => r.Product.Name)
			.Select(g => new
			{
				ProductName = g.Key,
				TotalReviews = g.Count()
			})
			.OrderByDescending(p => p.TotalReviews)
			.ToListAsync();

		return Ok(mostReviewedProducts);
	}
	//recently added products
	[HttpGet("recent-products")]
	public async Task<IActionResult> GetRecentlyAddedProducts()
	{
		var recentProducts = await _context.Products
			.OrderByDescending(p => p.CreatedAt)
			.Select(p => new
			{
				ProductID = p.ProductID,
				ProductName = p.Name,
				CreatedAt = p.CreatedAt
			})
			.ToListAsync();

		return Ok(recentProducts);
	}

	//Category-based-product-sell
	[HttpGet("category-sales")]
	public async Task<IActionResult> GetCategorySalesBreakdown()
	{
		var categorySales = await _context.OrderItems
			.Join(_context.Products,
				orderItem => orderItem.ProductID,
				product => product.ProductID,
				(orderItem, product) => new { orderItem, product })
			.Join(_context.Categories,
				op => op.product.CategoryID,
				category => category.CategoryID,
				(op, category) => new
				{
					ProductName = op.product.Name,
					CategoryName = category.Name,
					TotalRevenue = op.orderItem.Quantity * op.orderItem.Price
				})
			.GroupBy(c => new { c.ProductName, c.CategoryName })
			.Select(g => new
			{
				ProductName = g.Key.ProductName,
				CategoryName = g.Key.CategoryName,
				TotalRevenue = g.Sum(x => x.TotalRevenue)
			})
			.OrderByDescending(x => x.TotalRevenue)
			.ToListAsync();

		return Ok(categorySales);
	}

	#endregion

}

