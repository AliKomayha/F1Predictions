using F1Predictions.Models;
using F1Predictions.Models.DTOs;

namespace F1Predictions.Services.Interfaces
{
    public interface IPredictionsService
    {
        /// <summary>
        /// Gets all weekly predictions for a race with the user's existing picks.
        /// </summary>
        Task<ServiceResult<List<RacePredictionDto>>> GetRacePredictions(int raceId, int leagueId, int userId);

        /// <summary>
        /// Gets available drivers for a race (from DriverTeams in the race's championship).
        /// </summary>
        Task<ServiceResult<List<DriverOptionDto>>> GetDriversForRace(int raceId);

        /// <summary>
        /// Gets available   teams for a race (from DriverTeams in the race's championship).
        /// </summary>
        Task<ServiceResult<List<TeamOptionDto>>> GetTeamsForRace(int raceId);

        /// <summary>
        /// Gets available races for the user's league championship.
        /// </summary>
        Task<ServiceResult<List<RaceOptionDto>>> GetRacesForLeague(int leagueId);

        /// <summary>
        /// Submits or updates a user prediction.
        /// </summary>
        Task<ServiceResult<bool>> SubmitPrediction(SubmitPredictionRequest dto, int userId);

        /// <summary>
        /// Gets a specific member's predictions for a race (read-only view).
        /// </summary>
        Task<ServiceResult<List<RacePredictionDto>>> GetMemberPredictions(int raceId, int leagueId, int targetUserId, int requestingUserId);
    }
}
