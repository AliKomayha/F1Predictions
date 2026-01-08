using F1Predictions.Data;
using F1Predictions.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class TeamsService
    {
        private readonly AppDbContext _context;


        public TeamsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Team>> GetAll()
        {
            return await _context.Teams.AsNoTracking().ToListAsync();
        }

        public async Task<Team?> GetById(int id)
        {
            //return await _context.Teams
            //    .AsNoTracking()
            //    .FirstOrDefaultAsync(d => d.Id == id);

            return await _context.Teams.FindAsync(id);
        }


        public async Task Create(Team team)
        {
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

        }

        public async Task Update(Team team)
        {

            var existing = await _context.Teams.FindAsync (team.Id);
            if (existing == null) return;

            existing.Name = team.Name;
            existing.DisplayName = team.DisplayName;
            existing.BaseCountry = team.BaseCountry;
           

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null) return;

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }
    }
}
