using F1Predictions.Data;
using F1Predictions.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class DriversService
    {
        private readonly AppDbContext _context;


        public DriversService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<List<Driver>> GetAll()
        {
             return await _context.Drivers.AsNoTracking().ToListAsync();
        }

        public async Task<Driver?> GetById(int id)
        {
            //return await _context.Drivers
            //    .AsNoTracking()
            //    .FirstOrDefaultAsync(d => d.Id == id);

            return await _context.Drivers.FindAsync(id);
        }

        public async Task Create(Driver driver)
        {
            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();
        
        }

        public async Task Update(Driver driver)
        {

            var existing = await _context.Drivers.FindAsync(driver.Id);
            if (existing == null) return;

            existing.FirstName = driver.FirstName;
            existing.LastName = driver.LastName;
            existing.ChampionshipNumber = driver.ChampionshipNumber;
            existing.Nationality = driver.Nationality;

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return;

            _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();
        }

    }
}
