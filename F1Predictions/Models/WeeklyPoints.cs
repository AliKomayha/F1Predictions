using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Predictions.Models
{
    [Table("WeeklyPoints")]
    public class WeeklyPoints
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int LeagueId { get; set; }

        [Required]
        public int RaceId { get; set; }

        public int PointsTotal { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("LeagueId")]
        public League League { get; set; } = null!;

        [ForeignKey("RaceId")]
        public Race Race { get; set; } = null!;
    }
}
