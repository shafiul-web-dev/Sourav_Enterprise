namespace Sourav_Enterprise.Data
{
	public class ResetPasswordRequest
	{
		
			public int UserId { get; set; }  // ✅ Ensure correct property name
			public string NewPassword { get; set; }
		
	}
}
