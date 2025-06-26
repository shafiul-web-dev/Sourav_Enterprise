namespace Sourav_Enterprise.DTO.CategoryDto
{
	public class CategoryListDto
	{
		public int CategoryID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public DateTime CreatedAt { get; set; }
		public bool IsActive { get; set; }
	}
}
