using VotingAPI.Models.DTOs.Auth;

namespace VotingAPI.Services.Interfaces
{
    public interface IAuthService
    {
        public Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO);
        public Task<RegisterResponseDTO> Register(RegisterRequestDTO registerRequestDTO);
    }
}