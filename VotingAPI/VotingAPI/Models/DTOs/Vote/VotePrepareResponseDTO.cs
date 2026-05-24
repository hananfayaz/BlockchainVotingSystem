namespace VotingAPI.Models.DTOs.Vote
{
    public class VotePrepareResponseDTO
    {
        public string ContractAddress { get; set; } = string.Empty;

        public int CandidateIndex { get; set; }
    }
}