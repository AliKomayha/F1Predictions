using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace F1Predictions.Models
{
    public class Race
    {
        public int Id { get; set; }

        [Required]
        [Column("championship_id")]
        public int ChampionshipId { get; set; }

        [Required]
        [Column("track_id")]
        public int TrackId { get; set; }

        [Required]
        [Column("race_name")]
        public string RaceName { get; set; }

        [Required]
        [Column("round_number")]
        public int RoundNumber { get; set; }

        [Required]
        [Column("race_date")]
        public DateTime RaceDate { get; set; }

        [Required]
        public DateTimeOffset PredictionsLockedAt { get; set; }

        // Navigation
        [ForeignKey("ChampionshipId")]
        public Championship Championship { get; set; }

        [ForeignKey("TrackId")]
        public Track Track { get; set; }

        public ICollection<Session> Sessions { get; set; }
    }
}
