using F1Predictions.Data;
using F1Predictions.Models;
using F1Predictions.Models.DTOs;
using F1Predictions.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class PointsService : IPointsService
    {
        private readonly AppDbContext _context;

        // Maps PredictionType → (SessionType, Position)
        private static readonly Dictionary<string, (string SessionType, int Position)> PredictionTypeMapping = new()
        {
            { "Pole",         ("Qualifying",        1) },
            { "P1",           ("Race",              1) },
            { "P2",           ("Race",              2) },
            { "P3",           ("Race",              3) },
            { "SprintPole",   ("Sprint Qualifying", 1) },
            { "SprintWinner", ("Sprint",            1) },
        };

        public PointsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<PointsGrantResultDto>> GrantInitialPoints(int raceId)
        {
            // 1. Check RaceState - prevent double-granting
            var raceState = await _context.Set<RaceState>().FindAsync(raceId);
            var blockedStates = new[] { "VotingOpen", "VotingClosed", "RaceCompleted", "Finalized" };
            if (raceState != null && blockedStates.Contains(raceState.State))
            {
                return ServiceResult<PointsGrantResultDto>.Fail(
                    "Points have already been granted for this race. Use Reset & Re-grant to recalculate.");
            }

            return await CalculateAndGrantPoints(raceId);
        }

        public async Task<ServiceResult<PointsGrantResultDto>> ResetAndRegrant(int raceId)
        {
            // 1. Delete existing UserPredictionPoints for this race
            var existingPoints = await _context.Set<UserPredictionPoints>()
                .Where(upp => upp.UserPrediction.WeeklyPrediction.RaceId == raceId
                           && !upp.IsManuallyAssigned)
                .ToListAsync();

            _context.Set<UserPredictionPoints>().RemoveRange(existingPoints);

            // 2. Delete existing WeeklyPoints for this race
            var existingWeekly = await _context.Set<WeeklyPoints>()
                .Where(wp => wp.RaceId == raceId)
                .ToListAsync();

            _context.Set<WeeklyPoints>().RemoveRange(existingWeekly);

            await _context.SaveChangesAsync();

            // 3. Recalculate
            return await CalculateAndGrantPoints(raceId);
        }

        private async Task<ServiceResult<PointsGrantResultDto>> CalculateAndGrantPoints(int raceId)
        {
            // 1. Get all sessions for this race
            var sessions = await _context.Sessions
                .Where(s => s.RaceId == raceId)
                .ToListAsync();

            if (!sessions.Any())
            {
                return ServiceResult<PointsGrantResultDto>.Fail("No sessions found for this race.");
            }

            // 2. Build actual results map: (SessionType, Position) → DriverId
            var actualResults = new Dictionary<(string SessionType, int Position), int>();

            foreach (var session in sessions)
            {
                var results = await _context.Set<SessionResult>()
                    .Where(sr => sr.SessionId == session.Id && sr.Position != null)
                    .ToListAsync();

                foreach (var result in results)
                {
                    var key = (session.Type, result.Position!.Value);
                    if (!actualResults.ContainsKey(key))
                    {
                        actualResults[key] = result.DriverId;
                    }
                }
            }

            // 3. Get all user predictions for this race (position-based only)
            var eligibleTypes = PredictionTypeMapping.Keys.ToList();

            var userPredictions = await _context.Set<UserPrediction>()
                .Include(up => up.WeeklyPrediction)
                .Include(up => up.User)
                .Include(up => up.League)
                .Include(up => up.Driver)
                .Where(up => up.WeeklyPrediction.RaceId == raceId
                          && up.DriverId != null
                          && eligibleTypes.Contains(up.WeeklyPrediction.PredictionType))
                .ToListAsync();

            if (!userPredictions.Any())
            {
                return ServiceResult<PointsGrantResultDto>.Fail(
                    "No eligible predictions found for this race.");
            }

            // 4. Compare and award points
            var result2 = new PointsGrantResultDto
            {
                RaceId = raceId,
                TotalPredictionsChecked = userPredictions.Count
            };

            // Track points per user per league for WeeklyPoints aggregation
            var weeklyPointsTracker = new Dictionary<(int UserId, int LeagueId), int>();

            foreach (var prediction in userPredictions)
            {
                var predType = prediction.WeeklyPrediction.PredictionType;

                if (!PredictionTypeMapping.TryGetValue(predType, out var mapping))
                    continue;

                // Find actual driver for this position
                var actualDriverId = actualResults.GetValueOrDefault(mapping);

                var detail = new PointDetailDto
                {
                    PredictionType = predType,
                    UserName = $"{prediction.User.FirstName} {prediction.User.LastName}",
                    LeagueName = prediction.League.Name,
                    PredictedDriver = prediction.Driver != null
                        ? $"{prediction.Driver.FirstName} {prediction.Driver.LastName}"
                        : "Unknown",
                    ActualDriver = await GetDriverName(actualDriverId),
                    IsCorrect = actualDriverId != 0 && prediction.DriverId == actualDriverId
                };

                result2.Details.Add(detail);

                if (detail.IsCorrect)
                {
                    result2.CorrectPredictions++;
                    result2.PointsAwarded++;

                    // Insert UserPredictionPoints
                    _context.Set<UserPredictionPoints>().Add(new UserPredictionPoints
                    {
                        UserPredictionId = prediction.Id,
                        PointsAwarded = 1,
                        IsManuallyAssigned = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Track for weekly aggregation
                    var weeklyKey = (prediction.UserId, prediction.LeagueId);
                    if (!weeklyPointsTracker.ContainsKey(weeklyKey))
                        weeklyPointsTracker[weeklyKey] = 0;
                    weeklyPointsTracker[weeklyKey]++;
                }
            }

            // 5. Upsert WeeklyPoints
            foreach (var entry in weeklyPointsTracker)
            {
                var existing = await _context.Set<WeeklyPoints>()
                    .FirstOrDefaultAsync(wp => wp.UserId == entry.Key.UserId
                                            && wp.LeagueId == entry.Key.LeagueId
                                            && wp.RaceId == raceId);

                if (existing != null)
                {
                    existing.PointsTotal += entry.Value;
                }
                else
                {
                    _context.Set<WeeklyPoints>().Add(new WeeklyPoints
                    {
                        UserId = entry.Key.UserId,
                        LeagueId = entry.Key.LeagueId,
                        RaceId = raceId,
                        PointsTotal = entry.Value,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // 6. Auto-open voting: create PredictionVoteWindow for each league
            var now = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero)
                .ToOffset(TimeSpan.FromHours(2)); // GMT+2

            // Find all leagues that have predictions for this race
            var leagueIds = userPredictions.Select(up => up.LeagueId).Distinct().ToList();

            foreach (var leagueId in leagueIds)
            {
                // Remove existing vote windows for this race/league
                var existingWindow = await _context.Set<PredictionVoteWindow>()
                    .FirstOrDefaultAsync(vw => vw.RaceId == raceId && vw.LeagueId == leagueId);

                if (existingWindow != null)
                    _context.Set<PredictionVoteWindow>().Remove(existingWindow);

                _context.Set<PredictionVoteWindow>().Add(new PredictionVoteWindow
                {
                    LeagueId = leagueId,
                    RaceId = raceId,
                    OpensAt = now.DateTime,
                    ClosesAt = now.AddHours(24).DateTime
                });
            }

            // 7. Update RaceState to VotingOpen
            var raceState = await _context.Set<RaceState>().FindAsync(raceId);
            if (raceState != null)
            {
                raceState.State = "VotingOpen";
                raceState.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.Set<RaceState>().Add(new RaceState
                {
                    RaceId = raceId,
                    State = "VotingOpen",
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            result2.NewRaceState = "VotingOpen";
            return ServiceResult<PointsGrantResultDto>.Succeed(result2);
        }

        private async Task<string> GetDriverName(int driverId)
        {
            if (driverId == 0) return "No result";
            var driver = await _context.Drivers.FindAsync(driverId);
            return driver != null ? $"{driver.FirstName} {driver.LastName}" : "Unknown";
        }
    }
}
