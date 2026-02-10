using System.ComponentModel.DataAnnotations;

namespace F1Predictions.Models.DTOs
{
    /// <summary>
    /// A weekly prediction card with the user's existing pick (if any).
    /// </summary>
    public class RacePredictionDto
    {
        public int WeeklyPredictionId { get; set; }
        public string PredictionType { get; set; } = string.Empty;
        public string? AdminDefinedText { get; set; }
        public string AllowedTargetTypes { get; set; } = string.Empty;

        // User's existing pick for this prediction (null if not yet predicted)
        public UserPredictionDto? UserPick { get; set; }
    }

    /// <summary>
    /// The user's submitted prediction details.
    /// </summary>
    public class UserPredictionDto
    {
        public int Id { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public int? DriverId { get; set; }
        public string? DriverName { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public string? Text { get; set; }
        public bool IsLocked { get; set; }
    }

    /// <summary>
    /// Request to submit or update a prediction.
    /// </summary>
    public class SubmitPredictionRequest
    {
        [Required]
        public int WeeklyPredictionId { get; set; }

        [Required]
        public int LeagueId { get; set; }

        [Required]
        [MaxLength(20)]
        public string TargetType { get; set; } = null!; // Driver, Team, Text

        public int? DriverId { get; set; }
        public int? TeamId { get; set; }

        [MaxLength(500)]
        public string? Text { get; set; }
    }

    /// <summary>
    /// A driver option for dropdown selection.
    /// </summary>
    public class DriverOptionDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int ChampionshipNumber { get; set; }
        public string TeamName { get; set; } = string.Empty;
    }

    /// <summary>
    /// A team option for dropdown selection.
    /// </summary>
    public class TeamOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Available races for prediction (for a championship).
    /// </summary>
    public class RaceOptionDto
    {
        public int Id { get; set; }
        public string RaceName { get; set; } = string.Empty;
        public int RoundNumber { get; set; }
        public DateTime RaceDate { get; set; }
        public string TrackName { get; set; } = string.Empty;
    }
}
