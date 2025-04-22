using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;

namespace Poker.Hubs;

public class PokerHub : Hub
{
    private readonly ILogger<PokerHub> _logger;
    private static Dictionary<string, Models.Poker> _pokerGames = [];
    private static readonly Dictionary<string, string> _playerConnections = new();

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

        await Clients.Group(gameId).SendAsync("GameStarted", _pokerGames.GetValueOrDefault(gameId));
    }

    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        if (_pokerGames.ContainsKey(gameId))
        {
            _pokerGames[gameId].Players = _pokerGames[gameId]
                .Players.Append(
                    new Models.Player(Context.ConnectionId)
                    {
                        Name = "",
                        ConnectionId = Context.ConnectionId,
                    }
                )
                .ToArray();
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
        HandlePlayerAction(gameId, action, data);
        await Clients
            .Group(gameId)
            .SendAsync("GameStateUpdated", new Poker.Models.Poker(_pokerGames[gameId].Players));
    }

    private void HandlePlayerAction(string gameId, string action, object data)
    {
        if (!_pokerGames.ContainsKey(gameId))
            return;
        int index = _pokerGames[gameId]
            .Players.ToList()
            .FindIndex(p => p.ConnectionId == Context.ConnectionId);
        if (index == -1 || index != _pokerGames[gameId].Turn)
            return;
        _pokerGames[gameId].Lock.EnterWriteLock();
        switch (action.ToLower())
        {
            case "fold":
                var foldingPlayer = _pokerGames[gameId].Players[index];
                if (foldingPlayer != null)
                {
                    foldingPlayer.Folded = true;
                }
                break;
            case "check":
                // No state change needed for check

                break;
            case "call":
                var callingPlayer = _pokerGames[gameId].Players[index];
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
                    var raisingPlayer = _pokerGames[gameId].Players[index];
                    _pokerGames[gameId].LastPlayerToRaise = index;
                    if (raisingPlayer != null && raisingPlayer.Chips >= raiseAmount)
                    {
                        raisingPlayer.Chips -= raiseAmount;
                        raisingPlayer.LastBet = raiseAmount;
                        _pokerGames[gameId].Pot += raiseAmount;
                        _pokerGames[gameId].CurrentBet = raiseAmount;
                    }
                }
                break;
            default:
                _pokerGames[gameId].Lock.ExitWriteLock();
                return;
        }
        _pokerGames[gameId].AdvanceRound();
        _pokerGames[gameId].Lock.ExitWriteLock();

        // After handling the action, check if the round should progress
    }

    private void CheckRoundProgress(string gameId) { }
}
