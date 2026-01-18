using F1Predictions.Models;
using F1Predictions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace F1Predictions.Controllers
{
    public class SessionsController : Controller
    {
        private readonly SessionsService _sessionsService;
        private readonly RacesService _racesService;
        private readonly ChampionshipsService _championshipsService;

        public SessionsController(
            SessionsService sessionsService,
            RacesService racesService,
            ChampionshipsService championshipsService)
        {
            _sessionsService = sessionsService;
            _racesService = racesService;
            _championshipsService = championshipsService;
        }

        public async Task<IActionResult> Index(int? raceId)
        {
            if (!raceId.HasValue)
            {
                // Show race selection
                var races = await _racesService.GetAll();
                return View("SelectRace", races);
            }

            var sessions = await _sessionsService.GetByRace(raceId.Value);
            var race = await _racesService.GetById(raceId.Value);
            ViewBag.Race = race;
            ViewBag.RaceId = raceId.Value;

            return View(sessions);
        }

        public async Task<IActionResult> Details(int id)
        {
            var session = await _sessionsService.GetById(id);
            if (session == null)
                return NotFound();
            return View(session);
        }

        public async Task<IActionResult> Create(int? raceId)
        {
            await PopulateDropdowns(raceId);
            
            var model = new Session();
            if (raceId.HasValue)
            {
                var race = await _racesService.GetById(raceId.Value);
                if (race != null)
                {
                    model.RaceId = raceId.Value;
                    model.ChampionshipId = race.ChampionshipId;
                    model.DateTime = race.RaceDate;
                }
            }
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Session session)
        {
            ModelState.Remove("Race");
            ModelState.Remove("Championship");
            ModelState.Remove("SessionResults");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(session.RaceId);
                return View(session);
            }

            await _sessionsService.Create(session);
            return RedirectToAction(nameof(Index), new { raceId = session.RaceId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var session = await _sessionsService.GetById(id);
            if (session == null)
                return NotFound();

            await PopulateDropdowns(session.RaceId);
            return View(session);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Session session)
        {
            ModelState.Remove("Race");
            ModelState.Remove("Championship");
            ModelState.Remove("SessionResults");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(session.RaceId);
                return View(session);
            }

            await _sessionsService.Update(session);
            return RedirectToAction(nameof(Index), new { raceId = session.RaceId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int? raceId)
        {
            await _sessionsService.Delete(id);
            return RedirectToAction(nameof(Index), new { raceId });
        }

        private async Task PopulateDropdowns(int? raceId = null)
        {
            var races = await _racesService.GetAll();
            ViewBag.Races = new SelectList(
                races.Select(r => new { r.Id, Name = $"R{r.RoundNumber} - {r.RaceName} ({r.Championship.Year})" }),
                "Id", "Name", raceId);

            ViewBag.SessionTypes = new SelectList(SessionsService.GetSessionTypes());
        }
    }
}
