using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Predictions.Models
{
    [Table("UserPredictionPoints")]
    public class UserPredictionPoints
    {
        public int Id { get; set; }

        [Required]
        public int UserPredictionId { get; set; }

        [Required]
        public int PointsAwarded { get; set; }

        public bool IsManuallyAssigned { get; set; } = false;

        public int? AssignedById { get; set; } // Admin / Owner

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserPredictionId")]
        public UserPrediction UserPrediction { get; set; } = null!;

        [ForeignKey("AssignedById")]
        public User? AssignedBy { get; set; }
    }
}
