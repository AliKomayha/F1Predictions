using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace F1Predictions.Models
{
    public class Session
    {
        public int Id { get; set; }

        [Required]
        public int RaceId { get; set; }

        [Required]
        public int ChampionshipId { get; set; }

        [Required]
        public string Type { get; set; } // FP1, FP2, Q1, Q2, Q3, Sprint, Race

        [Required]
        public DateTime DateTime { get; set; }

        public int? Laps { get; set; }

        // Navigation
        [ForeignKey("RaceId")]
        public Race Race { get; set; }

        [ForeignKey("ChampionshipId")]
        public Championship Championship { get; set; }

        public ICollection<SessionResult> SessionResults { get; set; }
    }
}
