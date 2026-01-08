using F1Predictions.Models;
using F1Predictions.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictions.Controllers
{
    public class TeamsController : Controller
    {
        private readonly TeamsService _teamsService;

        public TeamsController(TeamsService teamsService)
        {
            _teamsService = teamsService;
        }



        public async Task<IActionResult> Index()
        {
            var teams = await _teamsService.GetAll();
            return View(teams);
        }
        public async Task<IActionResult> Details(int id)
        {
            var team = await _teamsService.GetById(id);
            if (team == null)
                return NotFound();
            return View(team);
        }

        // GET: TeamsController/Create
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Team team)
        {
            if (!ModelState.IsValid)
                return View(team);

            await _teamsService.Create(team);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var team = await _teamsService.GetById(id);
            if (team == null)
                return NotFound();

            return View(team);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(Team team)
        {
            if (!ModelState.IsValid)
                return View(team);

            await _teamsService.Update(team);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _teamsService.Delete(id);
            return RedirectToAction(nameof(Index));
        }

    }
}
