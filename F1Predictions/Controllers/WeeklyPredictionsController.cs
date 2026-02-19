using F1Predictions.Models;
using F1Predictions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace F1Predictions.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth")]
    public class WeeklyPredictionsController : Controller
    {
        private readonly WeeklyPredictionsService _weeklyPredictionsService;
        private readonly RacesService _racesService;

        public WeeklyPredictionsController(
            WeeklyPredictionsService weeklyPredictionsService,
            RacesService racesService)
        {
            _weeklyPredictionsService = weeklyPredictionsService;
            _racesService = racesService;
        }

        public async Task<IActionResult> Index(int? raceId)
        {
            var races = await _racesService.GetAll();
            ViewBag.Races = new SelectList(
                races.Select(r => new { r.Id, Name = $"{r.RaceName} ({r.Championship.Year})" }),
                "Id", "Name", raceId);
            ViewBag.SelectedRaceId = raceId;

            if (raceId.HasValue)
            {
                var predictions = await _weeklyPredictionsService.GetByRaceAsync(raceId.Value);
                ViewBag.SelectedRace = races.FirstOrDefault(r => r.Id == raceId.Value);
                return View(predictions);
            }

            return View(new List<WeeklyPrediction>());
        }

        public async Task<IActionResult> Details(int id)
        {
            var prediction = await _weeklyPredictionsService.GetByIdAsync(id);
            if (prediction == null)
                return NotFound();
            return View(prediction);
        }

        public async Task<IActionResult> Create(int? raceId)
        {
            await PopulateDropdowns(raceId);

            var model = new WeeklyPrediction();
            if (raceId.HasValue)
                model.RaceId = raceId.Value;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(WeeklyPrediction prediction)
        {
            ModelState.Remove("Race");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(prediction.RaceId);
                return View(prediction);
            }

            await _weeklyPredictionsService.CreateAsync(prediction);
            return RedirectToAction(nameof(Index), new { raceId = prediction.RaceId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var prediction = await _weeklyPredictionsService.GetByIdAsync(id);
            if (prediction == null)
                return NotFound();

            await PopulateDropdowns(prediction.RaceId);
            return View(prediction);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(WeeklyPrediction prediction)
        {
            ModelState.Remove("Race");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(prediction.RaceId);
                return View(prediction);
            }

            await _weeklyPredictionsService.UpdateAsync(prediction);
            return RedirectToAction(nameof(Index), new { raceId = prediction.RaceId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int? raceId)
        {
            await _weeklyPredictionsService.DeleteAsync(id);
            return RedirectToAction(nameof(Index), new { raceId });
        }

        [HttpPost]
        public async Task<IActionResult> InitializeDefault(int raceId)
        {
            await _weeklyPredictionsService.InitializeDefaultAsync(raceId);
            return RedirectToAction(nameof(Index), new { raceId });
        }

        private async Task PopulateDropdowns(int? raceId = null)
        {
            var races = await _racesService.GetAll();
            ViewBag.Races = new SelectList(
                races.Select(r => new { r.Id, Name = $"{r.RaceName} ({r.Championship.Year})" }),
                "Id", "Name", raceId);

            ViewBag.PredictionTypes = new SelectList(WeeklyPredictionsService.GetPredictionTypes());
            ViewBag.AllowedTargetTypes = new SelectList(WeeklyPredictionsService.GetAllowedTargetTypeOptions());
        }
    }
}
