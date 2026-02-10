using F1Predictions.Data;
using F1Predictions.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class WeeklyPredictionsService
    {
        private readonly AppDbContext _context;

        public WeeklyPredictionsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<WeeklyPrediction>> GetAllAsync()
        {
            return await _context.WeeklyPredictions
                .Include(wp => wp.Race)
                    .ThenInclude(r => r.Track)
                .Include(wp => wp.Race)
                    .ThenInclude(r => r.Championship)
                .AsNoTracking()
                .OrderByDescending(wp => wp.Race.RaceDate)
                .ThenBy(wp => wp.PredictionType)
                .ToListAsync();
        }

        public async Task<List<WeeklyPrediction>> GetByRaceAsync(int raceId)
        {
            return await _context.WeeklyPredictions
                .Include(wp => wp.Race)
                    .ThenInclude(r => r.Track)
                .Where(wp => wp.RaceId == raceId)
                .OrderBy(wp => wp.PredictionType)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<WeeklyPrediction?> GetByIdAsync(int id)
        {
            return await _context.WeeklyPredictions
                .Include(wp => wp.Race)
                    .ThenInclude(r => r.Track)
                .FirstOrDefaultAsync(wp => wp.Id == id);
        }

        public async Task CreateAsync(WeeklyPrediction prediction)
        {
            prediction.CreatedAt = DateTime.UtcNow;
            _context.WeeklyPredictions.Add(prediction);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(WeeklyPrediction prediction)
        {
            var existing = await _context.WeeklyPredictions.FindAsync(prediction.Id);
            if (existing == null) return;

            existing.RaceId = prediction.RaceId;
            existing.PredictionType = prediction.PredictionType;
            existing.AdminDefinedText = prediction.AdminDefinedText;
            existing.AllowedTargetTypes = prediction.AllowedTargetTypes;
            existing.IsActive = prediction.IsActive;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var prediction = await _context.WeeklyPredictions.FindAsync(id);
            if (prediction == null) return;

            _context.WeeklyPredictions.Remove(prediction);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Initialize default weekly predictions for a race.
        /// Default predictions:
        /// - Pole Position (Driver only, 1pt)
        /// - P1 (Driver only, 1pt)
        /// - P2 (Driver only, 1pt)
        /// - P3 (Driver only, 1pt)
        /// - Big Surprise (Driver=1pt or Team=2pts)
        /// - Big Flop (Driver=1pt or Team=2pts)
        /// - Crazy Prediction (Text only, 1pt)
        /// </summary>
        public async Task InitializeDefaultAsync(int raceId)
        {
            // Check if predictions already exist for this race
            var existing = await _context.WeeklyPredictions
                .AnyAsync(wp => wp.RaceId == raceId);

            if (existing) return;

            var defaults = new List<WeeklyPrediction>
            {
                new WeeklyPrediction
                {
                    RaceId = raceId,
                    PredictionType = "Pole",
                    AdminDefinedText = "Who will take pole position?",
                    AllowedTargetTypes = "Driver",
                    IsActive = true
                },
                new WeeklyPrediction
                {
                    RaceId = raceId,
                    PredictionType = "P1",
                    AdminDefinedText = "Who will win the race?",
                    AllowedTargetTypes = "Driver",
                    IsActive = true
                },
                new WeeklyPrediction
                {
                    RaceId = raceId,
                    PredictionType = "P2",
                    AdminDefinedText = "Who will finish P2?",
                    AllowedTargetTypes = "Driver",
                    IsActive = true
                },
                new WeeklyPrediction
                {
                    RaceId = raceId,
                    PredictionType = "P3",
                    AdminDefinedText = "Who will finish P3?",
                    AllowedTargetTypes = "Driver",
                    IsActive = true
                },
                new WeeklyPrediction
                {
                    RaceId = raceId,
                    PredictionType = "Surprise",
                    AdminDefinedText = "Who will be the big surprise this race?",
                    AllowedTargetTypes = "Driver,Team",
                    IsActive = true
                },
                new WeeklyPrediction
                {
                    RaceId = raceId,
                    PredictionType = "Flop",
                    AdminDefinedText = "Who will be the big flop this race?",
                    AllowedTargetTypes = "Driver,Team",
                    IsActive = true
                },
                new WeeklyPrediction
                {
                    RaceId = raceId,
                    PredictionType = "Crazy",
                    AdminDefinedText = "What is your crazy prediction for this race?",
                    AllowedTargetTypes = "Text",
                    IsActive = true
                }
            };

            _context.WeeklyPredictions.AddRange(defaults);
            await _context.SaveChangesAsync();
        }

        // Static list of prediction types
        public static List<string> GetPredictionTypes()
        {
            return new List<string>
            {
                "Pole",
                "P1",
                "P2",
                "P3",
                "SprintPole",
                "SprintWinner",
                "Surprise",
                "Flop",
                "Crazy",
                "Custom"
            };
        }

        // Static list of allowed target type combinations
        public static List<string> GetAllowedTargetTypeOptions()
        {
            return new List<string>
            {
                "Driver",
                "Driver,Team",
                "Text"
            };
        }
    }
}
