using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Predictions.Models
{
    [Table("Leagues")]
    public class League
    {
        public int Id { get; set; }

        [Required]
        public int ChampionshipId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public int OwnerId { get; set; }

        public bool IsPublic { get; set; } = false;

        [MaxLength(20)]
        public string? InviteCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("ChampionshipId")]
        public Championship Championship { get; set; } = null!;

        [ForeignKey("OwnerId")]
        public User Owner { get; set; } = null!;

        public ICollection<LeagueMember> Members { get; set; } = new List<LeagueMember>();
    }
}

