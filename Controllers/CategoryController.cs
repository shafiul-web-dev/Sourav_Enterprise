using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Data;
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



	}  
}
