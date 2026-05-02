using Microsoft.EntityFrameworkCore;
using VotingAPI.Data;
using VotingAPI.Models.DTOs.Auth;
using VotingAPI.Models.Entities;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly VotingDbContext dbContext;

        public AuthService(VotingDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            throw new NotImplementedException();
        }

        public async Task<RegisterResponseDTO> Register(RegisterRequestDTO registerRequestDTO)
        {
            var existingUser = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == registerRequestDTO.Email);

            if (existingUser != null)
            {
                throw new Exception("User with this email already exists.");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerRequestDTO.Password);

            User newUser = new()
            {
                FullName = registerRequestDTO.FullName,
                Email = registerRequestDTO.Email,
                PasswordHash = hashedPassword,
                Role = registerRequestDTO.Role,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Users.AddAsync(newUser);
            await dbContext.SaveChangesAsync();

            var response = new RegisterResponseDTO()
            {
                FullName = newUser.FullName,
                Email = newUser.Email,
                Role = newUser.Role
            };

            return response;
        }
    }
}