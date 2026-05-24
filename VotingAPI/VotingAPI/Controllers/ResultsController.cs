using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VotingAPI.Models.Enums;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly IResultService resultService;

        public ResultsController(IResultService resultService)
        {
            this.resultService = resultService;
        }

        [Authorize]
        [HttpGet("{electionId}")]
        public async Task<IActionResult> GetResults(Guid electionId)
        {
            var results = await resultService.GetElectionResults(electionId);
            return Ok(results);
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpGet("{electionId}/audit")]
        public async Task<IActionResult> GetAudit(Guid electionId)
        {
            var result = await resultService.GetElectionAudit(electionId);
            return Ok(new { message = result });
        }
    }
}