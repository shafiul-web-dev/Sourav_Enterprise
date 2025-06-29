using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Data;
using Sourav_Enterprise.DTO.ReviewDto;
using Sourav_Enterprise.Models;

namespace Sourav_Enterprise.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ReviewController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		public ReviewController(ApplicationDbContext context)
		{
			_context = context;
		}
		[HttpPost]
		public async Task<IActionResult> AddReview([FromBody] ReviewDto request)
		{
			var productExists = await _context.Products.AnyAsync(p => p.ProductID == request.ProductID);
			if (!productExists) return NotFound("Product not found.");

			
			var userPurchased = await _context.Orders
				.Include(o => o.OrderItems)
				.AnyAsync(o => o.UserID == request.UserID &&
							   o.OrderItems.Any(oi => oi.ProductID == request.ProductID) &&
							   o.Status == "Delivered");

			if (!userPurchased)
				return BadRequest("Only verified buyers can submit reviews.");

			
			var exists = await _context.Reviews
				.AnyAsync(r => r.UserID == request.UserID && r.ProductID == request.ProductID);
			if (exists) return Conflict("Review already exists. Use update instead.");

			var review = new Review
			{
				UserID = request.UserID,
				ProductID = request.ProductID,
				Rating = request.Rating,
				Comment = request.Comment,
				CreatedAt = DateTime.UtcNow
			};

			_context.Reviews.Add(review);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Review added.", review });
		}

		
		[HttpGet("by-product/{productId}")]
		public async Task<IActionResult> GetByProduct(int productId)
		{
			var reviews = await _context.Reviews
				.Include(r => r.User)
				.Where(r => r.ProductID == productId)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync();

			return Ok(reviews);
		}

	
		[HttpGet("by-user/{userId}")]
		public async Task<IActionResult> GetByUser(int userId)
		{
			var reviews = await _context.Reviews
				.Where(r => r.UserID == userId)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync();

			return Ok(reviews);
		}

	
		[HttpPut("{reviewId}")]
		public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] ReviewDto request)
		{
			var review = await _context.Reviews.FindAsync(reviewId);
			if (review == null) return NotFound("Review not found.");

			
			if (review.UserID != request.UserID)
				return Forbid();

			review.Rating = request.Rating;
			review.Comment = request.Comment;
			await _context.SaveChangesAsync();

			return Ok(new { message = "Review updated.", review });
		}

		
		[HttpDelete("{reviewId}")]
		public async Task<IActionResult> Delete(int reviewId)
		{
			var review = await _context.Reviews.FindAsync(reviewId);
			if (review == null) return NotFound("Review not found.");

			_context.Reviews.Remove(review);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Review deleted." });
		}

		
		[HttpGet("summary/{productId}")]
		public async Task<IActionResult> GetSummary(int productId)
		{
			var summary = await _context.Reviews
				.Where(r => r.ProductID == productId)
				.GroupBy(r => r.ProductID)
				.Select(g => new
				{
					ProductID = g.Key,
					AverageRating = Math.Round(g.Average(r => r.Rating), 2),
					TotalReviews = g.Count()
				})
				.FirstOrDefaultAsync();  

			return Ok(summary ?? new { ProductID = productId, AverageRating = 0.0, TotalReviews = 0 });

		}

	}
}
