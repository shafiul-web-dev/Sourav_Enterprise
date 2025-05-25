using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Data;

namespace Sourav_Enterprise.Models
{
	public class User
	{
		[Key]
		public int UserID { get; set; }

		[Required]
		[MaxLength(100)]
		public string Name { get; set; }

		[Required]
		[EmailAddress]
		[MaxLength(100)]
		public string Email { get; set; }

		[Required]
		[JsonIgnore]
		[MaxLength(255)]
		public string PasswordHash { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public ICollection<Order> Orders { get; set; }

	}


}