using F1Predictions.Models;
using F1Predictions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace F1Predictions.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth")]
    public class DriverTeamsController : Controller
    {
        private readonly DriverTeamsService _driverTeamsService;
        private readonly DriversService _driversService;
        private readonly TeamsService _teamsService;
        private readonly ChampionshipsService _championshipsService;

        public DriverTeamsController(
            DriverTeamsService driverTeamsService,
            DriversService driversService,
            TeamsService teamsService,
            ChampionshipsService championshipsService)
        {
            _driverTeamsService = driverTeamsService;
            _driversService = driversService;
            _teamsService = teamsService;
            _championshipsService = championshipsService;
        }

        public async Task<IActionResult> Index(int? championshipId)
        {
            var championships = await _championshipsService.GetAll();
            ViewBag.Championships = new SelectList(championships, "Id", "Name", championshipId);
            ViewBag.SelectedChampionshipId = championshipId;

            if (championshipId.HasValue)
            {
                var driverTeams = await _driverTeamsService.GetByChampionship(championshipId.Value);
                
                // Group by TeamId (not Team object) to properly combine drivers from same team
                var lineupByTeam = driverTeams
                    .GroupBy(dt => dt.TeamId)
                    .OrderBy(g => g.First().Team.DisplayName)
                    .ToDictionary(g => g.First().Team, g => g.ToList());
                
                ViewBag.LineupByTeam = lineupByTeam;
                ViewBag.SelectedChampionship = championships.FirstOrDefault(c => c.Id == championshipId.Value);
            }

            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            var driverTeam = await _driverTeamsService.GetById(id);
            if (driverTeam == null)
                return NotFound();
            return View(driverTeam);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(DriverTeam driverTeam)
        {
            // Remove navigation properties from validation
            ModelState.Remove("Driver");
            ModelState.Remove("Team");
            ModelState.Remove("Championship");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(driverTeam);
                return View(driverTeam);
            }

            await _driverTeamsService.Create(driverTeam);
            return RedirectToAction(nameof(Index), new { championshipId = driverTeam.ChampionshipId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var driverTeam = await _driverTeamsService.GetById(id);
            if (driverTeam == null)
                return NotFound();

            await PopulateDropdowns(driverTeam);
            return View(driverTeam);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(DriverTeam driverTeam)
        {
            ModelState.Remove("Driver");
            ModelState.Remove("Team");
            ModelState.Remove("Championship");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(driverTeam);
                return View(driverTeam);
            }

            await _driverTeamsService.Update(driverTeam);
            return RedirectToAction(nameof(Index), new { championshipId = driverTeam.ChampionshipId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int? championshipId)
        {
            await _driverTeamsService.Delete(id);
            return RedirectToAction(nameof(Index), new { championshipId });
        }

        private async Task PopulateDropdowns(DriverTeam? driverTeam = null)
        {
            var drivers = await _driversService.GetAll();
            var teams = await _teamsService.GetAll();
            var championships = await _championshipsService.GetAll();

            ViewBag.Drivers = new SelectList(
                drivers.Select(d => new { d.Id, Name = $"{d.FirstName} {d.LastName} (#{d.ChampionshipNumber})" }),
                "Id", "Name", driverTeam?.DriverId);
            
            ViewBag.Teams = new SelectList(
                teams.Select(t => new { t.Id, t.DisplayName }),
                "Id", "DisplayName", driverTeam?.TeamId);
            
            ViewBag.Championships = new SelectList(
                championships.Select(c => new { c.Id, Name = $"{c.Name} ({c.Year})" }),
                "Id", "Name", driverTeam?.ChampionshipId);
        }
    }
}
