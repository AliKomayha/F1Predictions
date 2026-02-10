namespace F1Predictions.Models.DTOs
{
    public class LeagueMemberDto
    {
        public int UserId { get; set; }

        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        public string Role { get; set; } = null!; // Owner / Admin / Member

        public DateTime JoinedAt { get; set; }
    }

}
