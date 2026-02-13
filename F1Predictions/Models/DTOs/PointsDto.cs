namespace F1Predictions.Models.DTOs
{
    public class PointsGrantResultDto
    {
        public int RaceId { get; set; }
        public int TotalPredictionsChecked { get; set; }
        public int CorrectPredictions { get; set; }
        public int PointsAwarded { get; set; }
        public string NewRaceState { get; set; } = null!;
        public List<PointDetailDto> Details { get; set; } = new();
    }

    public class PointDetailDto
    {
        public string PredictionType { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string LeagueName { get; set; } = null!;
        public string PredictedDriver { get; set; } = null!;
        public string ActualDriver { get; set; } = null!;
        public bool IsCorrect { get; set; }
    }
}
