using System.ComponentModel.DataAnnotations;

namespace VotingAPI.Models.Entities
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(42)]
        public string? EthAddress { get; set; } // Ethereum wallet address

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Voter";

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public ICollection<Election> CreatedElections { get; set; } = [];
        public ICollection<Voter> Voters { get; set; } = [];
        public ICollection<VoteRecord> VoteRecords { get; set; } = [];
    }
}