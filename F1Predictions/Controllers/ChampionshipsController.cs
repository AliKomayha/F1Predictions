using F1Predictions.Models;
using F1Predictions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictions.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth")]
    public class ChampionshipsController : Controller
    {
        private readonly ChampionshipsService _championshipsService;

        public ChampionshipsController(ChampionshipsService championshipsService)
        {
            _championshipsService = championshipsService;
        }

        public async Task<IActionResult> Index()
        {
            var championships = await _championshipsService.GetAll();
            return View(championships);
        }

        public async Task<IActionResult> Details(int id)
        {
            var championship = await _championshipsService.GetById(id);
            if (championship == null)
                return NotFound();
            return View(championship);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Championship championship)
        {
            if (!ModelState.IsValid)
                return View(championship);

            await _championshipsService.Create(championship);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var championship = await _championshipsService.GetById(id);
            if (championship == null)
                return NotFound();

            return View(championship);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Championship championship)
        {
            if (!ModelState.IsValid)
                return View(championship);

            await _championshipsService.Update(championship);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _championshipsService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
