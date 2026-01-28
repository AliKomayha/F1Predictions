using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Predictions.Models
{
    [Table("PredictionDecisions")]
    public class PredictionDecision
    {
        public int Id { get; set; }

        [Required]
        public int UserPredictionId { get; set; }

        [Required]
        public int DecidedByAdminId { get; set; }

        [Required]
        public bool IsApproved { get; set; }

        [Required]
        public int PointsGranted { get; set; }

        [MaxLength(500)]
        public string? DecisionNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserPredictionId")]
        public UserPrediction UserPrediction { get; set; } = null!;

        [ForeignKey("DecidedByAdminId")]
        public User DecidedByAdmin { get; set; } = null!;
    }
}
