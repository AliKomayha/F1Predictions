using F1Predictions.Data;
using F1Predictions.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class ChampionshipsService
    {
        private readonly AppDbContext _context;

        public ChampionshipsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Championship>> GetAll()
        {
            return await _context.Championships.AsNoTracking().ToListAsync();
        }

        public async Task<Championship?> GetById(int id)
        {
            return await _context.Championships.FindAsync(id);
        }

        public async Task Create(Championship championship)
        {
            _context.Championships.Add(championship);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Championship championship)
        {
            var existing = await _context.Championships.FindAsync(championship.Id);
            if (existing == null) return;

            existing.Year = championship.Year;
            existing.Name = championship.Name;

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var championship = await _context.Championships.FindAsync(id);
            if (championship == null) return;

            _context.Championships.Remove(championship);
            await _context.SaveChangesAsync();
        }
    }
}
