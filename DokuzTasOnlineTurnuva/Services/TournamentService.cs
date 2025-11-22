using DokuzTasOnlineTurnuva.Data;
using DokuzTasOnlineTurnuva.Models;
using Microsoft.EntityFrameworkCore;

namespace DokuzTasOnlineTurnuva.Services
{
    public class TournamentService
    {
        private readonly ApplicationDbContext _context;
        
        public TournamentService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<MatchType?> GetCurrentMatchType()
        {
            var dayOfWeek = DateTime.Today.DayOfWeek;
            
            return dayOfWeek switch
            {
                DayOfWeek.Monday => MatchType.League,
                DayOfWeek.Tuesday => MatchType.League,
                DayOfWeek.Wednesday => MatchType.League,
                DayOfWeek.Thursday => MatchType.League,
                DayOfWeek.Friday => MatchType.QuarterFinal,
                DayOfWeek.Saturday => MatchType.SemiFinal,
                DayOfWeek.Sunday => MatchType.Final,
                _ => null
            };
        }
        
        public async Task<bool> IsEliminationTimeValid(MatchType matchType)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSettings();
            var now = DateTime.Now.TimeOfDay;
            
            return matchType switch
            {
                MatchType.QuarterFinal => now >= settings.QuarterFinalStartTime && now <= settings.QuarterFinalEndTime,
                MatchType.SemiFinal => now >= settings.SemiFinalStartTime && now <= settings.SemiFinalEndTime,
                MatchType.Final => now >= settings.FinalStartTime && now <= settings.FinalEndTime,
                _ => true
            };
        }
        
        public async Task<List<ApplicationUser>> GetTop8Players()
        {
            var weekNumber = GetWeekNumber(DateTime.Today);
            var year = DateTime.Today.Year;
            
            var players = await _context.Users
                .Where(u => !u.IsBlacklisted)
                .Select(u => new
                {
                    User = u,
                    WeekStats = _context.PlayerStatistics
                        .Where(s => s.UserId == u.Id && s.WeekNumber == weekNumber && s.Year == year)
                        .Sum(s => s.Points),
                    WeekAveraj = _context.PlayerStatistics
                        .Where(s => s.UserId == u.Id && s.WeekNumber == weekNumber && s.Year == year)
                        .Sum(s => s.Averaj)
                })
                .OrderByDescending(x => x.WeekStats)
                .ThenByDescending(x => x.WeekAveraj)
                .Take(8)
                .ToListAsync();
            
            return players.Select(p => p.User).ToList();
        }
        
        public async Task<List<(string rank, ApplicationUser user, int points, int averaj)>> GetWeeklyRankings(int? weekNumber = null, int? year = null)
        {
            weekNumber ??= GetWeekNumber(DateTime.Today);
            year ??= DateTime.Today.Year;
            
            var rankings = await _context.Users
                .Where(u => !u.IsBlacklisted)
                .Select(u => new
                {
                    User = u,
                    Points = _context.PlayerStatistics
                        .Where(s => s.UserId == u.Id && s.WeekNumber == weekNumber && s.Year == year)
                        .Sum(s => s.Points),
                    Averaj = _context.PlayerStatistics
                        .Where(s => s.UserId == u.Id && s.WeekNumber == weekNumber && s.Year == year)
                        .Sum(s => s.Averaj)
                })
                .Where(x => x.Points > 0 || x.Averaj != 0)
                .OrderByDescending(x => x.Points)
                .ThenByDescending(x => x.Averaj)
                .ToListAsync();
            
            var result = new List<(string rank, ApplicationUser user, int points, int averaj)>();
            for (int i = 0; i < rankings.Count; i++)
            {
                var rank = i switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => $"{i + 1}."
                };
                result.Add((rank, rankings[i].User, rankings[i].Points, rankings[i].Averaj));
            }
            
            return result;
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
