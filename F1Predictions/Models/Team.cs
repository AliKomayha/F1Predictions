using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Predictions.Models
{
    public class Team
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Column("displayName")]
        public string DisplayName { get; set; }

        [Column("base_country")]
        public string BaseCountry { get; set; }

        // Navigation
        //public ICollection<DriverTeam> DriverTeams { get; set; }
        //public ICollection<SessionResult> SessionResults { get; set; }

    }
}
