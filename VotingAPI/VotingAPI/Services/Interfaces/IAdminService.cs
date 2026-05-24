using VotingAPI.Models.DTOs.Admin;

namespace VotingAPI.Services.Interfaces
{
    public interface IAdminService
    {
        Task<string> CreateOfficer(CreateOfficerDTO dto);
    }
}