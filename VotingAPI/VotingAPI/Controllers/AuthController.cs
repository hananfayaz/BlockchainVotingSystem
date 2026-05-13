using Microsoft.AspNetCore.Authorization;
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

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterRequestDTO registerRequestDTO)
        {
            try
            {
                var result = await authService.Register(registerRequestDTO);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDTO verifyOtpDTO)
        {
            try
            {
                var result = await authService.VerifyOtp(verifyOtpDTO);
                return Ok(new { message = result });
            }
            catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("ResendOtp")]
        public async Task<IActionResult> ResendOtp(ResendOtpDTO resendOtpDTO)
        {
            try
            {
                var result = await authService.ResendOtp(resendOtpDTO);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequestDTO loginRequestDTO)
        {
            try
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
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var token = Request.Cookies["access_token"];
                if (token != null)
                {
                    var revokeToken = token.Contains("RevokeToken");
                    await authService.Logout(revokeToken);
                }
                Response.Cookies.Delete("access_token", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Lax,
                });
                return Ok(new { message = "Logout successful" , token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}