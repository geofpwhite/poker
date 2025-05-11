using Poker.Models;
using Poker.Hubs;

using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Poker.Tests;

public class UnitTest1
{
    [Fact]
    public async Task TwoPlayersJoinAndPlayGame()
    {
        // Arrange
        var player1 = new Player("Player1") { Name = "Player1", Chips = 100 };
        var player2 = new Player("Player2") { Name = "Player2", Chips = 100 };
        var players = new[] { player1, player2 };
        var game = new PokerGame(players);
        var mockContext = new Mock<HubCallerContext>();
        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(clients => clients.All).Returns(mockClientProxy.Object);

    }
}
