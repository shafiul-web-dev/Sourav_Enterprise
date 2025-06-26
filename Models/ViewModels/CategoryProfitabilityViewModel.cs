namespace Sourav_Enterprise.Models.ViewModels
{
	public class CategoryProfitabilityViewModel
	{
		public string CategoryName { get; set; }
		public decimal Revenue { get; set; }
		public decimal TotalExpense { get; set; }
		public decimal ProfitMargin { get; set; }
	}
}
