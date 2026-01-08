using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace F1Predictions.Models
{
    public class DriverTeam
    {
        public int Id { get; set; }

        [Required]

        [Column("driver_id")]
        public int DriverId { get; set; }

        [Required]

        [Column("team_id")]
        public int TeamId { get; set; }

        [Required]

        [Column("championship_id")]
        public int ChampionshipId { get; set; }

        // Navigation
        [ForeignKey("DriverId")]
        public Driver Driver { get; set; }

        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        [ForeignKey("ChampionshipId")]
        public Championship Championship { get; set; }
    }
}
