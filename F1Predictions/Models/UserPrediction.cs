using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Predictions.Models
{
    [Table("UserPredictions")]
    public class UserPrediction
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int LeagueId { get; set; }

        [Required]
        public int WeeklyPredictionId { get; set; }

        [Required]
        [MaxLength(20)]
        public string TargetType { get; set; } = null!; // Driver, Team, Text

        public int? DriverId { get; set; }

        public int? TeamId { get; set; }

        [MaxLength(500)]
        public string? Text { get; set; }

        public bool IsLocked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("LeagueId")]
        public League League { get; set; } = null!;

        [ForeignKey("WeeklyPredictionId")]
        public WeeklyPrediction WeeklyPrediction { get; set; } = null!;

        [ForeignKey("DriverId")]
        public Driver? Driver { get; set; }

        [ForeignKey("TeamId")]
        public Team? Team { get; set; }

        public ICollection<UserPredictionPoints> Points { get; set; } = new List<UserPredictionPoints>();
        public ICollection<PredictionVote> Votes { get; set; } = new List<PredictionVote>();
        public ICollection<PredictionDecision> Decisions { get; set; } = new List<PredictionDecision>();
        public ICollection<PredictionReport> Reports { get; set; } = new List<PredictionReport>();
    }
}
