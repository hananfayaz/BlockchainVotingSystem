using VotingAPI.Models.DTOs.User;

namespace VotingAPI.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<UserResponseDTO>> GetAllUsers();
        Task<string> ConnectWallet(Guid userId, string ethAddress);
    }
}