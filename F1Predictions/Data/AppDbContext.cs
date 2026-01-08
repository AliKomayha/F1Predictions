using Microsoft.EntityFrameworkCore;
using F1Predictions.Models;

namespace F1Predictions.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
               : base(options) { }

        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Championship> Championships { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<DriverTeam> DriverTeams { get; set; }

    }
}
