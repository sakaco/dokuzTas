using Microsoft.AspNetCore.Identity;

namespace DokuzTasOnlineTurnuva.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int Points { get; set; }
        public int Averaj { get; set; }
        public int TotalMatches { get; set; }
        public int WonMatches { get; set; }
        public int LostMatches { get; set; }
        public DateTime? LastActiveTime { get; set; }
        public bool IsBlacklisted { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public string? CurrentConnectionId { get; set; }
        
        public ICollection<PlayerStatistic> Statistics { get; set; } = new List<PlayerStatistic>();
        public ICollection<Match> MatchesAsPlayer1 { get; set; } = new List<Match>();
        public ICollection<Match> MatchesAsPlayer2 { get; set; } = new List<Match>();
    }
}
