using Microsoft.EntityFrameworkCore;
using VotingAPI.Data;
using VotingAPI.Models.DTOs.Admin;
using VotingAPI.Models.Entities;
using VotingAPI.Models.Enums;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Services
{
    public class AdminService : IAdminService
    {
        private readonly VotingDbContext dbContext;

        public AdminService(VotingDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<string> CreateOfficer(CreateOfficerDTO dto)
        {
            var officerExists = await dbContext.Users.AnyAsync(u => u.Email == dto.Email);

            if (officerExists)
                throw new InvalidOperationException("Election Officer already exists");

            var officer = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRole.ElectionOfficer,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Users.AddAsync(officer);
            await dbContext.SaveChangesAsync();

            return "Officer created successfully";
        }
    }
}