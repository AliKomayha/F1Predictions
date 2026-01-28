using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Predictions.Models
{
    [Table("RaceStates")]
    public class RaceState
    {
        [Key]
        public int RaceId { get; set; }

        [Required]
        [MaxLength(30)]
        public string State { get; set; } = null!;
        // PredictionsOpen, PredictionsLocked, RaceCompleted, VotingOpen, VotingClosed, Finalized

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("RaceId")]
        public Race Race { get; set; } = null!;
    }
}
