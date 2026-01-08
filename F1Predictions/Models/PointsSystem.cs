using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace F1Predictions.Models
{
    public class PointsSystem
    {
        public int Id { get; set; }

        [Required]
        public int ChampionshipId { get; set; }

        [Required]
        public string SessionType { get; set; } // Race, Sprint

        [Required]
        public int Position { get; set; }

        [Required]
        public decimal Points { get; set; }

        public bool FastestLapBonus { get; set; } = false;

        // Navigation
        [ForeignKey("ChampionshipId")]
        public Championship Championship { get; set; }
    }
}
