using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VotingAPI.Models.DTOs.Voter;
using VotingAPI.Models.Enums;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotersController : ControllerBase
    {
        private readonly IVoterService voterService;

        public VotersController(IVoterService voterService)
        {
            this.voterService = voterService;
        }

        [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.ElectionOfficer)}")]
        [HttpPost("Register")]
        public async Task<IActionResult> Register(Guid ElectionId, Guid UserId)
        {
            var result = await voterService.RegisterVoter(ElectionId, UserId);
            return Ok(new { message = result });
        }

        [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.ElectionOfficer)}")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVoters(Guid id)
        {
            var result = await voterService.GetElectionVoters(id);
            return Ok(result);
        }
    }
}