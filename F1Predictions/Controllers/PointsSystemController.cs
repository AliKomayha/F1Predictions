using F1Predictions.Models;
using F1Predictions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace F1Predictions.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth")]
    public class PointsSystemController : Controller
    {
        private readonly PointsSystemService _pointsSystemService;
        private readonly ChampionshipsService _championshipsService;

        public PointsSystemController(
            PointsSystemService pointsSystemService,
            ChampionshipsService championshipsService)
        {
            _pointsSystemService = pointsSystemService;
            _championshipsService = championshipsService;
        }

        public async Task<IActionResult> Index(int? championshipId)
        {
            var championships = await _championshipsService.GetAll();
            ViewBag.Championships = new SelectList(championships, "Id", "Name", championshipId);
            ViewBag.SelectedChampionshipId = championshipId;

            if (championshipId.HasValue)
            {
                var points = await _pointsSystemService.GetByChampionship(championshipId.Value);
                ViewBag.SelectedChampionship = championships.FirstOrDefault(c => c.Id == championshipId.Value);
                
                // Group by session type for display
                ViewBag.RacePoints = points.Where(p => p.SessionType == "Race").OrderBy(p => p.Position).ToList();
                ViewBag.SprintPoints = points.Where(p => p.SessionType == "Sprint").OrderBy(p => p.Position).ToList();

                return View(points);
            }

            return View(new List<PointsSystem>());
        }

        public async Task<IActionResult> Details(int id)
        {
            var entry = await _pointsSystemService.GetById(id);
            if (entry == null)
                return NotFound();
            return View(entry);
        }

        public async Task<IActionResult> Create(int? championshipId)
        {
            await PopulateDropdowns(championshipId);
            
            var model = new PointsSystem();
            if (championshipId.HasValue)
                model.ChampionshipId = championshipId.Value;
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PointsSystem pointsSystem)
        {
            ModelState.Remove("Championship");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(pointsSystem.ChampionshipId);
                return View(pointsSystem);
            }

            await _pointsSystemService.Create(pointsSystem);
            return RedirectToAction(nameof(Index), new { championshipId = pointsSystem.ChampionshipId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var entry = await _pointsSystemService.GetById(id);
            if (entry == null)
                return NotFound();

            await PopulateDropdowns(entry.ChampionshipId);
            return View(entry);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PointsSystem pointsSystem)
        {
            ModelState.Remove("Championship");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(pointsSystem.ChampionshipId);
                return View(pointsSystem);
            }

            await _pointsSystemService.Update(pointsSystem);
            return RedirectToAction(nameof(Index), new { championshipId = pointsSystem.ChampionshipId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int? championshipId)
        {
            await _pointsSystemService.Delete(id);
            return RedirectToAction(nameof(Index), new { championshipId });
        }

        [HttpPost]
        public async Task<IActionResult> InitializeDefault(int championshipId)
        {
            await _pointsSystemService.InitializeDefaultPoints(championshipId);
            return RedirectToAction(nameof(Index), new { championshipId });
        }

        private async Task PopulateDropdowns(int? championshipId = null)
        {
            var championships = await _championshipsService.GetAll();
            ViewBag.Championships = new SelectList(
                championships.Select(c => new { c.Id, Name = $"{c.Name} ({c.Year})" }),
                "Id", "Name", championshipId);

            ViewBag.SessionTypes = new SelectList(PointsSystemService.GetPointsSessionTypes());
        }
    }
}
