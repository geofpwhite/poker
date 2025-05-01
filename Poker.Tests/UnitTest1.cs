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
        var mockHubContext = new Mock<IHubContext<PokerHub>>();
        var pokerHub = new PokerHub(mockHubContext.Object);

        var player1 = new Player { Name = "Player1", ConnectionId = "conn1" };
        var player2 = new Player { Name = "Player2", ConnectionId = "conn2" };

        // Act
        await pokerHub.JoinGame("game1", player1);
        await pokerHub.JoinGame("game1", player2);
        await pokerHub.StartGame("game1");

        await pokerHub.Check("game1", player1);
        await pokerHub.Check("game1", player2);

        // Assert
        var game = PokerHub.Games["game1"];
        Assert.NotNull(game);
        Assert.Equal(2, game.Players.Count);
    }
}
