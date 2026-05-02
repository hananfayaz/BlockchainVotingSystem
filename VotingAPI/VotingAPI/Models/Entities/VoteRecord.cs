using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VotingAPI.Models.Entities
{
    public class VoteRecord
    {
        [Key]
        public Guid TransactionId { get; set; }

        [Required]
        [MaxLength(66)]
        public string TxHash { get; set; } = string.Empty;   // blockchain tx hash (0x + 64 chars)

        public long? BlockNumber { get; set; }

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;

        // Foreign keys
        public Guid ElectionId { get; set; }
        public Guid UserId { get; set; }
        public Guid CandidateId { get; set; }

        // Navigation properties
        [ForeignKey("ElectionId")]
        public Election Election { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("CandidateId")]
        public Candidate Candidate { get; set; } = null!;
    }
}