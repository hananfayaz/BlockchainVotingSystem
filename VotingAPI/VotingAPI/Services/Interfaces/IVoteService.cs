using VotingAPI.Models.DTOs.Vote;

namespace VotingAPI.Services.Interfaces
{
    public interface IVoteService
    {
        Task<VotePrepareResponseDTO> PrepareVote(Guid userId, VotePrepareRequestDTO votePrepareRequestDTO);

        Task ConfirmVote(Guid userId, ConfirmVoteDTO confirmVoteDTO);

        //Task<string> CastVote(Guid userId, VoteRequestDTO voteRequestDTO);
    }
}