using F1Predictions.Data;
using F1Predictions.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class RacesService
    {
        private readonly AppDbContext _context;
        
        // Fixed timezone offset for Lebanon (GMT+2)
        private static readonly TimeSpan ApplicationTimeZoneOffset = TimeSpan.FromHours(2);

        public RacesService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Race>> GetAll()
        {
            return await _context.Races
                .Include(r => r.Championship)
                .Include(r => r.Track)
                .AsNoTracking()
                .OrderBy(r => r.Championship.Year)
                .ThenBy(r => r.RoundNumber)
                .ToListAsync();
        }

        public async Task<List<Race>> GetByChampionship(int championshipId)
        {
            return await _context.Races
                .Include(r => r.Championship)
                .Include(r => r.Track)
                .Where(r => r.ChampionshipId == championshipId)
                .OrderBy(r => r.RoundNumber)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Race?> GetById(int id)
        {
            return await _context.Races
                .Include(r => r.Championship)
                .Include(r => r.Track)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task Create(Race race)
        {
            _context.Races.Add(race);
            await _context.SaveChangesAsync();

            // Calculate and set lock time after race is created (so we have an ID)
            race.PredictionsLockedAt = await CalculatePredictionsLockTime(race.Id);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Race race)
        {
            var existing = await _context.Races.FindAsync(race.Id);
            if (existing == null) return;

            existing.ChampionshipId = race.ChampionshipId;
            existing.TrackId = race.TrackId;
            existing.RaceName = race.RaceName;
            existing.RoundNumber = race.RoundNumber;
            existing.RaceDate = race.RaceDate;

            // Recalculate lock time in case sessions changed
            existing.PredictionsLockedAt = await CalculatePredictionsLockTime(race.Id);

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var race = await _context.Races.FindAsync(id);
            if (race == null) return;

            _context.Races.Remove(race);
            await _context.SaveChangesAsync();
        }

        private async Task<DateTimeOffset> CalculatePredictionsLockTime(int raceId)
        {
            // Check for Sprint Qualifying first (sprint weekends), then regular Qualifying
            var session = await _context.Sessions
                .Where(s => s.RaceId == raceId &&
                       (s.Type == "Sprint Qualifying" || s.Type == "Qualifying"))
                .OrderBy(s => s.DateTime)  // Get the earliest one
                .FirstOrDefaultAsync();

            if (session != null)
            {
                // Convert DateTime to DateTimeOffset using fixed GMT+2 offset
                return new DateTimeOffset(session.DateTime, ApplicationTimeZoneOffset);
            }

            // If no qualifying session exists, default to far future
            return DateTimeOffset.Parse("9999-12-31 23:59:59 +00:00");
        }
    }
}
