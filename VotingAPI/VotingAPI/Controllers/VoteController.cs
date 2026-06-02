using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VotingAPI.Models.DTOs.Vote;
using VotingAPI.Models.Enums;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = nameof(UserRole.Voter))]
    public class VoteController : ControllerBase
    {
        private readonly IVoteService voteService;

        public VoteController(IVoteService voteService)
        {
            this.voteService = voteService;
        }

        [HttpPost("prepare")]
        public async Task<IActionResult> PrepareVote([FromBody] VotePrepareRequestDTO votePrepareRequestDTO)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Not logged in");

            var result = await voteService.PrepareVote(Guid.Parse(userId!), votePrepareRequestDTO);
            return Ok(new { message = result });
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmVote([FromBody] ConfirmVoteDTO confirmVoteDTO)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Not logged in");

            await voteService.ConfirmVote(Guid.Parse(userId!), confirmVoteDTO);
            return Ok(new { message = "Vote stored successfully" });
        }
    }
}