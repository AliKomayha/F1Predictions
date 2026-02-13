namespace F1Predictions.Models.DTOs
{
    public class VotablePredictionDto
    {
        public int UserPredictionId { get; set; }
        public string PredictionType { get; set; } = null!;
        public string TargetType { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public int UserId { get; set; }
        public string? DriverName { get; set; }
        public string? TeamName { get; set; }
        public string? Text { get; set; }
        public int YesVotes { get; set; }
        public int NoVotes { get; set; }
        public bool? MyVote { get; set; } // null = not voted, true = yes, false = no
        public bool IsResolved { get; set; }
        public bool? WasApproved { get; set; } // null = not resolved yet
        public int PointsValue { get; set; } // 1 for Driver/Text, 2 for Team
    }

    public class CastVoteRequest
    {
        public int UserPredictionId { get; set; }
        public bool Vote { get; set; } // true = Yes, false = No
    }

    public class VoteResultDto
    {
        public bool VoteRecorded { get; set; }
        public int YesVotes { get; set; }
        public int NoVotes { get; set; }
        public bool WasAutoResolved { get; set; }
        public bool? Resolution { get; set; } // true = approved, false = denied, null = pending
    }

    public class AdminDecideRequest
    {
        public int UserPredictionId { get; set; }
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }

    public class ReportPredictionRequest
    {
        public int UserPredictionId { get; set; }
        public string Reason { get; set; } = null!;
    }

    public class ResolveReportRequest
    {
        public int ReportId { get; set; }
        public string? Note { get; set; }
    }

    public class FinalizeResultDto
    {
        public int TotalPredictions { get; set; }
        public int ApprovedByVotes { get; set; }
        public int DeniedByVotes { get; set; }
        public int NoVotesCast { get; set; }
        public int TiedForAdmin { get; set; }
        public int AlreadyResolved { get; set; }
    }

    public class VotingStatusDto
    {
        public bool IsVotingOpen { get; set; }
        public DateTime? OpensAt { get; set; }
        public DateTime? ClosesAt { get; set; }
        public string RaceState { get; set; } = null!;
    }
}
