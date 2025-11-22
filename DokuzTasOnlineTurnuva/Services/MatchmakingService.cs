using DokuzTasOnlineTurnuva.Data;
using DokuzTasOnlineTurnuva.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace DokuzTasOnlineTurnuva.Services
{
    public class MatchmakingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        
        public MatchmakingService(ApplicationDbContext context, IConnectionMultiplexer redis)
        {
            _context = context;
            _redis = redis;
            _db = redis.GetDatabase();
        }
        
        public async Task<bool> CanPlayToday(string userId)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSettings();
            var today = DateTime.Today;
            
            var todayMatches = await _context.Matches
                .Where(m => (m.Player1Id == userId || m.Player2Id == userId) 
                    && m.StartTime.Date == today
                    && m.Status == MatchStatus.Completed)
                .CountAsync();
            
            var todayQuit = await _context.PlayerStatistics
                .AnyAsync(s => s.UserId == userId && s.Date.Date == today && s.QuitMatch);
            
            return todayMatches < settings.MaxDailyMatches && !todayQuit;
        }
        
        public async Task<bool> HasPlayedWithToday(string userId, string opponentId)
        {
            var today = DateTime.Today;
            
            return await _context.Matches
                .AnyAsync(m => m.StartTime.Date == today
                    && m.Status == MatchStatus.Completed
                    && ((m.Player1Id == userId && m.Player2Id == opponentId)
                        || (m.Player1Id == opponentId && m.Player2Id == userId)));
        }
        
        public async Task AddToQueue(string userId, string connectionId)
        {
            await _db.HashSetAsync("matchmaking:queue", userId, connectionId);
        }
        
        public async Task RemoveFromQueue(string userId)
        {
            await _db.HashDeleteAsync("matchmaking:queue", userId);
        }
        
        public async Task<int> GetQueueCount()
        {
            return (int)await _db.HashLengthAsync("matchmaking:queue");
        }
        
        public async Task<Dictionary<string, string>> GetQueuedPlayers()
        {
            var entries = await _db.HashGetAllAsync("matchmaking:queue");
            return entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
        }
        
        public async Task<Match?> CreateMatch(string player1Id, string player2Id, MatchType matchType)
        {
            var player1 = await _context.Users.FindAsync(player1Id);
            var player2 = await _context.Users.FindAsync(player2Id);
            
            if (player1 == null || player2 == null) return null;
            
            var match = new Match
            {
                Player1Id = player1Id,
                Player2Id = player2Id,
                MatchType = matchType,
                Status = MatchStatus.InProgress,
                StartTime = DateTime.UtcNow,
                WeekNumber = GetWeekNumber(DateTime.Today),
                Year = DateTime.Today.Year
            };
            
            _context.Matches.Add(match);
            await _context.SaveChangesAsync();
            
            return match;
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
