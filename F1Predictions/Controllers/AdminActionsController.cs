using F1Predictions.Data;
using F1Predictions.Models;
using F1Predictions.Models.DTOs;
using F1Predictions.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth")]
    public class AdminActionsController : Controller
    {
        private readonly IPointsService _pointsService;
        private readonly IVotingService _votingService;
        private readonly AppDbContext _context;

        public AdminActionsController(
            IPointsService pointsService,
            IVotingService votingService,
            AppDbContext context)
        {
            _pointsService = pointsService;
            _votingService = votingService;
            _context = context;
        }

        private int GetAdminId()
        {
            var claim = User.FindFirst("AdminId");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        // GET: AdminActions — lightweight menu, no queries
        public IActionResult Index()
        {
            return View();
        }

        // GET: AdminActions/RaceManagement
        public async Task<IActionResult> RaceManagement()
        {
            var races = await _context.Races
                .Include(r => r.Track)
                .Include(r => r.Championship)
                .OrderByDescending(r => r.RaceDate)
                .ToListAsync();

            var raceStates = await _context.Set<RaceState>().ToListAsync();
            var raceStateDict = raceStates.ToDictionary(rs => rs.RaceId, rs => rs.State);

            ViewBag.Races = races;
            ViewBag.RaceStates = raceStateDict;

            return View();
        }

        // GET: AdminActions/TiedPredictions
        public async Task<IActionResult> TiedPredictions()
        {
            var votableTypes = new[] { "Surprise", "Flop", "Crazy", "Custom" };

            var tiedPredictions = await _context.Set<UserPrediction>()
                .Include(up => up.WeeklyPrediction)
                .Include(up => up.User)
                .Include(up => up.Driver)
                .Include(up => up.Team)
                .Include(up => up.League)
                .Include(up => up.Votes)
                .Where(up => votableTypes.Contains(up.WeeklyPrediction.PredictionType)
                           && !up.Points.Any()
                           && !up.Decisions.Any())
                .AsSplitQuery()
                .ToListAsync();

            var tied = tiedPredictions
                .Where(p =>
                {
                    var yes = p.Votes.Count(v => v.Vote);
                    var no = p.Votes.Count(v => !v.Vote);
                    return yes == no && (yes + no) > 0;
                })
                .ToList();

            ViewBag.TiedPredictions = tied;
            return View();
        }

        // GET: AdminActions/PendingReports
        public async Task<IActionResult> PendingReports()
        {
            var pendingReports = await _context.Set<PredictionReport>()
                .Include(pr => pr.UserPrediction)
                    .ThenInclude(up => up.User)
                .Include(pr => pr.UserPrediction)
                    .ThenInclude(up => up.WeeklyPrediction)
                .Include(pr => pr.Reporter)
                .Where(pr => !pr.IsResolved)
                .OrderByDescending(pr => pr.CreatedAt)
                .ToListAsync();

            ViewBag.PendingReports = pendingReports;
            return View();
        }

        // POST: AdminActions/GrantPoints
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantPoints(int raceId)
        {
            var result = await _pointsService.GrantInitialPoints(raceId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Points granted! {result.Data!.CorrectPredictions} correct out of {result.Data.TotalPredictionsChecked} predictions. Voting is now open.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(RaceManagement));
        }

        // POST: AdminActions/ResetAndRegrant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAndRegrant(int raceId)
        {
            var result = await _pointsService.ResetAndRegrant(raceId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Initial points reset and re-granted! {result.Data!.CorrectPredictions} correct out of {result.Data.TotalPredictionsChecked} predictions. Voting points were preserved.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(RaceManagement));
        }

        // POST: AdminActions/FinalizeVoting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeVoting(int raceId)
        {
            var result = await _votingService.FinalizeVoting(raceId);

            if (result.Success)
            {
                var data = result.Data!;
                TempData["SuccessMessage"] = $"Voting finalized! Approved: {data.ApprovedByVotes}, Denied: {data.DeniedByVotes}, No votes: {data.NoVotesCast}, Tied (needs admin): {data.TiedForAdmin}, Already resolved: {data.AlreadyResolved}";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(RaceManagement));
        }

        // POST: AdminActions/AdminDecide
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminDecide(int userPredictionId, bool isApproved, string? note)
        {
            int adminId = GetAdminId();

            var request = new AdminDecideRequest
            {
                UserPredictionId = userPredictionId,
                IsApproved = isApproved,
                Note = note
            };

            var result = await _votingService.AdminDecide(request, adminId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Data;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(TiedPredictions));
        }

        // POST: AdminActions/ResolveReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int reportId, string? note)
        {
            int adminId = GetAdminId();

            var request = new ResolveReportRequest
            {
                ReportId = reportId,
                Note = note
            };

            var result = await _votingService.ResolveReport(request, adminId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Data;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(PendingReports));
        }
    }
}
