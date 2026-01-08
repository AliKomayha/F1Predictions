using F1Predictions.Data;
using F1Predictions.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class DriverTeamsService
    {
        private readonly AppDbContext _context;

        public DriverTeamsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DriverTeam>> GetAll()
        {
            return await _context.DriverTeams
                .Include(dt => dt.Driver)
                .Include(dt => dt.Team)
                .Include(dt => dt.Championship)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<DriverTeam>> GetByChampionship(int championshipId)
        {
            return await _context.DriverTeams
                .Include(dt => dt.Driver)
                .Include(dt => dt.Team)
                .Include(dt => dt.Championship)
                .Where(dt => dt.ChampionshipId == championshipId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<DriverTeam?> GetById(int id)
        {
            return await _context.DriverTeams
                .Include(dt => dt.Driver)
                .Include(dt => dt.Team)
                .Include(dt => dt.Championship)
                .FirstOrDefaultAsync(dt => dt.Id == id);
        }

        public async Task Create(DriverTeam driverTeam)
        {
            _context.DriverTeams.Add(driverTeam);
            await _context.SaveChangesAsync();
        }

        public async Task Update(DriverTeam driverTeam)
        {
            var existing = await _context.DriverTeams.FindAsync(driverTeam.Id);
            if (existing == null) return;

            existing.DriverId = driverTeam.DriverId;
            existing.TeamId = driverTeam.TeamId;
            existing.ChampionshipId = driverTeam.ChampionshipId;

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var driverTeam = await _context.DriverTeams.FindAsync(id);
            if (driverTeam == null) return;

            _context.DriverTeams.Remove(driverTeam);
            await _context.SaveChangesAsync();
        }

        // Get lineup grouped by team for a specific championship
        public async Task<Dictionary<Team, List<Driver>>> GetLineupByTeam(int championshipId)
        {
            var driverTeams = await GetByChampionship(championshipId);
            
            return driverTeams
                .GroupBy(dt => dt.Team)
                .ToDictionary(g => g.Key, g => g.Select(dt => dt.Driver).ToList());
        }
    }
}
