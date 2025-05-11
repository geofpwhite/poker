using Poker.Models;
using Xunit;

namespace Poker.Tests;

public class PokerGameTests
{
    [Fact]
    public void Shuffle_ShouldRandomizeDeck()
    {
        // Arrange
        Player player1 = new("Player1") { Name = "Player1", Chips = 100 };
        var players = new Player[] { player1 };
        // var game = new PokerGame(players);
        var game = new Models.PokerGame { Players = players };
        var originalDeck = game.Deck.ToArray();

        // Act
        game.Shuffle();

        // Assert
        Assert.NotEqual(originalDeck, game.Deck);
        Assert.Equal(52, game.Deck.Length);
    }

    [Fact]
    public void Deal_ShouldGiveEachPlayerTwoCards()
    {
        // Arrange
        Player player1 = new("Player1") { Name = "Player1", Chips = 100 };
        var players = new Player[] { player1 };
        var game = new PokerGame(players);

        // Act
        game.Deal();

        // Assert
        foreach (var player in game.Players)
        {
            Assert.NotNull(player.Cards);
            Assert.Equal(2, player.Cards.Length);
        }
        Assert.Equal(50, game.Deck.Length); // 50 - (1 players * 2 cards)
    }
    [Fact]
    public void ScoreHand_ShouldReturnCorrectScore()
    {
        // Arrange
        var game = new PokerGame();
        var hand = new Card[] { Card.AceH, Card.AceD, Card.TwoH, Card.ThreeD, Card.FourC, Card.EightC, Card.SevenS };
        var score = game.ScoreHand(hand);
        Assert.Equal(Hand.Pair, score);

        hand = new Card[] { Card.AceH, Card.AceD, Card.TwoH, Card.TwoD, Card.FourC, Card.FiveS, Card.SixS };
        score = game.ScoreHand(hand);
        Assert.Equal(Hand.TwoPair, score);

        hand = new Card[] { Card.AceH, Card.AceD, Card.AceC, Card.TwoD, Card.FourC, Card.SevenS, Card.SixS };
        score = game.ScoreHand(hand);
        Assert.Equal(Hand.ThreeOfAKind, score);

        hand = new Card[] { Card.KingH, Card.KingD, Card.TwoH, Card.ThreeD, Card.FourC, Card.FiveS, Card.SixS };
        score = game.ScoreHand(hand);
        Assert.Equal(Hand.Straight, score);
        hand = new Card[] { Card.AceH, Card.KingD, Card.TwoH, Card.ThreeD, Card.FourC, Card.FiveS, Card.SevenS };
        score = game.ScoreHand(hand);
        Assert.Equal(Hand.Straight, score);

        hand = new Card[] { Card.AceH, Card.AceD, Card.TwoH, Card.ThreeH, Card.FourH, Card.SevenH, Card.SixH };
        score = game.ScoreHand(hand);
        Assert.Equal(Hand.Flush, score);

        hand = new Card[] { Card.AceH, Card.TwoH, Card.AceD, Card.TwoD, Card.TwoC, Card.FiveS, Card.SixS };
        score = game.ScoreHand(hand);
        Assert.Equal(Hand.FullHouse, score);

        hand = new Card[] { Card.AceH, Card.AceD, Card.AceS, Card.AceC, Card.FourC, Card.FiveS, Card.SixS };
        score = game.ScoreHand(hand);
        Assert.Equal(Hand.FourOfAKind, score);


        hand = new Card[] { Card.AceH, Card.TwoH, Card.ThreeH, Card.FourH, Card.FiveH, Card.SixC, Card.SevenC };
        score = game.ScoreHand(hand);
        Assert.Equal(Hand.StraightFlush, score);


    }
}