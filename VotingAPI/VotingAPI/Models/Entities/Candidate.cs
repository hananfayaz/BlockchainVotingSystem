using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VotingAPI.Models.Entities
{
    public class Candidate
    {
        [Key]
        public Guid CandidateId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? PartyAffiliation { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? OnChainIndex { get; set; }          // index in smart contract (1, 2, 3...)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key
        public Guid ElectionId { get; set; }

        // Navigation properties
        [ForeignKey("ElectionId")]
        public Election Election { get; set; } = null!;

        public ICollection<VoteRecord> VoteRecords { get; set; } = new List<VoteRecord>();
    }
}