using F1Predictions.Data;
using F1Predictions.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class PointsSystemService
    {
        private readonly AppDbContext _context;

        public PointsSystemService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PointsSystem>> GetAll()
        {
            return await _context.PointsSystems
                .Include(ps => ps.Championship)
                .AsNoTracking()
                .OrderBy(ps => ps.Championship.Year)
                .ThenBy(ps => ps.SessionType)
                .ThenBy(ps => ps.Position)
                .ToListAsync();
        }

        public async Task<List<PointsSystem>> GetByChampionship(int championshipId)
        {
            return await _context.PointsSystems
                .Include(ps => ps.Championship)
                .Where(ps => ps.ChampionshipId == championshipId)
                .OrderBy(ps => ps.SessionType)
                .ThenBy(ps => ps.Position)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<PointsSystem>> GetBySessionType(int championshipId, string sessionType)
        {
            return await _context.PointsSystems
                .Where(ps => ps.ChampionshipId == championshipId && ps.SessionType == sessionType)
                .OrderBy(ps => ps.Position)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PointsSystem?> GetById(int id)
        {
            return await _context.PointsSystems
                .Include(ps => ps.Championship)
                .FirstOrDefaultAsync(ps => ps.Id == id);
        }

        public async Task Create(PointsSystem pointsSystem)
        {
            _context.PointsSystems.Add(pointsSystem);
            await _context.SaveChangesAsync();
        }

        public async Task Update(PointsSystem pointsSystem)
        {
            var existing = await _context.PointsSystems.FindAsync(pointsSystem.Id);
            if (existing == null) return;

            existing.ChampionshipId = pointsSystem.ChampionshipId;
            existing.SessionType = pointsSystem.SessionType;
            existing.Position = pointsSystem.Position;
            existing.Points = pointsSystem.Points;

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var entry = await _context.PointsSystems.FindAsync(id);
            if (entry == null) return;

            _context.PointsSystems.Remove(entry);
            await _context.SaveChangesAsync();
        }

        // Initialize default F1 points system for a championship
        public async Task InitializeDefaultPoints(int championshipId)
        {
            // Check if already exists
            var existing = await _context.PointsSystems
                .AnyAsync(ps => ps.ChampionshipId == championshipId);
            
            if (existing) return;

            // Race points (1st-10th)
            var racePoints = new[] { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };
            for (int i = 0; i < racePoints.Length; i++)
            {
                _context.PointsSystems.Add(new PointsSystem
                {
                    ChampionshipId = championshipId,
                    SessionType = "Race",
                    Position = i + 1,
                    Points = racePoints[i]
                });
            }

            // Sprint points (1st-8th)
            var sprintPoints = new[] { 8, 7, 6, 5, 4, 3, 2, 1 };
            for (int i = 0; i < sprintPoints.Length; i++)
            {
                _context.PointsSystems.Add(new PointsSystem
                {
                    ChampionshipId = championshipId,
                    SessionType = "Sprint",
                    Position = i + 1,
                    Points = sprintPoints[i]
                });
            }

            await _context.SaveChangesAsync();
        }

        // Get session types that award points
        public static List<string> GetPointsSessionTypes()
        {
            return new List<string> { "Race", "Sprint" };
        }
    }
}
