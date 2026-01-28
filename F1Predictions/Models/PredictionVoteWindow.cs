using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Predictions.Models
{
    [Table("PredictionVoteWindows")]
    public class PredictionVoteWindow
    {
        public int Id { get; set; }

        [Required]
        public int LeagueId { get; set; }

        [Required]
        public int RaceId { get; set; }

        [Required]
        public DateTime OpensAt { get; set; }

        [Required]
        public DateTime ClosesAt { get; set; }

        // Navigation properties
        [ForeignKey("LeagueId")]
        public League League { get; set; } = null!;

        [ForeignKey("RaceId")]
        public Race Race { get; set; } = null!;
    }
}
