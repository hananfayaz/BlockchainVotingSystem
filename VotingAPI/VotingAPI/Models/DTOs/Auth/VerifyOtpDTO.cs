using System.ComponentModel.DataAnnotations;

namespace VotingAPI.Models.DTOs.Auth
{
    public class VerifyOtpDTO
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Otp { get; set; } = string.Empty;
    }
}