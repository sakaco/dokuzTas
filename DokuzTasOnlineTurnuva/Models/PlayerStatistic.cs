using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DokuzTasOnlineTurnuva.Models
{
    public class PlayerStatistic
    {
        [Key]
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
        
        public DateTime Date { get; set; }
        public int MatchesPlayed { get; set; }
        public int Points { get; set; }
        public int Averaj { get; set; }
        public bool CompletedDailyMatches { get; set; }
        public int ConsecutiveDays { get; set; }
        public bool QuitMatch { get; set; }
        
        public int WeekNumber { get; set; }
        public int Year { get; set; }
    }
}
