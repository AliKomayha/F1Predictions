using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace F1Predictions.Models
{
    public class Race
    {
        public int Id { get; set; }

        [Required]
        public int ChampionshipId { get; set; }

        [Required]
        public int TrackId { get; set; }

        [Required]
        public string RaceName { get; set; }

        [Required]
        public int RoundNumber { get; set; }

        [Required]
        public DateTime RaceDate { get; set; }

        // Navigation
        [ForeignKey("ChampionshipId")]
        public Championship Championship { get; set; }

        [ForeignKey("TrackId")]
        public Track Track { get; set; }

        public ICollection<Session> Sessions { get; set; }
    }
}
