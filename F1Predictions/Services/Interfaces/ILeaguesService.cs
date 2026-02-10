using F1Predictions.Models;
using F1Predictions.Models.DTOs;

namespace F1Predictions.Services.Interfaces
{
    public interface ILeaguesService
    {
        /// <summary>
        /// Creates a new league with the specified owner.
        /// </summary>
        Task<ServiceResult<LeagueDto>> CreateLeague(CreateLeagueDto dto, int ownerUserId);

        /// <summary>
        /// Gets all leagues that a user is a member of.
        /// </summary>
        Task<ServiceResult<List<LeagueDto>>> GetUserLeagues(int userId);

        /// <summary>
        /// Joins a user to a league using an invite code.
        /// </summary>
        Task<ServiceResult<bool>> JoinLeagueByCodeAsync(string inviteCode, int userId);

        Task<ServiceResult<List<LeagueMemberDto>>> GetLeagueMembers(int userId, int leagueId);
    }
}
