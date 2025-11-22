using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DokuzTasOnlineTurnuva.Services;
using DokuzTasOnlineTurnuva.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DokuzTasOnlineTurnuva.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly TournamentService _tournamentService;
        private readonly MatchmakingService _matchmakingService;
        private readonly ApplicationDbContext _context;
        
        public HomeController(TournamentService tournamentService, MatchmakingService matchmakingService, ApplicationDbContext context)
        {
            _tournamentService = tournamentService;
            _matchmakingService = matchmakingService;
            _context = context;
        }
        
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var rankings = await _tournamentService.GetWeeklyRankings();
            
            var canPlay = userId != null && await _matchmakingService.CanPlayToday(userId);
            var queueCount = await _matchmakingService.GetQueueCount();
            
            ViewBag.CanPlay = canPlay;
            ViewBag.QueueCount = queueCount;
            ViewBag.Rankings = rankings;
            
            return View();
        }
        
        public IActionResult Game()
        {
            return View();
        }
        
        public async Task<IActionResult> Stats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account");
            
            var user = await _context.Users.FindAsync(userId);
            var stats = await _context.PlayerStatistics
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.Date)
                .Take(30)
                .ToListAsync();
            
            var matches = await _context.Matches
                .Where(m => (m.Player1Id == userId || m.Player2Id == userId) && m.Status == Models.MatchStatus.Completed)
                .OrderByDescending(m => m.StartTime)
                .Take(20)
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .ToListAsync();
            
            ViewBag.User = user;
            ViewBag.Stats = stats;
            ViewBag.Matches = matches;
            
            return View();
        }
    }
}
