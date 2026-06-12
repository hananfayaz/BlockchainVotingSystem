using VotingAPI.Models.DTOs.Election;

namespace VotingAPI.Services.Interfaces
{
    public interface IElectionService
    {
        Task<List<ElectionResponseDTO>> GetAllElections(Guid? currentUserId = null);
        Task<ElectionResponseDTO> GetElectionById(Guid electionId);
        Task<string> CreateElection(CreateElectionDTO createElectionDTO, Guid createdBy);
        Task<string> ActivateElection(Guid electionId);
        Task<string> CloseElection(Guid electionId);
        Task<string> AddCandidate(Guid electionId, AddCandidateDTO addCandidateDTO);
        Task<string> RemoveCandidate(Guid electionId, Guid candidateId);
        Task<List<CandidateResponseDTO>> GetCandidates(Guid electionId);
    }
}