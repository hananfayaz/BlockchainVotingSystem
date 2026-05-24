using VotingAPI.Models.Enums;

namespace VotingAPI.Models.DTOs.User
{
    public class UserResponseDTO
    {
        public string FullName { get; set; } = null!;
        public UserRole Role { get; set; }
    }
}