using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DokuzTasOnlineTurnuva.Data;
using DokuzTasOnlineTurnuva.Models;
using DokuzTasOnlineTurnuva.Services;
using Microsoft.EntityFrameworkCore;

namespace DokuzTasOnlineTurnuva.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TournamentService _tournamentService;
        
        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, TournamentService tournamentService)
        {
            _context = context;
            _userManager = userManager;
            _tournamentService = tournamentService;
        }
        
        public IActionResult Index()
        {
            return View();
        }
        
        public async Task<IActionResult> Players()
        {
            var users = await _userManager.GetUsersInRoleAsync("Player");
            return View(users);
        }
        
        [HttpPost]
        public async Task<IActionResult> CreatePlayer(string username, string email, string password)
        {
            var user = new ApplicationUser
            {
                UserName = username,
                Email = email
            };
            
            var result = await _userManager.CreateAsync(user, password);
            
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Player");
            }
            
            return RedirectToAction("Players");
        }
        
        [HttpPost]
        public async Task<IActionResult> DeletePlayer(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("Players");
        }
        
        [HttpPost]
        public async Task<IActionResult> ToggleBlacklist(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsBlacklisted = !user.IsBlacklisted;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Players");
        }
        
        [HttpPost]
        public async Task<IActionResult> ResetPlayer(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.Points = 0;
                user.Averaj = 0;
                user.TotalMatches = 0;
                user.WonMatches = 0;
                user.LostMatches = 0;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Players");
        }
        
        public async Task<IActionResult> Settings()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSettings();
            return View(settings);
        }
        
        [HttpPost]
        public async Task<IActionResult> UpdateSettings(SystemSettings model)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                _context.SystemSettings.Add(model);
            }
            else
            {
                settings.MaxDailyMatches = model.MaxDailyMatches;
                settings.QuestionTimeLimit = model.QuestionTimeLimit;
                settings.MoveTimeLimit = model.MoveTimeLimit;
                settings.InactivityLimit = model.InactivityLimit;
                settings.QuarterFinalStartTime = model.QuarterFinalStartTime;
                settings.QuarterFinalEndTime = model.QuarterFinalEndTime;
                settings.SemiFinalStartTime = model.SemiFinalStartTime;
                settings.SemiFinalEndTime = model.SemiFinalEndTime;
                settings.FinalStartTime = model.FinalStartTime;
                settings.FinalEndTime = model.FinalEndTime;
                settings.PointsPerWin = model.PointsPerWin;
                settings.AverajPerQuit = model.AverajPerQuit;
                settings.DailyBonusIncrement = model.DailyBonusIncrement;
                settings.LastUpdated = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction("Settings");
        }
        
        public async Task<IActionResult> Matches()
        {
            var matches = await _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .OrderByDescending(m => m.StartTime)
                .Take(100)
                .ToListAsync();
            
            return View(matches);
        }
        
        public async Task<IActionResult> Reports(int? weekNumber, int? year)
        {
            weekNumber ??= GetWeekNumber(DateTime.Today);
            year ??= DateTime.Today.Year;
            
            var rankings = await _tournamentService.GetWeeklyRankings(weekNumber, year);
            
            ViewBag.WeekNumber = weekNumber;
            ViewBag.Year = year;
            ViewBag.Rankings = rankings;
            
            return View();
        }
        
        public async Task<IActionResult> PlayerDetail(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            
            var stats = await _context.PlayerStatistics
                .Where(s => s.UserId == id)
                .OrderByDescending(s => s.Date)
                .ToListAsync();
            
            var matches = await _context.Matches
                .Where(m => (m.Player1Id == id || m.Player2Id == id) && m.Status == MatchStatus.Completed)
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();
            
            ViewBag.User = user;
            ViewBag.Stats = stats;
            ViewBag.Matches = matches;
            
            return View();
        }
        
        public async Task<IActionResult> Questions()
        {
            var questions = await _context.Questions.ToListAsync();
            return View(questions);
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateQuestion(Question model)
        {
            _context.Questions.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Questions");
        }
        
        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Questions");
        }
        
        [HttpPost]
        public async Task<IActionResult> ToggleQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                question.IsActive = !question.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Questions");
        }
        
        private int GetWeekNumber(DateTime date)
        {
            var jan1 = new DateTime(date.Year, 1, 1);
            var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
            var firstMonday = jan1.AddDays(daysOffset);
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            var firstWeek = cal.GetWeekOfYear(firstMonday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var weekNum = cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return weekNum - firstWeek + 1;
        }
    }
}
