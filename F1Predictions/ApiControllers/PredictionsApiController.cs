using F1Predictions.Extensions;
using F1Predictions.Models.DTOs;
using F1Predictions.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictions.ApiControllers
{
    [Route("api/predictions")]
    [ApiController]
    [Authorize]
    public class PredictionsApiController : ControllerBase
    {
        private readonly IPredictionsService _predictionsService;

        public PredictionsApiController(IPredictionsService predictionsService)
        {
            _predictionsService = predictionsService;
        }

        /// <summary>
        /// Gets all weekly predictions for a race with the user's existing picks.
        /// </summary>
        [HttpGet("race/{raceId}/league/{leagueId}")]
        public async Task<ActionResult<List<RacePredictionDto>>> GetRacePredictions(int raceId, int leagueId)
        {
            int userId = User.GetUserId();
            var result = await _predictionsService.GetRacePredictions(raceId, leagueId, userId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        /// <summary>
        /// Gets available drivers for a race (from the race's championship lineup).
        /// </summary>
        [HttpGet("drivers/{raceId}")]
        public async Task<ActionResult<List<DriverOptionDto>>> GetDriversForRace(int raceId)
        {
            var result = await _predictionsService.GetDriversForRace(raceId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        /// <summary>
        /// Gets available teams for a race (from the race's championship lineup).
        /// </summary>
        [HttpGet("teams/{raceId}")]
        public async Task<ActionResult<List<TeamOptionDto>>> GetTeamsForRace(int raceId)
        {
            var result = await _predictionsService.GetTeamsForRace(raceId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        /// <summary>
        /// Gets available races for the user's league championship.
        /// </summary>
        [HttpGet("races/{leagueId}")]
        public async Task<ActionResult<List<RaceOptionDto>>> GetRacesForLeague(int leagueId)
        {
            var result = await _predictionsService.GetRacesForLeague(leagueId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        /// <summary>
        /// Submits or updates a user prediction.
        /// </summary>
        [HttpPost("submit")]
        public async Task<ActionResult> SubmitPrediction([FromBody] SubmitPredictionRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int userId = User.GetUserId();
            var result = await _predictionsService.SubmitPrediction(dto, userId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        /// <summary>
        /// Gets a specific member's predictions for a race (read-only view).
        /// </summary>
        [HttpGet("member/{targetUserId}/race/{raceId}/league/{leagueId}")]
        public async Task<ActionResult<List<RacePredictionDto>>> GetMemberPredictions(int targetUserId, int raceId, int leagueId)
        {
            int requestingUserId = User.GetUserId();
            var result = await _predictionsService.GetMemberPredictions(raceId, leagueId, targetUserId, requestingUserId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        // ---- League Hub Endpoints ----

        /// <summary>
        /// Gets the current/upcoming race for a league. Use raceId query param to navigate to a specific race.
        /// </summary>
        [HttpGet("current-race/{leagueId}")]
        public async Task<ActionResult<CurrentRaceDto>> GetCurrentRace(int leagueId, [FromQuery] int? raceId = null)
        {
            var result = await _predictionsService.GetCurrentRace(leagueId, raceId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        /// <summary>
        /// Gets the league hub summary: points, member standings, and voting status.
        /// </summary>
        [HttpGet("league-summary/{leagueId}/{raceId}")]
        public async Task<ActionResult<LeagueSummaryDto>> GetLeagueSummary(int leagueId, int raceId)
        {
            int userId = User.GetUserId();
            var result = await _predictionsService.GetLeagueSummary(leagueId, raceId, userId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        /// <summary>
        /// Gets a member's predictions with points, votes, and visibility enforcement.
        /// </summary>
        [HttpGet("member-predictions/{targetUserId}/{raceId}/{leagueId}")]
        public async Task<ActionResult<List<MemberPredictionDto>>> GetMemberPredictionsEnhanced(int targetUserId, int raceId, int leagueId)
        {
            int requestingUserId = User.GetUserId();
            var result = await _predictionsService.GetMemberPredictionsEnhanced(raceId, leagueId, targetUserId, requestingUserId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }
    }
}
