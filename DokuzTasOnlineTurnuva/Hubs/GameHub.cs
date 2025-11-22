using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using DokuzTasOnlineTurnuva.Services;
using DokuzTasOnlineTurnuva.Models;
using DokuzTasOnlineTurnuva.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DokuzTasOnlineTurnuva.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        private readonly GameService _gameService;
        private readonly MatchmakingService _matchmakingService;
        private readonly TournamentService _tournamentService;
        private readonly InactivityService _inactivityService;
        private readonly ApplicationDbContext _context;
        
        public GameHub(GameService gameService, MatchmakingService matchmakingService, 
            TournamentService tournamentService, InactivityService inactivityService,
            ApplicationDbContext context)
        {
            _gameService = gameService;
            _matchmakingService = matchmakingService;
            _tournamentService = tournamentService;
            _inactivityService = inactivityService;
            _context = context;
        }
        
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    if (!string.IsNullOrEmpty(user.CurrentConnectionId) && user.CurrentConnectionId != Context.ConnectionId)
                    {
                        await Clients.Client(user.CurrentConnectionId).SendAsync("ForceDisconnect", "Başka cihazdan giriş yapıldı");
                    }
                    
                    user.CurrentConnectionId = Context.ConnectionId;
                    user.LastActiveTime = DateTime.UtcNow;
                    user.LastLoginTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            await base.OnConnectedAsync();
        }
        
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await _matchmakingService.RemoveFromQueue(userId);
                
                var user = await _context.Users.FindAsync(userId);
                if (user != null && user.CurrentConnectionId == Context.ConnectionId)
                {
                    user.CurrentConnectionId = null;
                    user.LastActiveTime = null;
                    await _context.SaveChangesAsync();
                }
                
                var activeMatch = await _context.Matches
                    .FirstOrDefaultAsync(m => (m.Player1Id == userId || m.Player2Id == userId) 
                        && m.Status == MatchStatus.InProgress);
                
                if (activeMatch != null)
                {
                    var opponentId = activeMatch.Player1Id == userId ? activeMatch.Player2Id : activeMatch.Player1Id;
                    var opponent = await _context.Users.FindAsync(opponentId);
                    
                    activeMatch.Status = MatchStatus.Completed;
                    activeMatch.EndTime = DateTime.UtcNow;
                    activeMatch.WinnerId = opponentId;
                    activeMatch.LoserId = userId;
                    
                    if (activeMatch.Player1Id == userId)
                        activeMatch.Player1Quit = true;
                    else
                        activeMatch.Player2Quit = true;
                    
                    await _gameService.UpdatePlayerStats(opponentId, 3, 9, false);
                    await _gameService.UpdatePlayerStats(userId, 0, -9, true);
                    
                    var todayStat = await _context.PlayerStatistics
                        .FirstOrDefaultAsync(s => s.UserId == userId && s.Date.Date == DateTime.Today);
                    if (todayStat != null)
                    {
                        todayStat.QuitMatch = true;
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    if (opponent?.CurrentConnectionId != null)
                    {
                        await Clients.Client(opponent.CurrentConnectionId).SendAsync("OpponentQuit", opponent.UserName);
                    }
                }
            }
            
            await base.OnDisconnectedAsync(exception);
        }
        
        public async Task UpdateActivity()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await _inactivityService.UpdateActivity(userId);
            }
        }
        
        public async Task<object> JoinMatchmaking()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = Context.User?.Identity?.Name;
            
            if (userId == null)
                return new { success = false, message = "Kullanıcı bulunamadı" };
            
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsBlacklisted)
                return new { success = false, message = "Maç yapma yetkiniz yok" };
            
            if (!await _matchmakingService.CanPlayToday(userId))
                return new { success = false, message = "Bugün daha fazla maç yapamazsınız" };
            
            var matchType = await _tournamentService.GetCurrentMatchType();
            if (matchType == null)
                return new { success = false, message = "Geçersiz gün" };
            
            if (matchType != MatchType.League && !await _tournamentService.IsEliminationTimeValid(matchType.Value))
                return new { success = false, message = "Eleme maçı saati dışında" };
            
            await _matchmakingService.AddToQueue(userId, Context.ConnectionId);
            var queueCount = await _matchmakingService.GetQueueCount();
            await Clients.All.SendAsync("QueueUpdate", queueCount);
            
            var queuedPlayers = await _matchmakingService.GetQueuedPlayers();
            
            if (queuedPlayers.Count >= 2)
            {
                var players = queuedPlayers.Take(2).ToList();
                var player1Id = players[0].Key;
                var player2Id = players[1].Key;
                
                if (await _matchmakingService.HasPlayedWithToday(player1Id, player2Id))
                {
                    return new { success = true, message = "Kuyruğa eklendi", queueCount };
                }
                
                var match = await _matchmakingService.CreateMatch(player1Id, player2Id, matchType.Value);
                
                if (match != null)
                {
                    await _matchmakingService.RemoveFromQueue(player1Id);
                    await _matchmakingService.RemoveFromQueue(player2Id);
                    
                    var player1 = await _context.Users.FindAsync(player1Id);
                    var player2 = await _context.Users.FindAsync(player2Id);
                    
                    var gameState = new GameState
                    {
                        MatchId = match.Id.ToString(),
                        Player1Id = player1Id,
                        Player2Id = player2Id,
                        Player1Name = player1?.UserName ?? "",
                        Player2Name = player2?.UserName ?? "",
                        Board = new int[24],
                        CurrentPlayer = 1,
                        GamePhase = "placement",
                        ShowQuestion = true,
                        TimeRemaining = 20,
                        QuestionStartTime = DateTime.UtcNow,
                        MatchType = matchType.Value,
                        Player1ConnectionId = players[0].Value,
                        Player2ConnectionId = players[1].Value
                    };
                    
                    var question = await _gameService.GetRandomQuestion();
                    gameState.CurrentQuestion = question;
                    
                    await _gameService.SaveGameState(gameState);
                    
                    await Clients.Client(players[0].Value).SendAsync("MatchFound", gameState);
                    await Clients.Client(players[1].Value).SendAsync("MatchFound", gameState);
                    
                    queueCount = await _matchmakingService.GetQueueCount();
                    await Clients.All.SendAsync("QueueUpdate", queueCount);
                }
            }
            
            return new { success = true, message = "Kuyruğa eklendi", queueCount };
        }
        
        public async Task LeaveQueue()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await _matchmakingService.RemoveFromQueue(userId);
                var queueCount = await _matchmakingService.GetQueueCount();
                await Clients.All.SendAsync("QueueUpdate", queueCount);
            }
        }
        
        public async Task<object> AnswerQuestion(string matchId, int answer)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return new { success = false };
            
            await _inactivityService.UpdateActivity(userId);
            
            var state = await _gameService.GetGameState(matchId);
            if (state == null) return new { success = false };
            
            var isCurrentPlayer = (state.CurrentPlayer == 1 && state.Player1Id == userId) ||
                                 (state.CurrentPlayer == 2 && state.Player2Id == userId);
            
            if (!isCurrentPlayer || !state.ShowQuestion || state.CurrentQuestion == null)
                return new { success = false };
            
            var correct = answer == state.CurrentQuestion.CorrectAnswer;
            
            if (correct)
            {
                state.ShowQuestion = false;
                state.MoveStartTime = DateTime.UtcNow;
                await _gameService.SaveGameState(state);
                
                await Clients.Clients(state.Player1ConnectionId, state.Player2ConnectionId)
                    .SendAsync("QuestionAnswered", new { correct = true, state });
            }
            else
            {
                state.CurrentPlayer = 3 - state.CurrentPlayer;
                var newQuestion = await _gameService.GetRandomQuestion();
                state.CurrentQuestion = newQuestion;
                state.QuestionStartTime = DateTime.UtcNow;
                state.TimeRemaining = 20;
                await _gameService.SaveGameState(state);
                
                await Clients.Clients(state.Player1ConnectionId, state.Player2ConnectionId)
                    .SendAsync("QuestionAnswered", new { correct = false, state });
            }
            
            return new { success = true };
        }
        
        public async Task<object> PlacePiece(string matchId, int position)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return new { success = false };
            
            await _inactivityService.UpdateActivity(userId);
            
            var state = await _gameService.GetGameState(matchId);
            if (state == null) return new { success = false };
            
            var isCurrentPlayer = (state.CurrentPlayer == 1 && state.Player1Id == userId) ||
                                 (state.CurrentPlayer == 2 && state.Player2Id == userId);
            
            if (!isCurrentPlayer || state.ShowQuestion || state.Board[position] != 0)
                return new { success = false };
            
            if ((state.CurrentPlayer == 1 && state.PlacedPieces1 >= 9) ||
                (state.CurrentPlayer == 2 && state.PlacedPieces2 >= 9))
                return new { success = false };
            
            state.Board[position] = state.CurrentPlayer;
            
            if (state.CurrentPlayer == 1)
            {
                state.PlacedPieces1++;
                state.Player1PiecesOnBoard++;
            }
            else
            {
                state.PlacedPieces2++;
                state.Player2PiecesOnBoard++;
            }
            
            var hasMill = _gameService.CheckMill(state.Board, position, state.CurrentPlayer);
            
            if (hasMill)
            {
                var opponent = 3 - state.CurrentPlayer;
                var hasOpponentPieces = state.Board.Any(p => p == opponent);
                
                if (hasOpponentPieces)
                {
                    state.GamePhase = "remove";
                    await _gameService.SaveGameState(state);
                    await Clients.Clients(state.Player1ConnectionId, state.Player2ConnectionId)
                        .SendAsync("PiecePlaced", state);
                    return new { success = true };
                }
            }
            
            if (state.PlacedPieces1 >= 9 && state.PlacedPieces2 >= 9)
            {
                state.GamePhase = "movement";
                
                if (state.Player1PiecesOnBoard <= 2)
                {
                    await EndMatch(matchId, state.Player2Id);
                    return new { success = true };
                }
                if (state.Player2PiecesOnBoard <= 2)
                {
                    await EndMatch(matchId, state.Player1Id);
                    return new { success = true };
                }
            }
            
            state.CurrentPlayer = 3 - state.CurrentPlayer;
            state.ShowQuestion = true;
            var newQuestion = await _gameService.GetRandomQuestion();
            state.CurrentQuestion = newQuestion;
            state.QuestionStartTime = DateTime.UtcNow;
            state.TimeRemaining = 20;
            
            await _gameService.SaveGameState(state);
            await Clients.Clients(state.Player1ConnectionId, state.Player2ConnectionId)
                .SendAsync("PiecePlaced", state);
            
            return new { success = true };
        }
        
        public async Task<object> MovePiece(string matchId, int from, int to)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return new { success = false };
            
            await _inactivityService.UpdateActivity(userId);
            
            var state = await _gameService.GetGameState(matchId);
            if (state == null) return new { success = false };
            
            var isCurrentPlayer = (state.CurrentPlayer == 1 && state.Player1Id == userId) ||
                                 (state.CurrentPlayer == 2 && state.Player2Id == userId);
            
            if (!isCurrentPlayer || state.ShowQuestion || state.GamePhase != "movement")
                return new { success = false };
            
            if (state.Board[from] != state.CurrentPlayer)
                return new { success = false };
            
            var canFly = state.CurrentPlayer == 1 ? state.Player1PiecesOnBoard <= 3 : state.Player2PiecesOnBoard <= 3;
            
            if (!_gameService.IsValidMove(from, to, state.Board, canFly))
                return new { success = false };
            
            state.Board[to] = state.CurrentPlayer;
            state.Board[from] = 0;
            
            var hasMill = _gameService.CheckMill(state.Board, to, state.CurrentPlayer);
            
            if (hasMill)
            {
                var opponent = 3 - state.CurrentPlayer;
                var hasOpponentPieces = state.Board.Any(p => p == opponent);
                
                if (hasOpponentPieces)
                {
                    state.GamePhase = "remove";
                    await _gameService.SaveGameState(state);
                    await Clients.Clients(state.Player1ConnectionId, state.Player2ConnectionId)
                        .SendAsync("PieceMoved", state);
                    return new { success = true };
                }
            }
            
            var nextPlayer = 3 - state.CurrentPlayer;
            if (!_gameService.CanMove(state.Board, nextPlayer, 
                nextPlayer == 1 ? state.Player1PiecesOnBoard : state.Player2PiecesOnBoard))
            {
                await EndMatch(matchId, state.CurrentPlayer == 1 ? state.Player1Id : state.Player2Id);
                return new { success = true };
            }
            
            state.CurrentPlayer = nextPlayer;
            state.ShowQuestion = true;
            var newQuestion = await _gameService.GetRandomQuestion();
            state.CurrentQuestion = newQuestion;
            state.QuestionStartTime = DateTime.UtcNow;
            state.TimeRemaining = 20;
            
            await _gameService.SaveGameState(state);
            await Clients.Clients(state.Player1ConnectionId, state.Player2ConnectionId)
                .SendAsync("PieceMoved", state);
            
            return new { success = true };
        }
        
        public async Task<object> RemovePiece(string matchId, int position)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return new { success = false };
            
            await _inactivityService.UpdateActivity(userId);
            
            var state = await _gameService.GetGameState(matchId);
            if (state == null) return new { success = false };
            
            var isCurrentPlayer = (state.CurrentPlayer == 1 && state.Player1Id == userId) ||
                                 (state.CurrentPlayer == 2 && state.Player2Id == userId);
            
            if (!isCurrentPlayer || state.GamePhase != "remove")
                return new { success = false };
            
            var opponent = 3 - state.CurrentPlayer;
            if (state.Board[position] != opponent)
                return new { success = false };
            
            state.Board[position] = 0;
            
            if (state.CurrentPlayer == 1)
                state.Player2PiecesOnBoard--;
            else
                state.Player1PiecesOnBoard--;
            
            var match = await _context.Matches.FindAsync(int.Parse(matchId));
            if (match != null)
            {
                if (state.CurrentPlayer == 1)
                    match.Player2PiecesRemoved++;
                else
                    match.Player1PiecesRemoved++;
                await _context.SaveChangesAsync();
            }
            
            if (state.Player1PiecesOnBoard <= 2 && state.PlacedPieces1 >= 9 && state.PlacedPieces2 >= 9)
            {
                await EndMatch(matchId, state.Player2Id);
                return new { success = true };
            }
            if (state.Player2PiecesOnBoard <= 2 && state.PlacedPieces1 >= 9 && state.PlacedPieces2 >= 9)
            {
                await EndMatch(matchId, state.Player1Id);
                return new { success = true };
            }
            
            if (state.PlacedPieces1 >= 9 && state.PlacedPieces2 >= 9)
            {
                state.GamePhase = "movement";
                
                var nextPlayer = 3 - state.CurrentPlayer;
                var nextPiecesOnBoard = nextPlayer == 1 ? state.Player1PiecesOnBoard : state.Player2PiecesOnBoard;
                
                if (!_gameService.CanMove(state.Board, nextPlayer, nextPiecesOnBoard))
                {
                    await EndMatch(matchId, state.CurrentPlayer == 1 ? state.Player1Id : state.Player2Id);
                    return new { success = true };
                }
            }
            else
            {
                state.GamePhase = "placement";
            }
            
            state.CurrentPlayer = 3 - state.CurrentPlayer;
            state.ShowQuestion = true;
            var newQuestion = await _gameService.GetRandomQuestion();
            state.CurrentQuestion = newQuestion;
            state.QuestionStartTime = DateTime.UtcNow;
            state.TimeRemaining = 20;
            
            await _gameService.SaveGameState(state);
            await Clients.Clients(state.Player1ConnectionId, state.Player2ConnectionId)
                .SendAsync("PieceRemoved", state);
            
            return new { success = true };
        }
        
        private async Task EndMatch(string matchId, string winnerId)
        {
            var match = await _context.Matches.FindAsync(int.Parse(matchId));
            if (match == null) return;
            
            match.Status = MatchStatus.Completed;
            match.EndTime = DateTime.UtcNow;
            match.WinnerId = winnerId;
            match.LoserId = winnerId == match.Player1Id ? match.Player2Id : match.Player1Id;
            
            var (winnerPoints, winnerAveraj) = await _gameService.CalculateMatchReward(match, winnerId, true, false);
            await _gameService.UpdatePlayerStats(winnerId, winnerPoints, winnerAveraj, false);
            
            var loserAveraj = winnerId == match.Player1Id ? -match.Player2PiecesRemoved : -match.Player1PiecesRemoved;
            await _gameService.UpdatePlayerStats(match.LoserId, 0, loserAveraj, false);
            
            await _context.SaveChangesAsync();
            
            var state = await _gameService.GetGameState(matchId);
            if (state != null)
            {
                var winner = await _context.Users.FindAsync(winnerId);
                await Clients.Clients(state.Player1ConnectionId, state.Player2ConnectionId)
                    .SendAsync("MatchEnded", new { winnerId, winnerName = winner?.UserName });
            }
        }
    }
}
