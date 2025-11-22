using System.ComponentModel.DataAnnotations;

namespace DokuzTasOnlineTurnuva.Models
{
    public class SystemSettings
    {
        [Key]
        public int Id { get; set; }
        
        public int MaxDailyMatches { get; set; } = 5;
        public int QuestionTimeLimit { get; set; } = 20;
        public int MoveTimeLimit { get; set; } = 30;
        public int InactivityLimit { get; set; } = 5;
        
        public TimeSpan QuarterFinalStartTime { get; set; } = new TimeSpan(18, 0, 0);
        public TimeSpan QuarterFinalEndTime { get; set; } = new TimeSpan(23, 59, 0);
        
        public TimeSpan SemiFinalStartTime { get; set; } = new TimeSpan(18, 0, 0);
        public TimeSpan SemiFinalEndTime { get; set; } = new TimeSpan(23, 59, 0);
        
        public TimeSpan FinalStartTime { get; set; } = new TimeSpan(18, 0, 0);
        public TimeSpan FinalEndTime { get; set; } = new TimeSpan(23, 59, 0);
        
        public int PointsPerWin { get; set; } = 3;
        public int AverajPerQuit { get; set; } = 9;
        public int DailyBonusIncrement { get; set; } = 5;
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
