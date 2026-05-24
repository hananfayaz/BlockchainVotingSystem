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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO registerRequestDTO)
        {
            var result = await authService.Register(registerRequestDTO);
            return Ok(new { message = result });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginRequestDTO)
        {
            var token = await authService.Login(loginRequestDTO);

            Response.Cookies.Append(key: "access_token", value: token, options: new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(1)
            });

            return Ok(new { message = "Login successful" });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDTO verifyOtpDTO)
        {
            var result = await authService.VerifyOtp(verifyOtpDTO);
            return Ok(new { message = result });
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDTO resendOtpDTO)
        {
            var result = await authService.ResendOtp(resendOtpDTO);
            return Ok(new { message = result });
        }
    }
}