using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Predictions.Models
{
    [Table("WeeklyPredictions")]
    public class WeeklyPrediction
    {
        public int Id { get; set; }

        [Required]
        public int RaceId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PredictionType { get; set; } = null!; // Pole, P1, P2, P3, SprintPole, SprintWinner, GoodSurprise, BigFlop, Crazy, Custom

        [Required]
        [MaxLength(20)]
        public string TargetType { get; set; } = null!; // Driver, Team, UserText

        [MaxLength(500)]
        public string? AdminDefinedText { get; set; }

        public int PointsAvailable { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("RaceId")]
        public Race Race { get; set; } = null!;

        public ICollection<UserPrediction> UserPredictions { get; set; } = new List<UserPrediction>();
    }
}
