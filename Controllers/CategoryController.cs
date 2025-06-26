using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Data;
using Sourav_Enterprise.DTO.CategoryDto;
using Sourav_Enterprise.Models;

namespace Sourav_Enterprise.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }
		[HttpGet("GetAllCategories")]
		public async Task<IActionResult> GetAllCategories()
		{
			var categories = await _context.Categories
				.FromSqlRaw("EXEC sp_GetAllCategories")
				.ToListAsync();
			return Ok(categories);
		}

		[HttpGet("GetCategoryById/{id}")]
		public async Task<IActionResult> GetCategoryById(int id)
		{
			var result = await _context.Categories
				.FromSqlRaw("EXEC sp_GetCategoryById @CategoryID = {0}", id)
				.ToListAsync();

			var category = result.FirstOrDefault();

			if (category == null)
				return NotFound($"No category found with ID = {id}");
			return Ok(category);
		}

		[HttpPost("CreateCategory")]
		public async Task<IActionResult> CreateCategory([FromBody] Category category)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			await _context.Database.ExecuteSqlRawAsync(
				"EXEC sp_AddCategory @Name = {0}, @Description = {1}",
				category.Name,
				category.Description
			);
			return Ok("Category created successfully using stored procedure.");
		}

		[HttpPut("UpdateCategory/{id}")]
		public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
		{
			if (id != category.CategoryID)
				return BadRequest("Category ID mismatch.");

			var exists = await _context.Categories.AnyAsync(c => c.CategoryID == id);
			if (!exists)
				return NotFound($"No category found with ID = {id}");

			await _context.Database.ExecuteSqlRawAsync(
				"EXEC sp_UpdateCategory @CategoryID = {0}, @Name = {1}, @Description = {2}",
				category.CategoryID,
				category.Name,
				category.Description
			);
			return Ok("Category updated successfully using stored procedure.");
		}

		[HttpDelete("DeleteCategory/{id}")]
		public async Task<IActionResult> DeleteCategory(int id)
		{
			var exists = await _context.Categories.AnyAsync(c => c.CategoryID == id);
			if (!exists)
				return NotFound($"No category found with ID = {id}");

			await _context.Database.ExecuteSqlRawAsync(
				"EXEC sp_DeleteCategory @CategoryID = {0}",
				id
			);
			return Ok("Category deleted successfully using stored procedure.");
		}

		[HttpGet("analytics/best-selling-categories")]
		public async Task<IActionResult> GetBestSellingCategories()
		{
			var result = await _context.BestSellingCategoryViewModel
				.FromSqlRaw("EXEC sp_GetBestSellingCategories")
				.ToListAsync();
			return Ok(result);
		}

		[HttpGet("analytics/category-profitability")]
		public async Task<IActionResult> GetCategoryProfitability()
		{
			var result = await _context.CategoryProfitabilityViewModel
				.FromSqlRaw("EXEC sp_GetCategoryProfitability")
				.ToListAsync();
			return Ok(result);
		}

		[HttpGet("analytics/category-demand-forecast")]
		public async Task<IActionResult> GetCategoryDemandForecast()
		{
			var result = await _context.CategoryDemandForecastViewModel
				.FromSqlRaw("EXEC sp_GetCategoryDemandForecast")
				.ToListAsync();
			return Ok(result);
		}

		[HttpGet("analytics/average-order-value")]
		public async Task<IActionResult> GetAverageOrderValueByCategory()
		{
			var result = await _context.AverageOrderValueViewModel
				.FromSqlRaw("EXEC sp_GetAverageOrderValueByCategory")
				.ToListAsync();
			return Ok(result);
		}

		[HttpGet("analytics/popular-category-per-customer")]
		public async Task<IActionResult> GetMostPopularCategoryPerCustomer()
		{
			var result = await _context.CustomerCategoryPreferenceViewModel
				.FromSqlRaw("EXEC sp_GetMostPopularCategoryPerCustomer")
				.ToListAsync();
			return Ok(result);
		}

		[HttpGet("analytics/category-inventory-optimization")]
		public async Task<IActionResult> GetCategoryInventoryOptimization()
		{
			var result = await _context.CategoryInventoryViewModel
				.FromSqlRaw("EXEC sp_GetCategoryInventoryOptimization")
				.ToListAsync();

			return Ok(result);
		}

		[HttpGet("analytics/top-selling-products")]
		public async Task<IActionResult> GetTopSellingProducts()
		{
			var result = await _context.TopSellingProductViewModel
				.FromSqlRaw("EXEC sp_GetTopSellingProducts")
				.ToListAsync();
			return Ok(result);
		}

		[HttpGet("analytics/product-count-per-category")]
		public async Task<IActionResult> GetProductCountPerCategory()
		{
			var result = await _context.ProductCountPerCategoryViewModel
				.FromSqlRaw("EXEC sp_GetProductCountPerCategory")
				.ToListAsync();
			return Ok(result);
		}

		[HttpGet("analytics/category-revenue-share")]
		public async Task<IActionResult> GetCategoryRevenueShare()
		{
			var result = await _context.CategoryRevenueShareViewModel
				.FromSqlRaw("EXEC sp_GetCategoryRevenueShare")
				.ToListAsync();
			return Ok(result);
		}
		[HttpPost("add")]
		public async Task<IActionResult> AddCategory([FromBody] CategoryCreateRequest request)
		{
			try
			{
				var nameParam = new SqlParameter("@Name", request.Name);
				var descParam = new SqlParameter("@Description", request.Description);

				await _context.Database.ExecuteSqlRawAsync("EXEC sp_AddCategoryButCheckDuplicate @Name, @Description", nameParam, descParam);

				return Ok(new { message = "Category added successfully." });
			}
			catch (SqlException ex)
			{
				if (ex.Message.Contains("already exists"))
					return Conflict(new { error = ex.Message });

				return StatusCode(500, new { error = ex.Message });
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteCategorySafely(int id)
		{
			try
			{
				var idParam = new SqlParameter("@CategoryID", id);

				await _context.Database.ExecuteSqlRawAsync("EXEC sp_DeleteCategorySafely @CategoryID", idParam);

				return Ok(new { message = "Category deleted successfully." });
			}
			catch (SqlException ex)
			{
				if (ex.Message.Contains("in use"))
					return Conflict(new { error = ex.Message });

				return StatusCode(500, new { error = ex.Message });
			}
		}
		[HttpPost("assign-featured-products")]
		public async Task<IActionResult> AssignFeaturedProducts()
		{
			try
			{
				await _context.Database.ExecuteSqlRawAsync("EXEC sp_AssignFeaturedProducts");
				return Ok(new { message = "Featured products have been assigned." });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = ex.Message });
			}
		}

		[HttpGet("get-all")]
		public async Task<IActionResult> GetAllCategories(
         [FromQuery] string search = null,
       	[FromQuery] string sortBy = "Name",
	     [FromQuery] bool sortDesc = false,
	    [FromQuery] int page = 1,
	    [FromQuery] int pageSize = 10,
	    [FromQuery] bool? isActive = null)
		{
			var searchParam = new SqlParameter("@Search", search ?? (object)DBNull.Value);
			var sortByParam = new SqlParameter("@SortBy", sortBy);
			var sortDescParam = new SqlParameter("@SortDesc", sortDesc);
			var pageParam = new SqlParameter("@Page", page);
			var pageSizeParam = new SqlParameter("@PageSize", pageSize);
			var isActiveParam = new SqlParameter("@IsActive", isActive ?? (object)DBNull.Value);

			var result = await _context.CategoryListDto
				.FromSqlRaw("EXEC sp_GetAllCategoriesAdvanced @Search, @SortBy, @SortDesc, @Page, @PageSize, @IsActive",
					searchParam, sortByParam, sortDescParam, pageParam, pageSizeParam, isActiveParam)
				.ToListAsync();

			return Ok(result);
		}
	}  
}
