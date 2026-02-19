using F1Predictions.Models;
using F1Predictions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictions.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth")]
    public class DriversController : Controller
    {
        private readonly DriversService _driversService;

        public DriversController(DriversService driversService)
        {
            _driversService = driversService;
        }

        // GET: DriversController
        public async Task<IActionResult> Index()
        {
           var drivers = await _driversService.GetAll();

            return View(drivers);
        }


        // GET: DriversController/Details/5
        public async Task<IActionResult> Details(int id) 
        {
            var driver = await _driversService.GetById(id);
            if (driver == null)
                return NotFound();

            return View(driver);
        }

        // GET: DriversController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DriversController/Create
        [HttpPost]
      
        public async Task<IActionResult> Create(Driver driver)
        {
            if (!ModelState.IsValid)
                return View(driver);

            await _driversService.Create(driver);
            return RedirectToAction(nameof(Index));
        }

        // GET: DriversController/Edit/5
     
        public async Task<IActionResult> Edit(int id)
        {
            var driver = await _driversService.GetById(id);
            if (driver == null)
                return NotFound();

            return View(driver);
        }

        // POST: DriversController/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(Driver driver)
        {
            if (!ModelState.IsValid)
                return View(driver);

            await _driversService.Update(driver);
            return RedirectToAction(nameof(Index));
        }


        // POST: DriversController/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _driversService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
