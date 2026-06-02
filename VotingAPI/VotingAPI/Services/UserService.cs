using Microsoft.EntityFrameworkCore;
using Nethereum.Util;
using VotingAPI.Data;
using VotingAPI.Models.DTOs.User;
using VotingAPI.Models.Enums;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Services
{
    public class UserService : IUserService
    {
        private readonly VotingDbContext dbContext;

        public UserService(VotingDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<string> ConnectWallet(Guid userId, string ethAddress)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId) ?? throw new KeyNotFoundException("User not found");

            var wallectConnected = await dbContext.Users.AnyAsync(u => u.EthAddress == ethAddress && u.UserId != userId);

            if (wallectConnected)
                throw new InvalidOperationException("Wallet already connected");

            if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(ethAddress))
                throw new ArgumentException("Invalid Ethereum address");

            user.EthAddress = ethAddress;
            await dbContext.SaveChangesAsync();

            return "Wallet connected successfully";
        }

        public async Task<List<UserResponseDTO>> GetAllUsers()
        {
            // Fetches only users (voters), not admins & election officers
            var usersRoles = await dbContext.Users.AsNoTracking().Where(u => u.Role != UserRole.Admin && u.Role != UserRole.ElectionOfficer).Select(u => new UserResponseDTO
            {
                FullName = u.FullName,
                Role = u.Role
            }).ToListAsync() ?? throw new KeyNotFoundException("No user found");

            return usersRoles;
        }
    }
}