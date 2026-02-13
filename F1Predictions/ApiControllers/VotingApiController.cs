using F1Predictions.Models.DTOs;
using F1Predictions.Services.Interfaces;
using F1Predictions.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictions.ApiControllers
{
    [Route("api/voting")]
    [ApiController]
    public class VotingApiController : ControllerBase
    {
        private readonly IVotingService _votingService;

        public VotingApiController(IVotingService votingService)
        {
            _votingService = votingService;
        }

        /// <summary>
        /// Gets voting status for a race/league.
        /// </summary>
        [HttpGet("status/{raceId}/{leagueId}")]
        [Authorize]
        public async Task<ActionResult<VotingStatusDto>> GetVotingStatus(int raceId, int leagueId)
        {
            var result = await _votingService.GetVotingStatus(raceId, leagueId);
            return Ok(result.Data);
        }

        /// <summary>
        /// Gets votable predictions for a race in a league.
        /// </summary>
        [HttpGet("predictions/{raceId}/{leagueId}")]
        [Authorize]
        public async Task<ActionResult<List<VotablePredictionDto>>> GetVotablePredictions(int raceId, int leagueId)
        {
            int userId = User.GetUserId();
            var result = await _votingService.GetVotablePredictions(raceId, leagueId, userId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        /// <summary>
        /// Casts a vote on a prediction.
        /// </summary>
        [HttpPost("cast")]
        [Authorize]
        public async Task<ActionResult<VoteResultDto>> CastVote([FromBody] CastVoteRequest request)
        {
            int voterId = User.GetUserId();
            var result = await _votingService.CastVote(request, voterId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        // ---- Admin endpoints (no auth for now) ----

        /// <summary>
        /// Admin decides on a tied prediction.
        /// </summary>
        [HttpPost("admin-decide")]
        public async Task<ActionResult> AdminDecide([FromBody] AdminDecideRequest request)
        {
            // For now, admin ID is passed or defaults to 0
            var result = await _votingService.AdminDecide(request, 0);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Data });
        }

        /// <summary>
        /// Manually finalize voting for a race.
        /// </summary>
        [HttpPost("finalize/{raceId}")]
        public async Task<ActionResult<FinalizeResultDto>> FinalizeVoting(int raceId)
        {
            var result = await _votingService.FinalizeVoting(raceId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        /// <summary>
        /// Report a prediction.
        /// </summary>
        [HttpPost("report")]
        [Authorize]
        public async Task<ActionResult> ReportPrediction([FromBody] ReportPredictionRequest request)
        {
            int reporterId = User.GetUserId();
            var result = await _votingService.ReportPrediction(request, reporterId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Data });
        }

        /// <summary>
        /// Admin resolves a report.
        /// </summary>
        [HttpPost("resolve-report")]
        public async Task<ActionResult> ResolveReport([FromBody] ResolveReportRequest request)
        {
            var result = await _votingService.ResolveReport(request, 0);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Data });
        }
    }
}
