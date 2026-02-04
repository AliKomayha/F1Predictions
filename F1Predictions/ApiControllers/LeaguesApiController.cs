using F1Predictions.Extensions;
using F1Predictions.Models.DTOs;
using F1Predictions.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictions.ApiControllers
{
    [Route("api/leagues")]
    [ApiController]
    [Authorize]
    public class LeaguesApiController : ControllerBase
    {
        private readonly ILeaguesService _leaguesService;

        public LeaguesApiController(ILeaguesService leaguesService)
        {
            _leaguesService = leaguesService;
        }

        /// <summary>
        /// Gets all leagues that the current user is a member of.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<LeagueDto>>> GetUserLeagues()
        {
            int userId = User.GetUserId();
            var result = await _leaguesService.GetUserLeagues(userId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        /// <summary>
        /// Creates a new league. The current user becomes the owner and is automatically added as a member.
        /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult<LeagueDto>> CreateLeague([FromBody] CreateLeagueDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int userId = User.GetUserId();
            var result = await _leaguesService.CreateLeague(dto, userId);

            if (!result.Success)
                return BadRequest(result.Message);

            return CreatedAtAction(nameof(GetUserLeagues), result.Data);
        }

        /// <summary>
        /// Joins a league using an invite code.
        /// </summary>
        [HttpPost("join")]
        public async Task<ActionResult> JoinLeague([FromBody] JoinLeagueDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int userId = User.GetUserId();
            var result = await _leaguesService.JoinLeagueByCodeAsync(dto.InviteCode, userId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new { message = result.Message });
        }
    }
}