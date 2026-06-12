namespace VotingAPI.Models.DTOs.Vote
{
    public class VotePrepareRequestDTO
    {
        public Guid ElectionId { get; set; }
        public Guid CandidateId { get; set; }
        public string? Otp { get; set; }
    }
}
