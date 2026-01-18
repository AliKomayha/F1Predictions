using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace F1Predictions.Models
{
    [Table("PointsSystem")]
    public class PointsSystem
    {
        public int Id { get; set; }

        [Required]
        [Column("championship_id")]
        public int ChampionshipId { get; set; }

        [Required]
        [Column("session_type")]
        public string SessionType { get; set; } // Race, Sprint

        [Required]
        public int Position { get; set; }

        [Required]
        public decimal Points { get; set; }


        // Navigation
        [ForeignKey("ChampionshipId")]
        public Championship Championship { get; set; }
    }
}
