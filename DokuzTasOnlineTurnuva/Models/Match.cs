using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DokuzTasOnlineTurnuva.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }
        
        public string Player1Id { get; set; } = string.Empty;
        [ForeignKey("Player1Id")]
        public ApplicationUser Player1 { get; set; } = null!;
        
        public string Player2Id { get; set; } = string.Empty;
        [ForeignKey("Player2Id")]
        public ApplicationUser Player2 { get; set; } = null!;
        
        public string? WinnerId { get; set; }
        public string? LoserId { get; set; }
        
        public MatchType MatchType { get; set; }
        public MatchStatus Status { get; set; }
        
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        
        public bool Player1Quit { get; set; }
        public bool Player2Quit { get; set; }
        
        public int Player1PiecesRemoved { get; set; }
        public int Player2PiecesRemoved { get; set; }
        
        public string GameState { get; set; } = "{}";
    }
    
    public enum MatchType
    {
        League,
        QuarterFinal,
        SemiFinal,
        Final
    }
    
    public enum MatchStatus
    {
        Waiting,
        InProgress,
        Completed,
        Cancelled
    }
}
