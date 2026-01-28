using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Predictions.Models
{
    [Table("PredictionReports")]
    public class PredictionReport
    {
        public int Id { get; set; }

        [Required]
        public int UserPredictionId { get; set; }

        [Required]
        public int ReporterId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = null!;

        public bool IsResolved { get; set; } = false;

        public int? ResolvedByAdminId { get; set; }

        [MaxLength(500)]
        public string? ResolutionNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserPredictionId")]
        public UserPrediction UserPrediction { get; set; } = null!;

        [ForeignKey("ReporterId")]
        public User Reporter { get; set; } = null!;

        [ForeignKey("ResolvedByAdminId")]
        public User? ResolvedByAdmin { get; set; }
    }
}
