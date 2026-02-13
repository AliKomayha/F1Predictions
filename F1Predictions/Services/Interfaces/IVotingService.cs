using F1Predictions.Models;
using F1Predictions.Models.DTOs;

namespace F1Predictions.Services.Interfaces
{
    public interface IVotingService
    {
        Task<ServiceResult<List<VotablePredictionDto>>> GetVotablePredictions(int raceId, int leagueId, int userId);
        Task<ServiceResult<VotingStatusDto>> GetVotingStatus(int raceId, int leagueId);
        Task<ServiceResult<VoteResultDto>> CastVote(CastVoteRequest request, int voterId);
        Task<ServiceResult<string>> AdminDecide(AdminDecideRequest request, int adminId);
        Task<ServiceResult<string>> ReportPrediction(ReportPredictionRequest request, int reporterId);
        Task<ServiceResult<string>> ResolveReport(ResolveReportRequest request, int adminId);
        Task<ServiceResult<FinalizeResultDto>> FinalizeVoting(int raceId);
        Task FinalizeExpiredVotingWindows(); // Called by background service
    }
}
