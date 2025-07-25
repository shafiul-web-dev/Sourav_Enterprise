﻿using Microsoft.EntityFrameworkCore;
using Sourav_Enterprise.Data;
using Sourav_Enterprise.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


namespace Sourav_Enterprise
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
			builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = "your_app_name",
			ValidAudience = "your_users",
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsAVerySecureAndLongSecretKeyForJWT!"))
		};
	});
			builder.Services.AddDbContext<ApplicationDbContext>(options =>
	               options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.Environment.IsDevelopment();
            
            app.UseSwagger();
            app.UseSwaggerUI();
            
            app.UseHttpsRedirection();

			app.UseAuthentication(); // 🔹 Add this BEFORE Authorization
			app.UseAuthorization();

			app.MapControllers();

            app.Run();
        }
    }
}
