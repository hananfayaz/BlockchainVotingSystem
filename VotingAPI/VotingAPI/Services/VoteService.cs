using Microsoft.EntityFrameworkCore;
using VotingAPI.Data;
using VotingAPI.Helpers;
using VotingAPI.Models.DTOs.Vote;
using VotingAPI.Models.Entities;
using VotingAPI.Models.Enums;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Services
{
    public class VoteService : IVoteService
    {
        private readonly VotingDbContext dbContext;
        private readonly IEmailService emailService;

        public VoteService(VotingDbContext dbContext, IEmailService emailService)
        {
            this.dbContext = dbContext;
            this.emailService = emailService;
        }

        public async Task<string> SendVoteOtp(Guid userId, VotePrepareRequestDTO votePrepareRequestDTO)
        {
            await ValidateVoteRequest(userId, votePrepareRequestDTO);

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId) ?? throw new KeyNotFoundException("User not found");
            var otp = EmailHelper.GetOtp();
            var body = EmailHelper.GetBody(user.FullName, otp);

            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);

            await dbContext.SaveChangesAsync();
            await emailService.SendEmailAsync(user.Email, "Vote OTP Verification", body);

            return "OTP sent to your registered email.";
        }

        public async Task<VotePrepareResponseDTO> PrepareVote(Guid userId, VotePrepareRequestDTO votePrepareRequestDTO)
        {
            var election = await ValidateVoteRequest(userId, votePrepareRequestDTO);
            await ValidateVoteOtp(userId, votePrepareRequestDTO.Otp);
            var candidate = election.Candidates.First(c => c.CandidateId == votePrepareRequestDTO.CandidateId);

            return new VotePrepareResponseDTO
            {
                ContractAddress = election.ContractAddress!,
                CandidateIndex = candidate.OnChainIndex.Value
            };
        }

        private async Task<Election> ValidateVoteRequest(Guid userId, VotePrepareRequestDTO votePrepareRequestDTO)
        {
            var election = await dbContext.Elections.Include(e => e.Candidates).FirstOrDefaultAsync(e => e.ElectionId == votePrepareRequestDTO.ElectionId) ?? throw new KeyNotFoundException("Election not found");

            if (election.Status != ElectionStatus.Active)
                throw new InvalidOperationException("Election is not active");

            var candidate = election.Candidates.FirstOrDefault(c => c.CandidateId == votePrepareRequestDTO.CandidateId) ?? throw new KeyNotFoundException("Candidate not found");

            if (candidate.OnChainIndex == null)
                throw new InvalidOperationException("Candidate not mapped on chain");

            var voter = await dbContext.Voters.FirstOrDefaultAsync(v => v.UserId == userId && v.ElectionId == votePrepareRequestDTO.ElectionId) ?? throw new UnauthorizedAccessException("Not registered for election");

            var alreadyVoted = voter.HasVoted || await dbContext.VoteTransactions.AnyAsync(vt => vt.VoterId == userId && vt.ElectionId == votePrepareRequestDTO.ElectionId);

            if (alreadyVoted)
                throw new InvalidOperationException("Already voted");

            return election;
        }

        private async Task ValidateVoteOtp(Guid userId, string? otp)
        {
            if (string.IsNullOrWhiteSpace(otp))
                throw new UnauthorizedAccessException("Vote OTP is required.");

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId) ?? throw new KeyNotFoundException("User not found");

            if (user.OtpCode != otp)
                throw new UnauthorizedAccessException("Invalid vote OTP.");

            if (user.OtpExpiry == null || user.OtpExpiry < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Vote OTP has expired.");

            user.OtpCode = null;
            user.OtpExpiry = null;

            await dbContext.SaveChangesAsync();
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
