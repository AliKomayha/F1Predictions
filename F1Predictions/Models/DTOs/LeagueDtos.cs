using System.ComponentModel.DataAnnotations;

namespace F1Predictions.Models.DTOs
{
    /// <summary>
    /// DTO for creating a new league.
    /// </summary>
    public class CreateLeagueDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// DTO for returning league information.
    /// </summary>
    public class LeagueDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int ChampionshipId { get; set; }
        public string ChampionshipName { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public string? InviteCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int MemberCount { get; set; }
    }

    /// <summary>
    /// DTO for joining a league by invite code.
    /// </summary>
    public class JoinLeagueDto
    {
        [Required]
        [MaxLength(20)]
        public string InviteCode { get; set; } = null!;
    }
}
