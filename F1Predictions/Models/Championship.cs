using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace F1Predictions.Models
{
    public class Championship
    {
        public int Id { get; set; }
        [Column("year")]
        public int Year { get; set; }
        [Column("name")]
        public required string Name { get; set; }


        // Navigation
        //public ICollection<Race> Races { get; set; }
        //public ICollection<Session> Sessions { get; set; }
        //public ICollection<DriverTeam> DriverTeams { get; set; }
        //public ICollection<PointsSystem> PointsRules { get; set; }

    }
}
