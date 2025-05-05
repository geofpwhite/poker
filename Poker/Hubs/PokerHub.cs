using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Poker.Models;

namespace Poker.Hubs;

public class PokerHub : Hub
{
    private readonly ILogger<PokerHub> _logger;
    public static Dictionary<string, Models.Poker> Games = [];
    private static readonly Dictionary<string, string> _playerConnections = new();

    public PokerHub(ILogger<PokerHub> logger)
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


    public async Task CloseGame(string gameId)
    {
        if (Games.ContainsKey(gameId))
        {
            Games.Remove(gameId);
            await Clients.Group(gameId).SendAsync("GameClosed", gameId);
        }
        else
        {
            await Clients.Caller.SendAsync("GameNotFound", gameId);
        }
    }

    public async Task CreateGame(string gameId)
    {
        if (Games.ContainsKey(gameId))
        {
            await Clients.Caller.SendAsync("GameAlreadyExists", gameId);
            return;
        }

        var game = new Models.Poker();
        Games[gameId] = game;
        await Clients.Caller.SendAsync("GameCreated", gameId);
    }
    public async Task StartGame(string gameId)
    {

        Games[gameId].Deal();
        Games[gameId].Started = true;
        await Clients.Group(gameId).SendAsync("GameStarted", Games.GetValueOrDefault(gameId));
        foreach (var player in Games[gameId].Players)
        {
            await Clients.Client(player.ConnectionId).SendAsync("PlayerCards", player.Cards.Select(c => c.ToString()));
        }
    }

    public async Task JoinGame(string playerName, string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        if (Games.ContainsKey(gameId))
        {
            Games[gameId].Players = Games[gameId]
                .Players.Append(
                    new Player(Context.ConnectionId)
                    {
                        Name = playerName,
                        ConnectionId = Context.ConnectionId,
                        Chips = 1000,
                        // Folded = true,
                    }
                )
                .ToArray();
        }
        await Clients.Group(gameId).SendAsync("UserJoined", Context.ConnectionId);
        await Clients.Group(gameId).SendAsync("GameStateUpdated", Games[gameId]);
    }

    public async Task LeaveGame(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
        await Clients.Group(gameId).SendAsync("UserLeft", Context.ConnectionId);
    }

    public async Task PlayerAction(string gameId, string action, int data)
    {
        // Handle the game state modification based on the action
        await HandlePlayerAction(gameId, action, data);
        await Clients
            .Group(gameId)
            .SendAsync("GameStateUpdated", Games[gameId]);
    }

    private async Task HandlePlayerAction(string gameId, string action, object data)
    {
        if (!Games.ContainsKey(gameId))
            return;
        int index = Array.FindIndex(Games[gameId].Players, p => p.ConnectionId == Context.ConnectionId);
        if (index == -1 || index != Games[gameId].Turn)
            return;
        Games[gameId].Lock.EnterWriteLock();
        switch (action.ToLower())
        {
            case "fold":
                var foldingPlayer = Games[gameId].Players[index];
                if (foldingPlayer != null)
                {
                    foldingPlayer.Folded = true;
                }
                break;
            case "check":
                // No state change needed for check

                break;
            case "call":
                var callingPlayer = Games[gameId].Players[index];
                if (callingPlayer != null)
                {
                    int callAmount = Games[gameId].CurrentBet;
                    if (callingPlayer.Chips >= callAmount)
                    {
                        callingPlayer.Chips -= callAmount;
                        Games[gameId].Pot += callAmount;
                    }
                }
                break;
            case "raise":
                if (data is int raiseAmount)
                {
                    var raisingPlayer = Games[gameId].Players[index];
                    Games[gameId].LastPlayerToRaise = index;
                    if (raisingPlayer != null && raisingPlayer.Chips >= raiseAmount)
                    {
                        raisingPlayer.Chips -= raiseAmount;
                        raisingPlayer.LastBet = raiseAmount;
                        Games[gameId].Pot += raiseAmount;
                        Games[gameId].CurrentBet = raiseAmount;
                    }
                }
                break;
            default:
                Games[gameId].Lock.ExitWriteLock();
                return;
        }
        Games[gameId].AdvanceRound();
        Games[gameId].Lock.ExitWriteLock();
        if (Games[gameId].Round == 0)
        {
            foreach (var player in Games[gameId].Players)
            {
                await Clients.Client(player.ConnectionId).SendAsync("PlayerCards", player.Cards.Select(c => c.ToString()));
            }
        }

        // After handling the action, check if the round should progress
    }

}
