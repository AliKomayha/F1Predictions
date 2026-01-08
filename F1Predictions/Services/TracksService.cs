using F1Predictions.Data;
using F1Predictions.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class TracksService
    {
        private readonly AppDbContext _context;

        public TracksService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Track>> GetAll()
        {
            return await _context.Tracks.AsNoTracking().ToListAsync();
        }

        public async Task<Track?> GetById(int id)
        {
            return await _context.Tracks.FindAsync(id);
        }

        public async Task Create(Track track)
        {
            _context.Tracks.Add(track);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Track track)
        {
            var existing = await _context.Tracks.FindAsync(track.Id);
            if (existing == null) return;

            existing.Name = track.Name;
            existing.Country = track.Country;
            existing.City = track.City;

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var track = await _context.Tracks.FindAsync(id);
            if (track == null) return;

            _context.Tracks.Remove(track);
            await _context.SaveChangesAsync();
        }
    }
}
