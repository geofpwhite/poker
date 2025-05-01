using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using Poker.Models;

namespace Poker.Services;

public class PokerHubService
{
    private HubConnection? _hubConnection;
    private readonly NavigationManager _navigationManager;

    public PokerHubService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public async Task StartConnectionAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/pokerhub"))
            .WithAutomaticReconnect()
            .Build();


        await _hubConnection.StartAsync();
    }

    public async Task JoinGameAsync(string playerName, string gameId)
    {
        if (_hubConnection is not null)
        {
            Console.WriteLine("joined game");
            await _hubConnection.InvokeAsync("JoinGame", playerName, gameId);
        }
    }

    public async Task StartGameAsync(string gameId)
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.InvokeAsync("StartGame", gameId);
        }
    }

    public async Task PlayerActionAsync(string gameId, string action, int amount = 0)
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.InvokeAsync("PlayerAction", gameId, action, amount);
        }
    }

    public async Task CloseGameAsync(string gameId)
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.InvokeAsync("CloseGame", gameId);
        }
    }

    public async Task CreateGameAsync(string gameId)
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.InvokeAsync("CreateGame", gameId);
        }
    }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
}