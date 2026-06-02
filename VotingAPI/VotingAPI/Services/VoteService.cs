using Microsoft.EntityFrameworkCore;
using VotingAPI.Data;
using VotingAPI.Models.DTOs.Vote;
using VotingAPI.Models.Entities;
using VotingAPI.Models.Enums;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Services
{
    public class VoteService : IVoteService
    {
        private readonly VotingDbContext dbContext;

        public VoteService(VotingDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<VotePrepareResponseDTO> PrepareVote(Guid userId, VotePrepareRequestDTO votePrepareRequestDTO)
        {
            var election = await dbContext.Elections.Include(e => e.Candidates).FirstOrDefaultAsync(e => e.ElectionId == votePrepareRequestDTO.ElectionId) ?? throw new KeyNotFoundException("Election not found");

            if (election.Status != ElectionStatus.Active)
                throw new InvalidOperationException("Election is not active");

            var candidate = election.Candidates.FirstOrDefault(c => c.CandidateId == votePrepareRequestDTO.CandidateId) ?? throw new KeyNotFoundException("Candidate not found");

            if (candidate.OnChainIndex == null)
                throw new InvalidOperationException("Candidate not mapped on chain");

            var voter = await dbContext.Voters.FirstOrDefaultAsync(v => v.UserId == userId && v.ElectionId == votePrepareRequestDTO.ElectionId) ?? throw new UnauthorizedAccessException("Not registered for election");

            var alreadyVoted = await dbContext.VoteTransactions.AnyAsync(vt => vt.VoterId == voter.VoterId);

            if (alreadyVoted)
                throw new InvalidOperationException("Already voted");

            return new VotePrepareResponseDTO
            {
                ContractAddress = election.ContractAddress!,
                CandidateIndex = candidate.OnChainIndex.Value
            };
        }

        public async Task ConfirmVote(Guid userId, ConfirmVoteDTO confirmVoteDTO)
        {
            var voter = await dbContext.Voters.FirstOrDefaultAsync(v => v.UserId == userId && v.ElectionId == confirmVoteDTO.ElectionId) ?? throw new KeyNotFoundException("Voter not found");

            var voterExists = await dbContext.VoteTransactions.AnyAsync(v => v.TxHash == confirmVoteDTO.TxHash && v.ElectionId == confirmVoteDTO.ElectionId &&
            v.VoterId == userId);

            if (voterExists)
                throw new InvalidOperationException("Vote already recorded");

            var candidateExists = await dbContext.Candidates.AnyAsync(c => c.CandidateId == confirmVoteDTO.CandidateId && c.ElectionId == confirmVoteDTO.ElectionId);

            if (!candidateExists)
                throw new KeyNotFoundException("Candidate not found");

            var voteTransaction = new VoteTransaction
            {
                TxHash = confirmVoteDTO.TxHash,
                BlockNumber = confirmVoteDTO.BlockNumber,
                VotedAt = DateTime.UtcNow,
                ElectionId = confirmVoteDTO.ElectionId,
                CandidateId = confirmVoteDTO.CandidateId,
                VoterId = userId
            };

            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                voter.HasVoted = true;
                await dbContext.VoteTransactions.AddAsync(voteTransaction);
                await dbContext.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            
        }
    }
}