using Microsoft.EntityFrameworkCore;
using VotingAPI.Models.Entities;
using VotingAPI.Models.Enums;

namespace VotingAPI.Data.Seed
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(VotingDbContext dbContext)
        {
            // Check if admin already exists
            var adminExists = await dbContext.Users.AnyAsync(u => u.Role == UserRole.Admin);

            if (adminExists)
                return;

            var admin = new User
            {
                FullName = "System Admin",
                Email = "chipichapa422@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin@123"),
                Role = UserRole.Admin,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Users.AddAsync(admin);
            await dbContext.SaveChangesAsync();
        }
    }
}