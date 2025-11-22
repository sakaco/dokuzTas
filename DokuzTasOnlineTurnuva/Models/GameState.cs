namespace DokuzTasOnlineTurnuva.Models
{
    public class GameState
    {
        public string MatchId { get; set; } = string.Empty;
        public string Player1Id { get; set; } = string.Empty;
        public string Player2Id { get; set; } = string.Empty;
        public string Player1Name { get; set; } = string.Empty;
        public string Player2Name { get; set; } = string.Empty;
        
        public int[] Board { get; set; } = new int[24];
        public int CurrentPlayer { get; set; } = 1;
        
        public int Player1PiecesOnBoard { get; set; } = 0;
        public int Player2PiecesOnBoard { get; set; } = 0;
        public int PlacedPieces1 { get; set; } = 0;
        public int PlacedPieces2 { get; set; } = 0;
        
        public string GamePhase { get; set; } = "placement";
        public int? SelectedPiece { get; set; }
        
        public Question? CurrentQuestion { get; set; }
        public bool ShowQuestion { get; set; } = true;
        public int TimeRemaining { get; set; } = 20;
        public DateTime? QuestionStartTime { get; set; }
        public DateTime? MoveStartTime { get; set; }
        
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public MatchType MatchType { get; set; }
        
        public string Player1ConnectionId { get; set; } = string.Empty;
        public string Player2ConnectionId { get; set; } = string.Empty;
    }
}
