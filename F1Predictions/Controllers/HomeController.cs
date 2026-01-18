using System.Diagnostics;
using F1Predictions.Data;
using F1Predictions.Models;
using F1Predictions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly ChampionshipsService _championshipsService;

        public HomeController(
            ILogger<HomeController> logger, 
            AppDbContext context,
            ChampionshipsService championshipsService)
        {
            _logger = logger;
            _context = context;
            _championshipsService = championshipsService;
        }

        public async Task<IActionResult> Index(int? championshipId)
        {
            // Get all championships for dropdown
            var championships = await _championshipsService.GetAll();
            ViewBag.Championships = new SelectList(championships, "Id", "Name", championshipId);

            // If no championship selected, get the latest one
            if (!championshipId.HasValue && championships.Any())
            {
                championshipId = championships.OrderByDescending(c => c.Year).First().Id;
            }

            ViewBag.SelectedChampionshipId = championshipId;
            ViewBag.SelectedChampionship = championships.FirstOrDefault(c => c.Id == championshipId);

            // Dashboard Statistics
            ViewBag.TotalDrivers = await _context.Drivers.CountAsync();
            ViewBag.TotalTeams = await _context.Teams.CountAsync();
            ViewBag.TotalRaces = championshipId.HasValue 
                ? await _context.Races.CountAsync(r => r.ChampionshipId == championshipId)
                : await _context.Races.CountAsync();
            ViewBag.TotalSessions = championshipId.HasValue
                ? await _context.Sessions.CountAsync(s => s.ChampionshipId == championshipId)
                : await _context.Sessions.CountAsync();

            // Calculate Championship Standings
            if (championshipId.HasValue)
            {
                var standings = await CalculateStandings(championshipId.Value);
                ViewBag.DriverStandings = standings.DriverStandings;
                ViewBag.ConstructorStandings = standings.ConstructorStandings;

                // Get upcoming races
                ViewBag.UpcomingRaces = await _context.Races
                    .Include(r => r.Track)
                    .Where(r => r.ChampionshipId == championshipId && r.RaceDate >= DateTime.Today)
                    .OrderBy(r => r.RaceDate)
                    .Take(3)
                    .ToListAsync();

                // Get recent results
                ViewBag.RecentResults = await _context.SessionResults
                    .Include(sr => sr.Session)
                        .ThenInclude(s => s.Race)
                    .Include(sr => sr.Driver)
                    .Include(sr => sr.Team)
                    .Where(sr => sr.Session.ChampionshipId == championshipId 
                        && (sr.Session.Type == "Race" || sr.Session.Type == "Sprint")
                        && sr.Position == 1)
                    .OrderByDescending(sr => sr.Session.DateTime)
                    .Take(5)
                    .ToListAsync();
            }

            return View();
        }

        private async Task<(List<DriverStandingViewModel> DriverStandings, List<ConstructorStandingViewModel> ConstructorStandings)> CalculateStandings(int championshipId)
        {
            // Get all session results for Race and Sprint sessions in this championship
            var results = await _context.SessionResults
                .Include(sr => sr.Session)
                .Include(sr => sr.Driver)
                .Include(sr => sr.Team)
                .Where(sr => sr.Session.ChampionshipId == championshipId 
                    && (sr.Session.Type == "Race" || sr.Session.Type == "Sprint"))
                .ToListAsync();

            // Driver Standings - group by driver and sum points
            var driverStandings = results
                .GroupBy(r => new { r.DriverId, r.Driver.FirstName, r.Driver.LastName, r.Driver.ChampionshipNumber })
                .Select(g => new DriverStandingViewModel
                {
                    DriverId = g.Key.DriverId,
                    DriverName = $"{g.Key.FirstName} {g.Key.LastName}",
                    DriverNumber = g.Key.ChampionshipNumber,
                    TeamName = g.OrderByDescending(x => x.Id).First().Team?.DisplayName ?? "N/A",
                    TotalPoints = g.Sum(r => r.Points),
                    Wins = g.Count(r => r.Position == 1 && r.Session.Type == "Race"),
                    Podiums = g.Count(r => r.Position <= 3 && r.Session.Type == "Race"),
                    RaceCount = g.Count(r => r.Session.Type == "Race")
                })
                .OrderByDescending(d => d.TotalPoints)
                .ThenByDescending(d => d.Wins)
                .ToList();

            // Assign positions
            for (int i = 0; i < driverStandings.Count; i++)
                driverStandings[i].Position = i + 1;

            // Constructor Standings - group by team and sum points
            var constructorStandings = results
                .GroupBy(r => new { r.TeamId, r.Team.DisplayName })
                .Select(g => new ConstructorStandingViewModel
                {
                    TeamId = g.Key.TeamId,
                    TeamName = g.Key.DisplayName,
                    TotalPoints = g.Sum(r => r.Points),
                    Wins = g.Count(r => r.Position == 1 && r.Session.Type == "Race"),
                    RaceCount = g.Select(r => r.Session.RaceId).Distinct().Count()
                })
                .OrderByDescending(c => c.TotalPoints)
                .ThenByDescending(c => c.Wins)
                .ToList();

            // Assign positions
            for (int i = 0; i < constructorStandings.Count; i++)
                constructorStandings[i].Position = i + 1;

            return (driverStandings, constructorStandings);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    // ViewModels for Standings
    public class DriverStandingViewModel
    {
        public int Position { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public int DriverNumber { get; set; }
        public string TeamName { get; set; }
        public decimal TotalPoints { get; set; }
        public int Wins { get; set; }
        public int Podiums { get; set; }
        public int RaceCount { get; set; }
    }

    public class ConstructorStandingViewModel
    {
        public int Position { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public decimal TotalPoints { get; set; }
        public int Wins { get; set; }
        public int RaceCount { get; set; }
    }
}
