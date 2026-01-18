using F1Predictions.Data;
using F1Predictions.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class SessionResultsService
    {
        private readonly AppDbContext _context;

        public SessionResultsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SessionResult>> GetBySession(int sessionId)
        {
            return await _context.SessionResults
                .Include(sr => sr.Session)
                    .ThenInclude(s => s.Race)
                .Include(sr => sr.Driver)
                .Include(sr => sr.Team)
                .Where(sr => sr.SessionId == sessionId)
                .OrderBy(sr => sr.Position ?? 999)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<SessionResult?> GetById(int id)
        {
            return await _context.SessionResults
                .Include(sr => sr.Session)
                    .ThenInclude(s => s.Race)
                .Include(sr => sr.Driver)
                .Include(sr => sr.Team)
                .FirstOrDefaultAsync(sr => sr.Id == id);
        }

        public async Task Create(SessionResult result)
        {
            _context.SessionResults.Add(result);
            await _context.SaveChangesAsync();
        }

        public async Task Update(SessionResult result)
        {
            var existing = await _context.SessionResults.FindAsync(result.Id);
            if (existing == null) return;

            existing.SessionId = result.SessionId;
            existing.DriverId = result.DriverId;
            existing.TeamId = result.TeamId;
            existing.Position = result.Position;
            existing.GridPosition = result.GridPosition;
            existing.Status = result.Status;
            existing.Points = result.Points;
            existing.TimeMs = result.TimeMs;
            existing.FastestLap = result.FastestLap;

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var result = await _context.SessionResults.FindAsync(id);
            if (result == null) return;

            _context.SessionResults.Remove(result);
            await _context.SaveChangesAsync();
        }

        // Calculate points based on position and session type
        public async Task<decimal> CalculatePoints(int championshipId, string sessionType, int position)
        {
            var pointsEntry = await _context.PointsSystems
                .FirstOrDefaultAsync(ps => 
                    ps.ChampionshipId == championshipId && 
                    ps.SessionType == sessionType && 
                    ps.Position == position);

            return pointsEntry?.Points ?? 0;
        }

        // Get result statuses for dropdown
        public static List<string> GetStatuses()
        {
            return new List<string>
            {
                "Finished", "DNF", "DNS", "DSQ", "Retired", "Not Classified"
            };
        }
    }
}
