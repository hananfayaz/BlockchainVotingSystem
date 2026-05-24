using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VotingAPI.Models.DTOs.Election;
using VotingAPI.Models.Enums;
using VotingAPI.Services;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ElectionsController : ControllerBase
    {
        private readonly IElectionService electionService;

        public ElectionsController(IElectionService electionService)
        {
            this.electionService = electionService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllElections()
        {
            var elections = await electionService.GetAllElections();
            return Ok(new { elections });
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetElectionById(Guid id)
        {
            var election = await electionService.GetElectionById(id);
            return Ok(new { election });
        }

        [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.ElectionOfficer)}")]
        [HttpPost]
        public async Task<IActionResult> CreateElection([FromBody] CreateElectionDTO createElectionDTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new KeyNotFoundException("User not found");

            var result = await electionService.CreateElection(createElectionDTO, Guid.Parse(userId));
            return Ok(new { message = result });
        }

        [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.ElectionOfficer)}")]
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateElection(Guid id)
        {
            var result = await electionService.ActivateElection(id);
            return Ok(new { message = result });
        }

        [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.ElectionOfficer)}")]
        [HttpPut("{id}/close")]
        public async Task<IActionResult> CloseElection(Guid id)
        {
            var result = await electionService.CloseElection(id);
            return Ok(new { message = result });
        }

        [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.ElectionOfficer)}")]
        [HttpPost("{id}/candidates")]
        public async Task<IActionResult> AddCandidate(Guid id, [FromBody] AddCandidateDTO addCandidateDTO)
        {
            var result = await electionService.AddCandidate(id, addCandidateDTO);
            return Ok(new { message = result });
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpDelete("{id}/candidates/{candidateId}")]
        public async Task<IActionResult> RemoveCandidate(Guid id, Guid candidateId)
        {
            var result = await electionService.RemoveCandidate(id, candidateId);
            return Ok(new { message = result });
        }

        [Authorize]
        [HttpGet("{id}/candidates")]
        public async Task<IActionResult> GetCandidates(Guid id)
        {
            var result = await electionService.GetCandidates(id);
            return Ok(new { message = result });
        }
    }
}