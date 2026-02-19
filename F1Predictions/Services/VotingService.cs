using F1Predictions.Data;
using F1Predictions.Models;
using F1Predictions.Models.DTOs;
using F1Predictions.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class VotingService : IVotingService
    {
        private readonly AppDbContext _context;

        // Votable prediction types
        private static readonly string[] VotableTypes = { "Surprise", "Flop", "Crazy", "Custom" };

        // Fixed timezone offset for Lebanon (GMT+2)
        private static readonly TimeSpan ApplicationTimeZoneOffset = TimeSpan.FromHours(2);

        public VotingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<VotingStatusDto>> GetVotingStatus(int raceId, int leagueId)
        {
            var window = await _context.Set<PredictionVoteWindow>()
                .FirstOrDefaultAsync(vw => vw.RaceId == raceId && vw.LeagueId == leagueId);

            var raceState = await _context.Set<RaceState>().FindAsync(raceId);

            var nowGmt2 = DateTimeOffset.UtcNow.ToOffset(ApplicationTimeZoneOffset).DateTime;

            return ServiceResult<VotingStatusDto>.Succeed(new VotingStatusDto
            {
                IsVotingOpen = window != null && nowGmt2 >= window.OpensAt && nowGmt2 <= window.ClosesAt,
                OpensAt = window?.OpensAt,
                ClosesAt = window?.ClosesAt,
                RaceState = raceState?.State ?? "Unknown"
            });
        }

        public async Task<ServiceResult<List<VotablePredictionDto>>> GetVotablePredictions(int raceId, int leagueId, int userId)
        {
            // Verify user is a league member
            var isMember = await _context.Set<LeagueMember>()
                .AnyAsync(lm => lm.LeagueId == leagueId && lm.UserId == userId);

            if (!isMember)
                return ServiceResult<List<VotablePredictionDto>>.Fail("You are not a member of this league.");

            // Get all votable predictions for this race in this league
            var predictions = await _context.Set<UserPrediction>()
                .Include(up => up.WeeklyPrediction)
                .Include(up => up.User)
                .Include(up => up.Driver)
                .Include(up => up.Team)
                .Include(up => up.Votes)
                .Where(up => up.WeeklyPrediction.RaceId == raceId
                          && up.LeagueId == leagueId
                          && VotableTypes.Contains(up.WeeklyPrediction.PredictionType))
                .ToListAsync();

            var result = new List<VotablePredictionDto>();

            foreach (var pred in predictions)
            {
                var yesVotes = pred.Votes.Count(v => v.Vote);
                var noVotes = pred.Votes.Count(v => !v.Vote);
                var myVote = pred.Votes.FirstOrDefault(v => v.VoterId == userId);

                // Check if already resolved (has points entry)
                var pointsEntry = await _context.Set<UserPredictionPoints>()
                    .FirstOrDefaultAsync(upp => upp.UserPredictionId == pred.Id);

                // Check admin decision
                var decision = await _context.Set<PredictionDecision>()
                    .FirstOrDefaultAsync(pd => pd.UserPredictionId == pred.Id);

                result.Add(new VotablePredictionDto
                {
                    UserPredictionId = pred.Id,
                    PredictionType = pred.WeeklyPrediction.PredictionType,
                    TargetType = pred.TargetType,
                    UserName = $"{pred.User.FirstName} {pred.User.LastName}",
                    UserId = pred.UserId,
                    DriverName = pred.Driver != null ? $"{pred.Driver.FirstName} {pred.Driver.LastName}" : null,
                    TeamName = pred.Team?.DisplayName,
                    Text = pred.Text,
                    YesVotes = yesVotes,
                    NoVotes = noVotes,
                    MyVote = myVote?.Vote,
                    IsResolved = pointsEntry != null,
                    WasApproved = decision?.IsApproved ?? (pointsEntry != null ? pointsEntry.PointsAwarded > 0 : null),
                    PointsValue = GetPointsValue(pred.TargetType),
                    IsOwnPrediction = pred.UserId == userId
                });
            }

            return ServiceResult<List<VotablePredictionDto>>.Succeed(result);
        }

        public async Task<ServiceResult<VoteResultDto>> CastVote(CastVoteRequest request, int voterId)
        {
            // 1. Get the prediction with its context
            var prediction = await _context.Set<UserPrediction>()
                .Include(up => up.WeeklyPrediction)
                .FirstOrDefaultAsync(up => up.Id == request.UserPredictionId);

            if (prediction == null)
                return ServiceResult<VoteResultDto>.Fail("Prediction not found.");

            // 2. Verify votable type
            if (!VotableTypes.Contains(prediction.WeeklyPrediction.PredictionType))
                return ServiceResult<VoteResultDto>.Fail("This prediction type is not votable.");

            // 3. Prevent voting on your own prediction
            if (prediction.UserId == voterId)
                return ServiceResult<VoteResultDto>.Fail("You cannot vote on your own prediction.");

            // 4. Verify voter is a league member
            var isMember = await _context.Set<LeagueMember>()
                .AnyAsync(lm => lm.LeagueId == prediction.LeagueId && lm.UserId == voterId);

            if (!isMember)
                return ServiceResult<VoteResultDto>.Fail("You are not a member of this league.");

            // 4. Check voting window is open
            var nowGmt2 = DateTimeOffset.UtcNow.ToOffset(ApplicationTimeZoneOffset).DateTime;
            var window = await _context.Set<PredictionVoteWindow>()
                .FirstOrDefaultAsync(vw => vw.RaceId == prediction.WeeklyPrediction.RaceId
                                        && vw.LeagueId == prediction.LeagueId);

            if (window == null || nowGmt2 < window.OpensAt || nowGmt2 > window.ClosesAt)
                return ServiceResult<VoteResultDto>.Fail("Voting is not currently open.");

            // 5. Check for duplicate vote (update if exists)
            var existingVote = await _context.Set<PredictionVote>()
                .FirstOrDefaultAsync(v => v.UserPredictionId == request.UserPredictionId && v.VoterId == voterId);

            if (existingVote != null)
            {
                existingVote.Vote = request.Vote;
                existingVote.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.Set<PredictionVote>().Add(new PredictionVote
                {
                    UserPredictionId = request.UserPredictionId,
                    VoterId = voterId,
                    Vote = request.Vote,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            // 6. Check for auto-resolution (majority reached)
            var allVotes = await _context.Set<PredictionVote>()
                .Where(v => v.UserPredictionId == request.UserPredictionId)
                .ToListAsync();

            var yesCount = allVotes.Count(v => v.Vote);
            var noCount = allVotes.Count(v => !v.Vote);

            // Get total league members
            var totalMembers = await _context.Set<LeagueMember>()
                .CountAsync(lm => lm.LeagueId == prediction.LeagueId);

            var majorityThreshold = (totalMembers / 2) + 1;

            var result = new VoteResultDto
            {
                VoteRecorded = true,
                YesVotes = yesCount,
                NoVotes = noCount,
                WasAutoResolved = false,
                Resolution = null
            };

            // Check if already resolved
            var alreadyResolved = await _context.Set<UserPredictionPoints>()
                .AnyAsync(upp => upp.UserPredictionId == request.UserPredictionId);

            if (!alreadyResolved)
            {
                if (yesCount >= majorityThreshold)
                {
                    // Auto-approve: grant points
                    await GrantVotePoints(prediction, true);
                    result.WasAutoResolved = true;
                    result.Resolution = true;
                }
                else if (noCount >= majorityThreshold)
                {
                    // Auto-deny: insert 0 points
                    await GrantVotePoints(prediction, false);
                    result.WasAutoResolved = true;
                    result.Resolution = false;
                }
            }

            return ServiceResult<VoteResultDto>.Succeed(result);
        }

        public async Task<ServiceResult<string>> AdminDecide(AdminDecideRequest request, int adminId)
        {
            var prediction = await _context.Set<UserPrediction>()
                .Include(up => up.WeeklyPrediction)
                .FirstOrDefaultAsync(up => up.Id == request.UserPredictionId);

            if (prediction == null)
                return ServiceResult<string>.Fail("Prediction not found.");

            // Check if already decided
            var existingDecision = await _context.Set<PredictionDecision>()
                .FirstOrDefaultAsync(pd => pd.UserPredictionId == request.UserPredictionId);

            if (existingDecision != null)
                return ServiceResult<string>.Fail("Admin decision already made for this prediction.");

            // Insert admin decision
            _context.Set<PredictionDecision>().Add(new PredictionDecision
            {
                UserPredictionId = request.UserPredictionId,
                DecidedByAdminId = adminId,
                IsApproved = request.IsApproved,
                PointsGranted = request.IsApproved ? GetPointsValue(prediction.TargetType) : 0,
                DecisionNote = request.Note,
                CreatedAt = DateTime.UtcNow
            });

            // Grant or deny points
            await GrantVotePoints(prediction, request.IsApproved);

            return ServiceResult<string>.Succeed(
                request.IsApproved ? "Prediction approved and points granted." : "Prediction denied. 0 points recorded.");
        }

        public async Task<ServiceResult<string>> ReportPrediction(ReportPredictionRequest request, int reporterId)
        {
            var prediction = await _context.Set<UserPrediction>()
                .FirstOrDefaultAsync(up => up.Id == request.UserPredictionId);

            if (prediction == null)
                return ServiceResult<string>.Fail("Prediction not found.");

            // Check reporter is a league member
            var isMember = await _context.Set<LeagueMember>()
                .AnyAsync(lm => lm.LeagueId == prediction.LeagueId && lm.UserId == reporterId);

            if (!isMember)
                return ServiceResult<string>.Fail("You are not a member of this league.");

            _context.Set<PredictionReport>().Add(new PredictionReport
            {
                UserPredictionId = request.UserPredictionId,
                ReporterId = reporterId,
                Reason = request.Reason,
                IsResolved = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return ServiceResult<string>.Succeed("Report submitted successfully.");
        }

        public async Task<ServiceResult<string>> ResolveReport(ResolveReportRequest request, int adminId)
        {
            var report = await _context.Set<PredictionReport>().FindAsync(request.ReportId);

            if (report == null)
                return ServiceResult<string>.Fail("Report not found.");

            report.IsResolved = true;
            report.ResolvedByAdminId = adminId;
            report.ResolutionNote = request.Note;

            await _context.SaveChangesAsync();
            return ServiceResult<string>.Succeed("Report resolved.");
        }

        public async Task<ServiceResult<FinalizeResultDto>> FinalizeVoting(int raceId)
        {
            var result = new FinalizeResultDto();

            // Get all votable predictions for this race (across all leagues)
            var predictions = await _context.Set<UserPrediction>()
                .Include(up => up.WeeklyPrediction)
                .Include(up => up.Votes)
                .Where(up => up.WeeklyPrediction.RaceId == raceId
                          && VotableTypes.Contains(up.WeeklyPrediction.PredictionType))
                .ToListAsync();

            result.TotalPredictions = predictions.Count;

            foreach (var pred in predictions)
            {
                // Skip if already resolved
                var alreadyResolved = await _context.Set<UserPredictionPoints>()
                    .AnyAsync(upp => upp.UserPredictionId == pred.Id);

                if (alreadyResolved)
                {
                    result.AlreadyResolved++;
                    continue;
                }

                var yesCount = pred.Votes.Count(v => v.Vote);
                var noCount = pred.Votes.Count(v => !v.Vote);

                if (yesCount == 0 && noCount == 0)
                {
                    // No votes: insert 0 points
                    await GrantVotePoints(pred, false);
                    result.NoVotesCast++;
                }
                else if (yesCount > noCount)
                {
                    // Majority yes: grant points
                    await GrantVotePoints(pred, true);
                    result.ApprovedByVotes++;
                }
                else if (noCount > yesCount)
                {
                    // Majority no: deny points
                    await GrantVotePoints(pred, false);
                    result.DeniedByVotes++;
                }
                else
                {
                    // Tie: flag for admin (don't insert points yet)
                    result.TiedForAdmin++;
                }
            }

            // Update RaceState to VotingClosed
            var raceState = await _context.Set<RaceState>().FindAsync(raceId);
            if (raceState != null)
            {
                raceState.State = "VotingClosed";
                raceState.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return ServiceResult<FinalizeResultDto>.Succeed(result);
        }

        /// <summary>
        /// Called by background service — checks for expired vote windows and finalizes them.
        /// </summary>
        public async Task FinalizeExpiredVotingWindows()
        {
            var nowGmt2 = DateTimeOffset.UtcNow.ToOffset(ApplicationTimeZoneOffset).DateTime;

            // Find vote windows that have closed
            var expiredWindows = await _context.Set<PredictionVoteWindow>()
                .Where(vw => vw.ClosesAt <= nowGmt2)
                .Select(vw => vw.RaceId)
                .Distinct()
                .ToListAsync();

            foreach (var raceId in expiredWindows)
            {
                // Check if already finalized
                var raceState = await _context.Set<RaceState>().FindAsync(raceId);
                if (raceState?.State == "VotingOpen")
                {
                    await FinalizeVoting(raceId);
                }
            }
        }

        private async Task GrantVotePoints(UserPrediction prediction, bool approved)
        {
            var pointsValue = approved ? GetPointsValue(prediction.TargetType) : 0;

            _context.Set<UserPredictionPoints>().Add(new UserPredictionPoints
            {
                UserPredictionId = prediction.Id,
                PointsAwarded = pointsValue,
                IsManuallyAssigned = false,
                CreatedAt = DateTime.UtcNow
            });

            // Update WeeklyPoints if points were granted
            if (pointsValue > 0)
            {
                var raceId = prediction.WeeklyPrediction.RaceId;
                var existing = await _context.Set<WeeklyPoints>()
                    .FirstOrDefaultAsync(wp => wp.UserId == prediction.UserId
                                            && wp.LeagueId == prediction.LeagueId
                                            && wp.RaceId == raceId);

                if (existing != null)
                {
                    existing.PointsTotal += pointsValue;
                }
                else
                {
                    _context.Set<WeeklyPoints>().Add(new WeeklyPoints
                    {
                        UserId = prediction.UserId,
                        LeagueId = prediction.LeagueId,
                        RaceId = raceId,
                        PointsTotal = pointsValue,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private static int GetPointsValue(string targetType)
        {
            return targetType switch
            {
                "Team" => 2,
                _ => 1  // Driver and Text = 1 point
            };
        }
    }
}
