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
        public DbSet<League> Leagues { get; set; }
        public DbSet<LeagueMember> LeagueMembers { get; set; }
        public DbSet<WeeklyPrediction> WeeklyPredictions { get; set; }
        public DbSet<UserPrediction> UserPredictions { get; set; }
        public DbSet<UserPredictionPoints> UserPredictionPoints { get; set; }
        public DbSet<WeeklyPoints> WeeklyPoints { get; set; }
        public DbSet<PredictionVote> PredictionVotes { get; set; }
        public DbSet<PredictionDecision> PredictionDecisions { get; set; }
        public DbSet<PredictionReport> PredictionReports { get; set; }
        public DbSet<PredictionVoteWindow> PredictionVoteWindows { get; set; }
        public DbSet<RaceState> RaceStates { get; set; }

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

            // One member per user per league
            modelBuilder.Entity<LeagueMember>()
                .HasIndex(lm => new { lm.LeagueId, lm.UserId })
                .IsUnique()
                .HasDatabaseName("UX_LeagueMembers");

            // One prediction per user per league per weekly prediction
            modelBuilder.Entity<UserPrediction>()
                .HasIndex(up => new { up.UserId, up.LeagueId, up.WeeklyPredictionId })
                .IsUnique()
                .HasDatabaseName("UX_UserPrediction_OnePerLeague");

            // One points total per user per league per race
            modelBuilder.Entity<WeeklyPoints>()
                .HasIndex(wp => new { wp.UserId, wp.LeagueId, wp.RaceId })
                .IsUnique()
                .HasDatabaseName("UX_WeeklyPoints");

            // One vote per voter per prediction
            modelBuilder.Entity<PredictionVote>()
                .HasIndex(pv => new { pv.UserPredictionId, pv.VoterId })
                .IsUnique()
                .HasDatabaseName("UX_PredictionVotes");

            // One vote window per league per race
            modelBuilder.Entity<PredictionVoteWindow>()
                .HasIndex(pvw => new { pvw.LeagueId, pvw.RaceId })
                .IsUnique()
                .HasDatabaseName("UX_PredictionVoteWindows");

            // Configure cascade delete restrictions to avoid multiple cascade paths
            modelBuilder.Entity<PredictionVote>()
                .HasOne(pv => pv.Voter)
                .WithMany()
                .HasForeignKey(pv => pv.VoterId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PredictionReport>()
                .HasOne(pr => pr.Reporter)
                .WithMany()
                .HasForeignKey(pr => pr.ReporterId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserPrediction>()
                .HasOne(up => up.League)
                .WithMany()
                .HasForeignKey(up => up.LeagueId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WeeklyPoints>()
                .HasOne(wp => wp.League)
                .WithMany()
                .HasForeignKey(wp => wp.LeagueId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
