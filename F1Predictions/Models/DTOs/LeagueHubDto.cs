namespace F1Predictions.Models.DTOs
{
    /// <summary>
    /// Current/upcoming race info for a league.
    /// </summary>
    public class CurrentRaceDto
    {
        public int Id { get; set; }
        public string RaceName { get; set; } = string.Empty;
        public int RoundNumber { get; set; }
        public int TotalRounds { get; set; }
        public DateTime RaceDate { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public DateTimeOffset PredictionsLockedAt { get; set; }
        public bool ArePredictionsLocked { get; set; }
        public string RaceState { get; set; } = "PredictionsOpen";
        public bool IsVotingOpen { get; set; }
        public DateTime? VotingClosesAt { get; set; }
    }

    /// <summary>
    /// Full league hub summary for a specific race.
    /// </summary>
    public class LeagueSummaryDto
    {
        public int LeagueId { get; set; }
        public string LeagueName { get; set; } = string.Empty;
        public CurrentRaceDto CurrentRace { get; set; } = null!;
        public int UserTotalPoints { get; set; }
        public int UserRacePoints { get; set; }
        public List<MemberStandingDto> Members { get; set; } = new();
    }

    /// <summary>
    /// A member in the standings list.
    /// </summary>
    public class MemberStandingDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int Rank { get; set; }
        public bool HasUndoneVotes { get; set; } // green dot indicator
    }

    /// <summary>
    /// Enhanced prediction with points and vote info for member view.
    /// </summary>
    public class MemberPredictionDto
    {
        public int WeeklyPredictionId { get; set; }
        public string PredictionType { get; set; } = string.Empty;
        public string? AdminDefinedText { get; set; }
        public string AllowedTargetTypes { get; set; } = string.Empty;

        // User's pick
        public UserPredictionDto? UserPick { get; set; }

        // Points info
        public int? PointsAwarded { get; set; } // null = not yet resolved
        public string PointsStatus { get; set; } = "Pending"; // Correct, Wrong, VotingInProgress, Pending

        // Vote info (for votable types)
        public bool IsVotable { get; set; }
        public int YesVotes { get; set; }
        public int NoVotes { get; set; }
        public bool? MyVote { get; set; } // null = haven't voted
        public bool IsVoteResolved { get; set; }
    }
}
