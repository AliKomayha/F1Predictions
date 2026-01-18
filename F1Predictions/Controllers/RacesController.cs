using F1Predictions.Models;
using F1Predictions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace F1Predictions.Controllers
{
    public class RacesController : Controller
    {
        private readonly RacesService _racesService;
        private readonly ChampionshipsService _championshipsService;
        private readonly TracksService _tracksService;

        public RacesController(
            RacesService racesService,
            ChampionshipsService championshipsService,
            TracksService tracksService)
        {
            _racesService = racesService;
            _championshipsService = championshipsService;
            _tracksService = tracksService;
        }

        public async Task<IActionResult> Index(int? championshipId)
        {
            var championships = await _championshipsService.GetAll();
            ViewBag.Championships = new SelectList(championships, "Id", "Name", championshipId);
            ViewBag.SelectedChampionshipId = championshipId;

            List<Race> races;
            if (championshipId.HasValue)
            {
                races = await _racesService.GetByChampionship(championshipId.Value);
                ViewBag.SelectedChampionship = championships.FirstOrDefault(c => c.Id == championshipId.Value);
            }
            else
            {
                races = await _racesService.GetAll();
            }

            return View(races);
        }

        public async Task<IActionResult> Details(int id)
        {
            var race = await _racesService.GetById(id);
            if (race == null)
                return NotFound();
            return View(race);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Race race)
        {
            ModelState.Remove("Championship");
            ModelState.Remove("Track");
            ModelState.Remove("Sessions");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(race);
                return View(race);
            }

            await _racesService.Create(race);
            return RedirectToAction(nameof(Index), new { championshipId = race.ChampionshipId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var race = await _racesService.GetById(id);
            if (race == null)
                return NotFound();

            await PopulateDropdowns(race);
            return View(race);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Race race)
        {
            ModelState.Remove("Championship");
            ModelState.Remove("Track");
            ModelState.Remove("Sessions");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(race);
                return View(race);
            }

            await _racesService.Update(race);
            return RedirectToAction(nameof(Index), new { championshipId = race.ChampionshipId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int? championshipId)
        {
            await _racesService.Delete(id);
            return RedirectToAction(nameof(Index), new { championshipId });
        }

        private async Task PopulateDropdowns(Race? race = null)
        {
            var championships = await _championshipsService.GetAll();
            var tracks = await _tracksService.GetAll();

            ViewBag.Championships = new SelectList(
                championships.Select(c => new { c.Id, Name = $"{c.Name} ({c.Year})" }),
                "Id", "Name", race?.ChampionshipId);

            ViewBag.Tracks = new SelectList(
                tracks.Select(t => new { t.Id, Name = $"{t.Name} - {t.Country}" }),
                "Id", "Name", race?.TrackId);
        }
    }
}
