using DokuzTasOnlineTurnuva.Data;
using DokuzTasOnlineTurnuva.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace DokuzTasOnlineTurnuva.Services
{
    public class GameService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        
        private static readonly int[][] Mills = new int[][]
        {
            new[] {0, 1, 2}, new[] {5, 6, 7}, new[] {0, 3, 5}, new[] {2, 4, 7},
            new[] {8, 9, 10}, new[] {13, 14, 15}, new[] {8, 11, 13}, new[] {10, 12, 15},
            new[] {16, 17, 18}, new[] {21, 22, 23}, new[] {16, 19, 21}, new[] {18, 20, 23},
            new[] {1, 9, 17}, new[] {6, 14, 22}, new[] {3, 11, 19}, new[] {4, 12, 20}
        };
        
        private static readonly Dictionary<int, int[]> Neighbors = new Dictionary<int, int[]>
        {
            {0, new[] {1, 3}}, {1, new[] {0, 2, 9}}, {2, new[] {1, 4}},
            {3, new[] {0, 5, 11}}, {4, new[] {2, 7, 12}}, {5, new[] {3, 6}},
            {6, new[] {5, 7, 14}}, {7, new[] {4, 6}}, {8, new[] {9, 11}},
            {9, new[] {1, 8, 10, 17}}, {10, new[] {9, 12}}, {11, new[] {3, 8, 13, 19}},
            {12, new[] {4, 10, 15, 20}}, {13, new[] {11, 14}}, {14, new[] {6, 13, 15, 22}},
            {15, new[] {12, 14}}, {16, new[] {17, 19}}, {17, new[] {9, 16, 18}},
            {18, new[] {17, 20}}, {19, new[] {11, 16, 21}}, {20, new[] {12, 18, 23}},
            {21, new[] {19, 22}}, {22, new[] {14, 21, 23}}, {23, new[] {20, 22}}
        };
        
        public GameService(ApplicationDbContext context, IConnectionMultiplexer redis)
        {
            _context = context;
            _redis = redis;
            _db = redis.GetDatabase();
        }
        
        public async Task<GameState?> GetGameState(string matchId)
        {
            var json = await _db.StringGetAsync($"game:{matchId}");
            if (json.IsNullOrEmpty) return null;
            return JsonSerializer.Deserialize<GameState>(json!);
        }
        
        public async Task SaveGameState(GameState state)
        {
            var json = JsonSerializer.Serialize(state);
            await _db.StringSetAsync($"game:{state.MatchId}", json, TimeSpan.FromHours(24));
        }
        
        public async Task<Question?> GetRandomQuestion()
        {
            var questions = await _context.Questions.Where(q => q.IsActive).ToListAsync();
            if (!questions.Any()) return null;
            var random = new Random();
            return questions[random.Next(questions.Count)];
        }
        
        public bool CheckMill(int[] board, int position, int player)
        {
            return Mills.Any(mill => mill.Contains(position) && mill.All(p => board[p] == player));
        }
        
        public bool CanMove(int[] board, int player, int piecesOnBoard)
        {
            if (piecesOnBoard <= 3) return true;
            
            var pieces = new List<int>();
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == player) pieces.Add(i);
            }
            
            return pieces.Any(p => Neighbors[p].Any(n => board[n] == 0));
        }
        
        public bool IsValidMove(int from, int to, int[] board, bool canFly)
        {
            if (board[to] != 0) return false;
            if (canFly) return true;
            return Neighbors.ContainsKey(from) && Neighbors[from].Contains(to);
        }
        
        public async Task<(int points, int averaj)> CalculateMatchReward(Match match, string userId, bool isWinner, bool opponentQuit)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSettings();
            int points = 0;
            int averaj = 0;
            
            if (isWinner)
            {
                points = settings.PointsPerWin;
                if (opponentQuit)
                {
                    averaj = settings.AverajPerQuit;
                }
            }
            else
            {
                if (userId == match.LoserId)
                {
                    var piecesRemoved = userId == match.Player1Id ? match.Player1PiecesRemoved : match.Player2PiecesRemoved;
                    averaj = -piecesRemoved;
                }
            }
            
            return (points, averaj);
        }
        
        public async Task UpdatePlayerStats(string userId, int points, int averaj, bool quit)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;
            
            user.Points += points;
            user.Averaj += averaj;
            user.TotalMatches++;
            
            if (points > 0)
            {
                user.WonMatches++;
            }
            else
            {
                user.LostMatches++;
            }
            
            var today = DateTime.Today;
            var stat = await _context.PlayerStatistics
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Date.Date == today);
            
            if (stat == null)
            {
                stat = new PlayerStatistic
                {
                    UserId = userId,
                    Date = today,
                    WeekNumber = GetWeekNumber(today),
                    Year = today.Year
                };
                _context.PlayerStatistics.Add(stat);
            }
            
            stat.MatchesPlayed++;
            stat.Points += points;
            stat.Averaj += averaj;
            stat.QuitMatch = quit;
            
            if (stat.MatchesPlayed >= 5)
            {
                stat.CompletedDailyMatches = true;
                
                var yesterday = today.AddDays(-1);
                var yesterdayStat = await _context.PlayerStatistics
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Date.Date == yesterday);
                
                if (yesterdayStat?.CompletedDailyMatches == true)
                {
                    stat.ConsecutiveDays = yesterdayStat.ConsecutiveDays + 1;
                }
                else
                {
                    stat.ConsecutiveDays = 1;
                }
                
                var bonusAveraj = stat.ConsecutiveDays * settings.DailyBonusIncrement;
                stat.Averaj += bonusAveraj;
                user.Averaj += bonusAveraj;
            }
            
            await _context.SaveChangesAsync();
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
