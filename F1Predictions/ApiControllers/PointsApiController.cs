using F1Predictions.Models.DTOs;
using F1Predictions.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictions.ApiControllers
{
    [Route("api/points")]
    [ApiController]
    public class PointsApiController : ControllerBase
    {
        private readonly IPointsService _pointsService;

        public PointsApiController(IPointsService pointsService)
        {
            _pointsService = pointsService;
        }

        /// <summary>
        /// Grants initial position-based points for a race.
        /// Compares predictions against session results.
        /// </summary>
        [HttpPost("grant/{raceId}")]
        public async Task<ActionResult<PointsGrantResultDto>> GrantInitialPoints(int raceId)
        {
            var result = await _pointsService.GrantInitialPoints(raceId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        /// <summary>
        /// Resets previously granted points and recalculates.
        /// Use when race results have changed.
        /// </summary>
        [HttpPost("reset-and-regrant/{raceId}")]
        public async Task<ActionResult<PointsGrantResultDto>> ResetAndRegrant(int raceId)
        {
            var result = await _pointsService.ResetAndRegrant(raceId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }
    }
}
