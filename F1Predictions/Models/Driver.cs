using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace F1Predictions.Models
{
    public class Driver
    {
        [ValidateNever]
        public int Id { get; set; }

        [Required]
        [Column("first_name")]
        public string FirstName { get; set; }

        [Required]
        [Column("last_name")]
        public string LastName { get; set; }

        [Column("championship_number")]
        public required int ChampionshipNumber { get; set; }

        public string Nationality { get; set; }

        //[ValidateNever]
        // Navigation
        //public ICollection<DriverTeam> DriverTeams { get; set; }
        //public ICollection<SessionResult> SessionResults { get; set; }


    }
}
