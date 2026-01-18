using F1Predictions.Data;
using F1Predictions.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class SessionsService
    {
        private readonly AppDbContext _context;

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
        }

        public async Task Update(Session session)
        {
            var existing = await _context.Sessions.FindAsync(session.Id);
            if (existing == null) return;

            existing.RaceId = session.RaceId;
            existing.ChampionshipId = session.ChampionshipId;
            existing.Type = session.Type;
            existing.DateTime = session.DateTime;
            existing.Laps = session.Laps;

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return;

            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();
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
    }
}
