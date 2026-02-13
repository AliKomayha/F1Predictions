using F1Predictions.Models;
using F1Predictions.Models.DTOs;

namespace F1Predictions.Services.Interfaces
{
    public interface IPointsService
    {
        Task<ServiceResult<PointsGrantResultDto>> GrantInitialPoints(int raceId);
        Task<ServiceResult<PointsGrantResultDto>> ResetAndRegrant(int raceId);
    }
}
