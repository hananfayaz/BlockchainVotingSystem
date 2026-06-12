using VotingAPI.Models.Enums;

namespace VotingAPI.Models.DTOs.User
{
    public class UserResponseDTO
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? EthAddress { get; set; }
        public UserRole Role { get; set; }
    }
}
