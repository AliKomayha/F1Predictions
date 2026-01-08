using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace F1Predictions.Models
{
    public class SessionResult
    {
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }

        [Required]
        public int DriverId { get; set; }

        [Required]
        public int TeamId { get; set; }

        public int? Position { get; set; }
        public int? GridPosition { get; set; }

        [Required]
        public string Status { get; set; } // Finished, DNF, DNS, DSQ

        public decimal Points { get; set; } = 0;
        public long? TimeMs { get; set; }
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
