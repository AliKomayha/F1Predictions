using F1Predictions.Models;
using F1Predictions.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictions.Controllers
{
    public class TracksController : Controller
    {
        private readonly TracksService _tracksService;

        public TracksController(TracksService tracksService)
        {
            _tracksService = tracksService;
        }

        public async Task<IActionResult> Index()
        {
            var tracks = await _tracksService.GetAll();
            return View(tracks);
        }

        public async Task<IActionResult> Details(int id)
        {
            var track = await _tracksService.GetById(id);
            if (track == null)
                return NotFound();
            return View(track);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Track track)
        {
            if (!ModelState.IsValid)
                return View(track);

            await _tracksService.Create(track);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var track = await _tracksService.GetById(id);
            if (track == null)
                return NotFound();

            return View(track);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Track track)
        {
            if (!ModelState.IsValid)
                return View(track);

            await _tracksService.Update(track);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _tracksService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
