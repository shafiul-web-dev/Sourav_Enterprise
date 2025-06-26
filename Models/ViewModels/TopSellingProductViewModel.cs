namespace Sourav_Enterprise.Models.ViewModels
{
	public class TopSellingProductViewModel
	{
		public string Product_Name { get; set; }
		public int CategoryID { get; set; }
		public string Category_Name { get; set; }
		public decimal Price { get; set; }
		public int Total_Sold { get; set; }
		public decimal Total_Amount { get; set; }

	}
}
