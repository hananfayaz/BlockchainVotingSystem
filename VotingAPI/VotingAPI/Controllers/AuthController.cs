using Microsoft.AspNetCore.Mvc;
using VotingAPI.Models.DTOs.Auth;
using VotingAPI.Services.Interfaces;

namespace VotingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;

        public AuthController(IAuthService authService)
        {
            this.authService = authService;
        }

        [HttpPost("Login")]
        public IActionResult Login()
        {
            return Ok("Login successful");
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO registerRequest)
        {
            if (registerRequest != null)
            {
                var result = await authService.Register(registerRequest);
                return Ok(result);
            }
            return BadRequest("User can't be null");
        }
    }
}