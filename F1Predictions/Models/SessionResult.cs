using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace F1Predictions.Models
{
    public class SessionResult
    {
        public int Id { get; set; }

        [Required]
        [Column("session_id")]
        public int SessionId { get; set; }

        [Required]
        [Column("driver_id")]
        public int DriverId { get; set; }

        [Required]
        [Column("team_id")]
        public int TeamId { get; set; }

        public int? Position { get; set; }

        [Column("grid_position")]
        public int? GridPosition { get; set; }

        [Required]
        public string Status { get; set; } // Finished, DNF, DNS, DSQ

        public decimal Points { get; set; } = 0;

        [Column("time_ms")]
        public long? TimeMs { get; set; }

        [Column("fastest_lap")]
        public bool FastestLap { get; set; } = false;

        // Navigation
        [ForeignKey("SessionId")]
        public Session Session { get; set; }

        [ForeignKey("DriverId")]
        public Driver Driver { get; set; }

        [ForeignKey("TeamId")]
        public Team Team { get; set; }
    }
}
