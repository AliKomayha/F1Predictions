using F1Predictions.Models.DTOs;
using F1Predictions.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictions.Controllers
{
    public class AdminSetupController : Controller
    {
        private readonly AdminAuthService _adminAuthService;

        public AdminSetupController(AdminAuthService adminAuthService)
        {
            _adminAuthService = adminAuthService;
        }

        // GET: AdminSetup/Create
        public async Task<IActionResult> Create()
        {
            bool adminExists = await _adminAuthService.AnyAdminExistsAsync();
            ViewBag.AdminExists = adminExists;
            return View();
        }

        // POST: AdminSetup/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAdminDto dto)
        {
            bool adminExists = await _adminAuthService.AnyAdminExistsAsync();
            if (adminExists)
            {
                ViewBag.AdminExists = true;
                return View(dto);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AdminExists = false;
                return View(dto);
            }

            var result = await _adminAuthService.CreateAdminAsync(dto);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Create));
            }

            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.AdminExists = false;
            return View(dto);
        }
    }
}
