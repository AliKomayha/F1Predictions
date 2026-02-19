using System.Security.Claims;
using F1Predictions.Models.DTOs;
using F1Predictions.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictions.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth")]
    public class AdminAuthController : Controller
    {
        private readonly AdminAuthService _adminAuthService;
        private const string AuthScheme = "AdminCookieAuth";

        public AdminAuthController(AdminAuthService adminAuthService)
        {
            _adminAuthService = adminAuthService;
        }

        // GET: AdminAuth/Login
        [AllowAnonymous]
        public IActionResult Login()
        {
            // If already logged in, redirect to admin actions
            if (User.Identities.Any(i => i.AuthenticationType == "AdminCookieAuth" && i.IsAuthenticated))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: AdminAuth/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _adminAuthService.LoginAsync(dto);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(dto);
            }

            var admin = result.Data!;

            // Build claims
            var claims = new List<Claim>
            {
                new Claim("AdminId", admin.Id.ToString()),
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, admin.Role),
                new Claim(ClaimTypes.Email, admin.Email ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, AuthScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(AuthScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

            return RedirectToAction("Index", "Home");
        }

        // POST: AdminAuth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(AuthScheme);
            return RedirectToAction("Login");
        }
    }
}
