using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Predictions.Models
{
    [Table("PredictionVotes")]
    public class PredictionVote
    {
        public int Id { get; set; }

        [Required]
        public int UserPredictionId { get; set; }

        [Required]
        public int VoterId { get; set; }

        [Required]
        public bool Vote { get; set; } // true = Yes, false = No

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserPredictionId")]
        public UserPrediction UserPrediction { get; set; } = null!;

        [ForeignKey("VoterId")]
        public User Voter { get; set; } = null!;
    }
}
