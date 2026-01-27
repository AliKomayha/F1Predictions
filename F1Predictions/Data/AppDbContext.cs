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
        public DbSet<Race> Races { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionResult> SessionResults { get; set; }
        public DbSet<PointsSystem> PointsSystems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserOtp> UserOtps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // One result per driver per session
            modelBuilder.Entity<SessionResult>()
                .HasIndex(sr => new { sr.SessionId, sr.DriverId })
                .IsUnique()
                .HasDatabaseName("IX_SessionResult_Session_Driver");

            // One race per championship per round
            modelBuilder.Entity<Race>()
                .HasIndex(r => new { r.ChampionshipId, r.RoundNumber })
                .IsUnique()
                .HasDatabaseName("IX_Race_Championship_Round");

            // One points entry per championship, session type, and position
            modelBuilder.Entity<PointsSystem>()
                .HasIndex(ps => new { ps.ChampionshipId, ps.SessionType, ps.Position })
                .IsUnique()
                .HasDatabaseName("IX_PointsSystem_Championship_SessionType_Position");
        }
    }
}
