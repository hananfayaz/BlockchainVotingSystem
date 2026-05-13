using System.ComponentModel.DataAnnotations;

namespace VotingAPI.Models.DTOs.Elections
{
    public class CreateElectionDTO
    {
        [Required(ErrorMessage = "Election title is required.")]
        [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [MaxLength(42, ErrorMessage = "Contract address cannot exceed 42 characters.")]
        public string? ContractAddress { get; set; }

        [Required(ErrorMessage = "Start time is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public DateTime EndTime { get; set; }
    }
}