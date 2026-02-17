using F1Predictions.Data;
using F1Predictions.Models;
using F1Predictions.Models.DTOs;
using F1Predictions.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Controllers
{
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

        // GET: AdminActions
        public async Task<IActionResult> Index()
        {
            // 1. Load races with their states (most recent first)
            var races = await _context.Races
                .Include(r => r.Track)
                .Include(r => r.Championship)
                .OrderByDescending(r => r.RaceDate)
                .ToListAsync();

            var raceStates = await _context.Set<RaceState>().ToListAsync();
            var raceStateDict = raceStates.ToDictionary(rs => rs.RaceId, rs => rs.State);

            // 2. Load tied predictions (votable predictions with no points entry and equal yes/no votes)
            var votableTypes = new[] { "Surprise", "Flop", "Crazy", "Custom" };

            var tiedPredictions = await _context.Set<UserPrediction>()
                .Include(up => up.WeeklyPrediction)
                .Include(up => up.User)
                .Include(up => up.Driver)
                .Include(up => up.Team)
                .Include(up => up.League)
                .Include(up => up.Votes)
                .Include(up => up.Points)
                .Include(up => up.Decisions)
                .Where(up => votableTypes.Contains(up.WeeklyPrediction.PredictionType)
                           && !up.Points.Any()         // Not yet resolved
                           && !up.Decisions.Any())      // No admin decision yet
                .ToListAsync();

            // Filter to only truly tied ones (yes == no and at least 1 vote)
            var tied = tiedPredictions
                .Where(p =>
                {
                    var yes = p.Votes.Count(v => v.Vote);
                    var no = p.Votes.Count(v => !v.Vote);
                    return yes == no && (yes + no) > 0;
                })
                .ToList();

            // 3. Load pending reports
            var pendingReports = await _context.Set<PredictionReport>()
                .Include(pr => pr.UserPrediction)
                    .ThenInclude(up => up.User)
                .Include(pr => pr.UserPrediction)
                    .ThenInclude(up => up.WeeklyPrediction)
                .Include(pr => pr.Reporter)
                .Where(pr => !pr.IsResolved)
                .OrderByDescending(pr => pr.CreatedAt)
                .ToListAsync();

            ViewBag.Races = races;
            ViewBag.RaceStates = raceStateDict;
            ViewBag.TiedPredictions = tied;
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

            return RedirectToAction(nameof(Index));
        }

        // POST: AdminActions/ResetAndRegrant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAndRegrant(int raceId)
        {
            var result = await _pointsService.ResetAndRegrant(raceId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Points reset and re-granted! {result.Data!.CorrectPredictions} correct out of {result.Data.TotalPredictionsChecked} predictions. Voting reopened.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
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

            return RedirectToAction(nameof(Index));
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

            return RedirectToAction(nameof(Index));
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

            return RedirectToAction(nameof(Index));
        }
    }
}
