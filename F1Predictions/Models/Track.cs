using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace F1Predictions.Models
{
    public class Track
    {
        [ValidateNever]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Country { get; set; }

        public string City { get; set; }

        // Navigation
        //public ICollection<Race> Races { get; set; }
    }
}
