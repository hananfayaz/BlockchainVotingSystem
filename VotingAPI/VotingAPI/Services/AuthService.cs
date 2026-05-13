using Microsoft.EntityFrameworkCore;
using VotingAPI.Data;
using VotingAPI.Models.DTOs.Auth;
using VotingAPI.Models.Entities;
using VotingAPI.Models.Enums;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly VotingDbContext dbContext;
        private readonly IJwtService jwtService;
        private readonly IEmailService emailService;

        public AuthService(VotingDbContext dbContext, IJwtService jwtService, IEmailService emailService)
        {
            this.dbContext = dbContext;
            this.jwtService = jwtService;
            this.emailService = emailService;
        }

        public async Task<string> Register(RegisterRequestDTO registerRequest)
        {
            var existingUser = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == registerRequest.Email);

            if (existingUser != null)
                throw new Exception("User with this email already exists.");

            if (registerRequest.Role != UserRole.Candidate && registerRequest.Role != UserRole.Voter)
                throw new Exception("Invalid role. Only Candidate and Voter roles are allowed for registration.");

            var otp = new Random().Next(100000, 999999).ToString();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);

            User user = new()
            {
                FullName = registerRequest.FullName,
                Email = registerRequest.Email,
                PasswordHash = hashedPassword,
                // TODO: EthAddress
                Role = registerRequest.Role,
                IsVerified = false,
                OtpCode = otp,
                OtpExpiry = DateTime.UtcNow.AddMinutes(10),
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            await emailService.SendEmailAsync(toEmail: user.Email, subject: "OTP Verification", body: $"Your OTP is: {otp}. It expires in 10 minutes.");

            return "Registration successful. OTP sent to email.";
        }

        public async Task<string> VerifyOtp(VerifyOtpDTO verifyOtpDTO)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == verifyOtpDTO.Email) ?? throw new Exception("User not found.");

            if (user.IsVerified)
                throw new Exception("User already verified.");

            if (user.OtpCode != verifyOtpDTO.Otp)
                throw new Exception("Invalid OTP.");

            if (user.OtpExpiry < DateTime.UtcNow)
                throw new Exception("OTP has expired.");

            user.IsVerified = true;
            user.OtpCode = null;
            user.OtpExpiry = null;

            await dbContext.SaveChangesAsync();
            return "Account verified successfully.";
        }

        public async Task<string> ResendOtp(ResendOtpDTO resendOtpDTO)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == resendOtpDTO.Email) ?? throw new Exception("User not found.");

            if (user.IsVerified)
                throw new Exception("User already verified.");

            if (user.OtpExpiry > DateTime.UtcNow)
                throw new Exception("Current OTP is still valid. Please wait before requesting a new one.");

            var otp = new Random().Next(100000, 999999).ToString();

            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);

            await dbContext.SaveChangesAsync();

            await emailService.SendEmailAsync(toEmail: user.Email, subject: "OTP Verification", body: $"Your new OTP is: {otp}. It expires in 10 minutes.");

            return "OTP resent successfully.";
        }

        public async Task<string> Login(LoginRequestDTO loginRequestDTO)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == loginRequestDTO.Email) ?? throw new Exception("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(loginRequestDTO.Password, user.PasswordHash))
                throw new Exception("Invalid email or password.");

            if (user.RevokeToken)
                throw new Exception("Token has been revoked. Please log in again.");

            if (!user.IsVerified)
                throw new Exception("Account not verified. Please verify your account before logging in.");

            if (loginRequestDTO.Role != user.Role)
                throw new Exception("Invalid role.");

            var token = jwtService.GenerateToken(user);

            return token;
        }

        public async Task<string> Logout(bool revokeToken)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.RevokeToken == revokeToken) ?? throw new Exception("User not found.");

            user.RevokeToken = true;
            await dbContext.SaveChangesAsync();
            return "Logged out successfully.";
        }
    }
}