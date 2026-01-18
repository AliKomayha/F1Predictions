using F1Predictions.Models;
using F1Predictions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace F1Predictions.Controllers
{
    public class SessionResultsController : Controller
    {
        private readonly SessionResultsService _sessionResultsService;
        private readonly SessionsService _sessionsService;
        private readonly DriversService _driversService;
        private readonly TeamsService _teamsService;
        private readonly DriverTeamsService _driverTeamsService;

        public SessionResultsController(
            SessionResultsService sessionResultsService,
            SessionsService sessionsService,
            DriversService driversService,
            TeamsService teamsService,
            DriverTeamsService driverTeamsService)
        {
            _sessionResultsService = sessionResultsService;
            _sessionsService = sessionsService;
            _driversService = driversService;
            _teamsService = teamsService;
            _driverTeamsService = driverTeamsService;
        }

        public async Task<IActionResult> Index(int sessionId)
        {
            var session = await _sessionsService.GetById(sessionId);
            if (session == null)
                return NotFound();

            var results = await _sessionResultsService.GetBySession(sessionId);
            ViewBag.Session = session;
            ViewBag.SessionId = sessionId;

            return View(results);
        }

        public async Task<IActionResult> Details(int id)
        {
            var result = await _sessionResultsService.GetById(id);
            if (result == null)
                return NotFound();
            return View(result);
        }

        public async Task<IActionResult> Create(int sessionId)
        {
            var session = await _sessionsService.GetById(sessionId);
            if (session == null)
                return NotFound();

            await PopulateDropdowns(session.ChampionshipId);
            ViewBag.Session = session;

            var model = new SessionResult
            {
                SessionId = sessionId,
                Status = "Finished"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SessionResult result)
        {
            ModelState.Remove("Session");
            ModelState.Remove("Driver");
            ModelState.Remove("Team");

            if (!ModelState.IsValid)
            {
                var session = await _sessionsService.GetById(result.SessionId);
                await PopulateDropdowns(session?.ChampionshipId ?? 0);
                ViewBag.Session = session;
                return View(result);
            }

            // Calculate points based on position and session type
            var session2 = await _sessionsService.GetById(result.SessionId);
            if (session2 != null && result.Position.HasValue)
            {
                result.Points = await _sessionResultsService.CalculatePoints(
                    session2.ChampionshipId,
                    session2.Type,
                    result.Position.Value
                    );
            }

            await _sessionResultsService.Create(result);
            return RedirectToAction(nameof(Index), new { sessionId = result.SessionId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var result = await _sessionResultsService.GetById(id);
            if (result == null)
                return NotFound();

            var session = await _sessionsService.GetById(result.SessionId);
            await PopulateDropdowns(session?.ChampionshipId ?? 0, result);
            ViewBag.Session = session;

            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SessionResult result)
        {
            ModelState.Remove("Session");
            ModelState.Remove("Driver");
            ModelState.Remove("Team");

            if (!ModelState.IsValid)
            {
                var session = await _sessionsService.GetById(result.SessionId);
                await PopulateDropdowns(session?.ChampionshipId ?? 0, result);
                ViewBag.Session = session;
                return View(result);
            }

            // Recalculate points
            var session2 = await _sessionsService.GetById(result.SessionId);
            if (session2 != null && result.Position.HasValue)
            {
                result.Points = await _sessionResultsService.CalculatePoints(
                    session2.ChampionshipId,
                    session2.Type,
                    result.Position.Value);
            }

            await _sessionResultsService.Update(result);
            return RedirectToAction(nameof(Index), new { sessionId = result.SessionId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int sessionId)
        {
            await _sessionResultsService.Delete(id);
            return RedirectToAction(nameof(Index), new { sessionId });
        }

        private async Task PopulateDropdowns(int championshipId, SessionResult? result = null)
        {
            // Get drivers from the championship lineup
            var driverTeams = await _driverTeamsService.GetByChampionship(championshipId);
            
            ViewBag.Drivers = new SelectList(
                driverTeams.Select(dt => new { dt.DriverId, Name = $"{dt.Driver.FirstName} {dt.Driver.LastName} (#{dt.Driver.ChampionshipNumber})" }).Distinct(),
                "DriverId", "Name", result?.DriverId);

            ViewBag.Teams = new SelectList(
                driverTeams.Select(dt => dt.Team).DistinctBy(t => t.Id).Select(t => new { t.Id, t.DisplayName }),
                "Id", "DisplayName", result?.TeamId);

            ViewBag.Statuses = new SelectList(SessionResultsService.GetStatuses(), result?.Status ?? "Finished");
        }
    }
}
