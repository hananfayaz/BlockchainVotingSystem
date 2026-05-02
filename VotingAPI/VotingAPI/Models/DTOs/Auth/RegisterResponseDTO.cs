namespace VotingAPI.Models.DTOs.Auth
{
    public class RegisterResponseDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}