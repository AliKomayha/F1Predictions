using F1Predictions.Data;
using F1Predictions.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class SessionsService
    {
        private readonly AppDbContext _context;
        
        // Fixed timezone offset for Lebanon (GMT+2)
        private static readonly TimeSpan ApplicationTimeZoneOffset = TimeSpan.FromHours(2);

        public SessionsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Session>> GetAll()
        {
            return await _context.Sessions
                .Include(s => s.Race)
                    .ThenInclude(r => r.Track)
                .Include(s => s.Championship)
                .AsNoTracking()
                .OrderBy(s => s.DateTime)
                .ToListAsync();
        }

        public async Task<List<Session>> GetByRace(int raceId)
        {
            return await _context.Sessions
                .Include(s => s.Race)
                    .ThenInclude(r => r.Track)
                .Include(s => s.Championship)
                .Where(s => s.RaceId == raceId)
                .OrderBy(s => s.DateTime)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Session>> GetByChampionship(int championshipId)
        {
            return await _context.Sessions
                .Include(s => s.Race)
                    .ThenInclude(r => r.Track)
                .Include(s => s.Championship)
                .Where(s => s.ChampionshipId == championshipId)
                .OrderBy(s => s.Race.RoundNumber)
                .ThenBy(s => s.DateTime)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Session?> GetById(int id)
        {
            return await _context.Sessions
                .Include(s => s.Race)
                    .ThenInclude(r => r.Track)
                .Include(s => s.Championship)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task Create(Session session)
        {
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Update race lock time if this is a qualifying session
            if (session.Type == "Qualifying" || session.Type == "Sprint Qualifying")
            {
                await UpdateRaceLockTime(session.RaceId);
            }
        }

        public async Task Update(Session session)
        {
            var existing = await _context.Sessions.FindAsync(session.Id);
            if (existing == null) return;

            var oldRaceId = existing.RaceId;
            var wasQualifying = existing.Type == "Qualifying" || existing.Type == "Sprint Qualifying";
            var isQualifying = session.Type == "Qualifying" || session.Type == "Sprint Qualifying";

            existing.RaceId = session.RaceId;
            existing.ChampionshipId = session.ChampionshipId;
            existing.Type = session.Type;
            existing.DateTime = session.DateTime;
            existing.Laps = session.Laps;

            await _context.SaveChangesAsync();

            // Update race lock time if qualifying session changed
            if (isQualifying || wasQualifying)
            {
                await UpdateRaceLockTime(session.RaceId);
                // If race changed, update the old race too
                if (oldRaceId != session.RaceId && wasQualifying)
                {
                    await UpdateRaceLockTime(oldRaceId);
                }
            }
        }

        public async Task Delete(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return;

            var raceId = session.RaceId;
            var wasQualifying = session.Type == "Qualifying" || session.Type == "Sprint Qualifying";

            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();

            // Update race lock time if a qualifying session was deleted
            if (wasQualifying)
            {
                await UpdateRaceLockTime(raceId);
            }
        }

        // Get session types for dropdown
        public static List<string> GetSessionTypes()
        {
            return new List<string>
            {
                "FP1", "FP2", "FP3",
                "Qualifying", "Sprint Qualifying",
                "Sprint", "Race"
            };
        }

        private async Task UpdateRaceLockTime(int raceId)
        {
            var race = await _context.Races.FindAsync(raceId);
            if (race == null) return;

            // Check for Sprint Qualifying first (sprint weekends), then regular Qualifying
            var session = await _context.Sessions
                .Where(s => s.RaceId == raceId &&
                       (s.Type == "Sprint Qualifying" || s.Type == "Qualifying"))
                .OrderBy(s => s.DateTime)  // Get the earliest one
                .FirstOrDefaultAsync();

            if (session != null)
            {
                // Convert DateTime to DateTimeOffset using fixed GMT+2 offset
                race.PredictionsLockedAt = new DateTimeOffset(session.DateTime, ApplicationTimeZoneOffset);
            }
            else
            {
                // If no qualifying session exists, default to far future
                race.PredictionsLockedAt = DateTimeOffset.Parse("9999-12-31 23:59:59 +00:00");
            }

            await _context.SaveChangesAsync();
        }
    }
}
