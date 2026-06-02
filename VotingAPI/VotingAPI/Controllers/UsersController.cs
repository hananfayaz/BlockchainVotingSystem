using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VotingAPI.Models.DTOs.User;
using VotingAPI.Models.Enums;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService userService;

        public UsersController(IUserService userService)
        {
            this.userService = userService;
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await userService.GetAllUsers();
            return Ok(new { message = result });
        }

        [Authorize(Roles = nameof(UserRole.Voter))]
        [HttpPost("connect-wallet")]
        public async Task<IActionResult> ConnectWallet(string ethAddress)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User not logged-in");

            var result = await userService.ConnectWallet(Guid.Parse(userId), ethAddress);
            return Ok(new { message = result });
        }
    }
}