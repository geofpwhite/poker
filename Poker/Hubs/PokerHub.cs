using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace Poker.Hubs;

public class PokerHub : Hub
{
    private readonly ILogger<PokerHub> _logger;
    private static Dictionary<string, Models.Poker> _pokerGames = [];
    private static readonly Dictionary<string, string> _playerConnections = new();
    private static Dictionary<string, ReaderWriterLockSlim> _gameLocks = new();
    public PokerHub(ILogger<PokerHub> logger, Models.Poker pokerGame)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartGame(string gameId)
    {
        if (!_pokerGames.ContainsKey(gameId))
        {
            _pokerGames.Add(gameId, new Models.Poker());
        }
        else
        {
            _pokerGames[gameId] = new Models.Poker(_pokerGames[gameId].Players);
        }
        _gameLocks.Add(gameId, new ReaderWriterLockSlim());

        await Clients.Group(gameId).SendAsync("GameStarted", _pokerGames.GetValueOrDefault(gameId));
    }

    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        if (_pokerGames.ContainsKey(gameId))
        {
            _pokerGames[gameId].Players.Append(new Models.Player(Context.ConnectionId) { Name = "", ConnectionId = Context.ConnectionId });
        }

        await Clients.Group(gameId).SendAsync("UserJoined", Context.ConnectionId);
    }

    public async Task LeaveGame(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
        await Clients.Group(gameId).SendAsync("UserLeft", Context.ConnectionId);
    }

    public async Task PlayerAction(string gameId, string action, int data)
    {
        // Handle the game state modification based on the action
        _gameLocks[gameId].EnterWriteLock();
        HandlePlayerAction(gameId, action, data);
        _gameLocks[gameId].ExitWriteLock();
        await Clients.Group(gameId).SendAsync("GameStateUpdated", new Poker.Models.Poker(_pokerGames[gameId].Players));
    }

    private void HandlePlayerAction(string gameId, string action, object data)
    {
        int index = _pokerGames[gameId].Players.ToList().FindIndex(p => p.ConnectionId == Context.ConnectionId);
        Console.WriteLine($"Index: {index}, Turn: {_pokerGames[gameId].Turn}");
        if (index == -1 || index != _pokerGames[gameId].Turn) return;
        switch (action.ToLower())
        {
            case "bet":
                if (data is int betAmount)
                {
                    var player = _pokerGames[gameId].Players.FirstOrDefault(p => p.Name == Context.ConnectionId);
                    if (player != null && player.Chips >= betAmount)
                    {
                        player.Chips -= betAmount;
                        _pokerGames[gameId].Pot += betAmount;
                        _pokerGames[gameId].CurrentBet = betAmount;
                    }
                }
                break;
            case "fold":
                var foldingPlayer = _pokerGames[gameId].Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (foldingPlayer != null)
                {
                    foldingPlayer.Folded = true;
                }
                break;
            case "check":
                // No state change needed for check

                break;
            case "call":
                var callingPlayer = _pokerGames[gameId].Players.FirstOrDefault(p => p.Name == Context.ConnectionId);
                if (callingPlayer != null)
                {
                    int callAmount = _pokerGames[gameId].CurrentBet;
                    if (callingPlayer.Chips >= callAmount)
                    {
                        callingPlayer.Chips -= callAmount;
                        _pokerGames[gameId].Pot += callAmount;
                    }
                }
                break;
            case "raise":
                if (data is int raiseAmount)
                {
                    var raisingPlayer = _pokerGames[gameId].Players.FirstOrDefault(p => p.Name == Context.ConnectionId);
                    if (raisingPlayer != null && raisingPlayer.Chips >= raiseAmount)
                    {
                        raisingPlayer.Chips -= raiseAmount;
                        _pokerGames[gameId].Pot += raiseAmount;
                        _pokerGames[gameId].CurrentBet = raiseAmount;
                    }
                }
                break;
        }

        // After handling the action, check if the round should progress
        CheckRoundProgress(gameId);
    }

    private void CheckRoundProgress(string gameId)
    {
        // Check if all players have acted
        bool allPlayersActed = _pokerGames[gameId].Players
            .Where(p => !p.Folded)
            .All(p => p.Chips == 0 || p.Chips == _pokerGames[gameId].CurrentBet);

        if (allPlayersActed)
        {
            // Progress to next round
            _pokerGames[gameId].Round++;
            if (_pokerGames[gameId].Round > 3) // End of game
            {
                _pokerGames[gameId].Showdown();
                // Reset game state for next hand
                _pokerGames[gameId].Round = 0;
                _pokerGames[gameId].Pot = 0;
                _pokerGames[gameId].CurrentBet = 0;
                foreach (var player in _pokerGames[gameId].Players)
                {
                    player.Folded = false;
                }
            }
            else
            {
                _pokerGames[gameId].Deal();
            }
        }
    }
}