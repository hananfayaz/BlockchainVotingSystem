using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VotingAPI.Models.Entities
{
    public class Election
    {
        [Key]
        public Guid ElectionId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(42)]
        public string? ContractAddress { get; set; }    // filled after smart contract deploy

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Draft";  // Draft | Active | Closed

        public DateTime CreatedAt { get; set; }

        // Foreign key
        public Guid CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("CreatedBy")]
        public User Creator { get; set; } = null!;

        public ICollection<Candidate> Candidates { get; set; } = [];
        public ICollection<Voter> Voters { get; set; } = [];
        public ICollection<VoteRecord> VoteRecords { get; set; } = [];
    }
}