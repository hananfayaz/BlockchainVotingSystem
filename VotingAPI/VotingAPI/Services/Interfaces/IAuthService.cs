using VotingAPI.Models.DTOs.Auth;

namespace VotingAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> Register(RegisterRequestDTO registerRequestDTO);
        Task<string> VerifyOtp(VerifyOtpDTO verifyOtpDTO);
        Task<string> ResendOtp(ResendOtpDTO resendOtpDTO);
        Task<string> Login(LoginRequestDTO loginRequestDTO);
    }
}