namespace Poker.Models;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.ObjectPool;

public class Poker
{
    public Player[] Players { get; set; } = [];

    [JsonIgnore]
    public Card[] Deck { get; set; } = [];

    public int Round { get; set; } = 0;
    public int Dealer { get; set; } = 0;
    public int SmallBlindAmount { get; set; } = 1;
    public int BigBlindAmount { get; set; } = 2;
    public int Pot { get; set; } = 0;
    public int CurrentBet { get; set; } = 0;

    public bool Started { get; set; } = false;
    public int LastPlayerToRaise { get; set; } = 0;
    public int Turn { get; set; } = 0;
    public ReaderWriterLockSlim Lock = new();


    [JsonConverter(typeof(CardJsonConverter))]
    public Card[] CommunityCards { get; set; } = [];

    public Poker(Player[]? players = null)
    {
        Players = players ?? [];
        Deck = [];
        foreach (Card card in Enum.GetValues(typeof(Card)))
        {
            Deck = Deck.Append(card).ToArray();
        }
        Shuffle();
        Console.WriteLine(Deck.Length);
        //print deck
        Round = 0;
        Turn = 0;
        CommunityCards = [];
    }

    /// <summary>
    /// numerically scores hand based on poker hand rankings
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    public Hand ScoreHand(Card[] cards)
    {
        if (cards.Length != 7)
        {
            return Hand.HighCard;
        }
        cards = cards.OrderBy(card => card).ToArray();
        Hand returnScore = Hand.HighCard;
        Dictionary<CardValue, int> values = [];
        Dictionary<CardSuit, int> suits = [];
        Dictionary<CardSuit, CardValue[]> valueSuits = [];
        /* high card 0 pair 1 two pair 2 three of a kind 3 straight 4 flush 5 full house 6 four of a kind 7 straight flush 8 */
        foreach (Card card in cards)
        {
            Tuple<CardSuit, CardValue> suitAndValue = SuitAndValue(card);

            //adding to suits and values
            if (!suits.TryAdd(suitAndValue.Item1, 1))
            {
                suits[suitAndValue.Item1] = suits[suitAndValue.Item1] + 1;
            }
            if (!values.TryAdd(suitAndValue.Item2, 1))
            {
                values[suitAndValue.Item2] = values[suitAndValue.Item2] + 1;
            }
            if (!valueSuits.TryAdd(suitAndValue.Item1, [suitAndValue.Item2]))
            {
                valueSuits[suitAndValue.Item1] = valueSuits[suitAndValue.Item1]
                    .Concat([suitAndValue.Item2])
                    .ToArray();
            }

            // Check for card combinations using nested switches
            switch (values[suitAndValue.Item2])
            {
                case 2:
                    switch (returnScore)
                    {
                        case Hand.HighCard:
                            returnScore = Hand.Pair; // Pair
                            break;
                        case Hand.Pair:
                            returnScore = Hand.TwoPair; // Two pair
                            break;
                        case Hand.ThreeOfAKind:
                            returnScore = Hand.FullHouse; // Full house
                            break;
                    }
                    break;
                case 3:
                    switch (returnScore)
                    {
                        case <= Hand.Pair:
                            returnScore = Hand.ThreeOfAKind; // Three of a kind
                            break;
                        case > Hand.Pair
                        and < Hand.FullHouse:
                            returnScore = Hand.FullHouse; // Full house
                            break;
                    }
                    break;
                case 4:
                    returnScore = Hand.FourOfAKind; // Four of a kind
                    return returnScore;
            }
        }
        foreach (CardSuit suit in suits.Keys)
        {
            if (suits[suit] >= 5 && returnScore < Hand.Flush)
            {
                //flush
                returnScore = Hand.Flush;
            }
        }

        int len = values.Keys.ToArray().Length;
        if (len < 5)
        {
            return returnScore;
        }
        for (int i = 0; i < len - 5; i++)
        {
            CardValue[] cardsToCheck = values
                .Keys.OrderBy(value => value)
                .Skip(i)
                .Take(5)
                .ToArray();
            bool sequential = cardsToCheck
                .Zip(cardsToCheck.Skip(1), (current, next) => current + 1 == next)
                .All(isSequential => isSequential);
            foreach (CardSuit suit in valueSuits.Keys)
            {
                if (
                    cardsToCheck.All(value => valueSuits[suit].Any(suit2 => suit2 == value))
                    && sequential
                )
                {
                    returnScore = Hand.StraightFlush;
                    return returnScore;
                }
            }

            if (sequential && returnScore < Hand.Straight)
            {
                returnScore = Hand.Straight;
                break;
            }
        }

        //handle if ace is low part of straight
        foreach (CardSuit suit in valueSuits.Keys)
        {
            if (
                valueSuits[suit].Contains(CardValue.Ace)
                && valueSuits[suit].Contains(CardValue.Two)
                && valueSuits[suit].Contains(CardValue.Three)
                && valueSuits[suit].Contains(CardValue.Four)
                && valueSuits[suit].Contains(CardValue.Five)
            )
            {
                returnScore = Hand.StraightFlush;
                return returnScore;
            }
        }
        if (
            values.Keys.Contains(CardValue.Ace)
            && values.Keys.Contains(CardValue.Two)
            && values.Keys.Contains(CardValue.Three)
            && values.Keys.Contains(CardValue.Four)
            && values.Keys.Contains(CardValue.Five)
        )
        {
            returnScore = Hand.Straight;
        }


        return returnScore;
    }

    /// <summary>
    /// this is called when valid player acts
    /// </summary>
    public void AdvanceRound()
    {

        Turn = (Turn + 1) % Players.Length;
        while (Players[Turn].Folded)
            Turn = (Turn + 1) % Players.Length;
        if (Players.Where(player => !player.Folded).All(player => player.LastBet == CurrentBet))
        {
            if (Round == 4)
            {
                Showdown();
                Shuffle();
                Pot = 0;
                Round = 0;
                Deal();
                Turn = 0;
                foreach (Player player in Players)
                {
                    player.LastBet = 0;
                    player.Folded = false;
                }
            }
            else
            {
                Round++;
                Deal();
                CurrentBet = 0;
                foreach (Player player in Players)
                {
                    player.LastBet = 0;
                }
            }
        }
    }

    public Player? CompareHands(Player p1, Player p2)
    {
        Hand p1Score = ScoreHand(p1.Cards.Concat(CommunityCards).ToArray());
        Hand p2Score = ScoreHand(p2.Cards.Concat(CommunityCards).ToArray());
        if (p1Score != p2Score)
        {
            if (p1Score > p2Score)
            {
                return p1;
            }
            if (p1Score < p2Score)
            {
                return p2;
            }
        }
        p1.Cards = p1.Cards.Concat(CommunityCards).ToArray();
        p2.Cards = p2.Cards.Concat(CommunityCards).ToArray();
        switch (p1Score)
        {
            case Hand.Pair:
                //winner is higher pair; can be equal
                Card[] p1Pair = p1.Cards.Where(card =>
                        p1.Cards.Count(card2 =>
                            SuitAndValue(card2).Item2 == SuitAndValue(card).Item2
                        ) == 2
                    )
                    .ToArray();
                Card[] p2Pair = p2
                    .Cards.Where(card =>
                        p2.Cards.Count(card2 =>
                            SuitAndValue(card2).Item2 == SuitAndValue(card).Item2
                        ) == 2
                    )
                    .ToArray();
                CardValue p1PairValue = SuitAndValue(p1Pair[0]).Item2;
                CardValue p2PairValue = SuitAndValue(p2Pair[0]).Item2;
                if (p1PairValue > p2PairValue)
                {
                    return p1;
                }
                else if (p1PairValue < p2PairValue)
                {
                    return p2;
                }
                else
                {
                    return null;
                }
            case Hand.TwoPair:
                //winner is higher upper pair; can be equal
                Card[] p1TwoPair = p1
                    .Cards.Where(card =>
                        p1.Cards.Count(card2 =>
                            SuitAndValue(card2).Item2 == SuitAndValue(card).Item2
                        ) == 2
                    )
                    .ToArray();
                Card[] p2TwoPair = p2
                    .Cards.Where(card =>
                        p2.Cards.Count(card2 =>
                            SuitAndValue(card2).Item2 == SuitAndValue(card).Item2
                        ) == 2
                    )
                    .ToArray();
                CardValue upper1 = p1TwoPair.Max(card => SuitAndValue(card).Item2);
                CardValue upper2 = p2TwoPair.Max(card => SuitAndValue(card).Item2);
                if (upper1 > upper2)
                {
                    return p1;
                }
                else if (upper1 < upper2)
                {
                    return p2;
                }
                else
                {
                    return null;
                }
            case Hand.ThreeOfAKind:
                //winner is higher three of a kind; can not be equal
                Card[] p1ThreeOfAKind = p1
                    .Cards.Where(card =>
                        p1.Cards.Count(card2 =>
                            SuitAndValue(card2).Item2 == SuitAndValue(card).Item2
                        ) == 3
                    )
                    .ToArray();
                Card[] p2ThreeOfAKind = p2
                    .Cards.Where(card =>
                        p2.Cards.Count(card2 =>
                            SuitAndValue(card2).Item2 == SuitAndValue(card).Item2
                        ) == 3
                    )
                    .ToArray();
                CardValue three1 = SuitAndValue(p1ThreeOfAKind[0]).Item2;
                CardValue three2 = SuitAndValue(p2ThreeOfAKind[0]).Item2;

                if (three1 > three2)
                {
                    return p1;
                }
                else if (three1 < three2)
                {
                    return p2;
                }
                else
                {
                    throw new Exception("Three of a kind is equal, something is wrong");
                }
            case Hand.Straight
            or Hand.StraightFlush:
                //winner is higher straight; can be equal; this can get weird because A12345 loses to 123456 since Ace is a low card
                CardValue p1MaxStraight = CardValue.Two;
                CardValue p2MaxStraight = CardValue.Two;
                HashSet<CardValue> p1Values = new HashSet<CardValue>(
                    p1.Cards.Select(card => SuitAndValue(card).Item2)
                );
                HashSet<CardValue> p2Values = new HashSet<CardValue>(
                    p2.Cards.Select(card => SuitAndValue(card).Item2)
                );
                CardValue[] ary1 = p1Values.Order().ToArray();
                CardValue[] ary2 = p2Values.Order().ToArray();

                for (int i = 0; i < ary1.Length - 5; i++)
                {
                    CardValue[] cardsToCheck = ary1.Skip(i).Take(5).ToArray();
                    bool sequential = cardsToCheck
                        .Zip(cardsToCheck.Skip(1), (current, next) => current + 1 == next)
                        .All(isSequential => isSequential);
                    if (sequential && !cardsToCheck.Contains(CardValue.Ace))
                    {
                        p1MaxStraight = cardsToCheck.Max(card => card);
                    }
                    else if (sequential)
                    {
                        if (cardsToCheck.Contains(CardValue.King))
                        {
                            p1MaxStraight = CardValue.Ace;
                        }
                        else
                        {
                            p1MaxStraight = CardValue.Five;
                        }
                    }
                }
                for (int i = 0; i < ary2.Length - 5; i++)
                {
                    CardValue[] cardsToCheck = ary2.Skip(i).Take(5).ToArray();
                    bool sequential = cardsToCheck
                        .Zip(cardsToCheck.Skip(1), (current, next) => current + 1 == next)
                        .All(isSequential => isSequential);
                    if (sequential && !cardsToCheck.Contains(CardValue.Ace))
                    {
                        p2MaxStraight = cardsToCheck.Max(card => card);
                    }
                    else if (sequential)
                    {
                        if (cardsToCheck.Contains(CardValue.King))
                        {
                            p2MaxStraight = CardValue.Ace;
                        }
                        else
                        {
                            p2MaxStraight = CardValue.Five;
                        }
                    }
                }
                if (p1MaxStraight > p2MaxStraight)
                {
                    return p1;
                }
                else if (p1MaxStraight < p2MaxStraight)
                {
                    return p2;
                }
                else
                {
                    return null;
                }
            case Hand.Flush:
                //winner is highest card of flush; can be equal
                CardValue p1MaxValue = CardValue.Two;
                CardValue p2MaxValue = CardValue.Two;
                foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
                {
                    if (p1.Cards.Count(card => SuitAndValue(card).Item1 == suit) >= 5)
                    {
                        p1MaxValue = SuitAndValue(
                            p1.Cards.Where(card => SuitAndValue(card).Item1 == suit).Max()
                        ).Item2;
                    }
                    if (p2.Cards.Count(card => SuitAndValue(card).Item1 == suit) >= 5)
                    {
                        p2MaxValue = SuitAndValue(
                            p2.Cards.Where(card => SuitAndValue(card).Item1 == suit).Max()
                        ).Item2;
                    }
                }
                if (p1MaxValue > p2MaxValue)
                {
                    return p1;
                }
                else if (p1MaxValue < p2MaxValue)
                {
                    return p2;
                }
                else
                {
                    return null;
                }

            case Hand.FullHouse:
                //winner is higher three of a kind; can not be equal
                Card[] p1FullHouseThreeOfAKind = p1
                    .Cards.Where(card =>
                        p1.Cards.Count(card2 =>
                            SuitAndValue(card2).Item2 == SuitAndValue(card).Item2
                        ) == 3
                    )
                    .ToArray();
                Card[] p2FullHouseThreeOfAKind = p2
                    .Cards.Where(card =>
                        p2.Cards.Count(card2 =>
                            SuitAndValue(card2).Item2 == SuitAndValue(card).Item2
                        ) == 3
                    )
                    .ToArray();
                CardValue threefh1 = SuitAndValue(p1FullHouseThreeOfAKind[0]).Item2;
                CardValue threefh2 = SuitAndValue(p2FullHouseThreeOfAKind[0]).Item2;
                if (threefh1 > threefh2)
                {
                    return p1;
                }
                else if (threefh1 < threefh2)
                {
                    return p2;
                }
                else
                {
                    throw new Exception("Three of a kind is equal, something is wrong");
                }
            case Hand.FourOfAKind:
                //winner is higher four of a kind; can not be equal
                Card[] p1FourOfAKind = p1
                    .Cards.Where(card =>
                        p1.Cards.Count(card2 =>
                            SuitAndValue(card2).Item2 == SuitAndValue(card).Item2
                        ) == 3
                    )
                    .ToArray();
                Card[] p2FourOfAKind = p2
                    .Cards.Where(card =>
                        p2.Cards.Count(card2 =>
                            SuitAndValue(card2).Item2 == SuitAndValue(card).Item2
                        ) == 3
                    )
                    .ToArray();
                CardValue four1 = SuitAndValue(p1FourOfAKind[0]).Item2;
                CardValue four2 = SuitAndValue(p2FourOfAKind[0]).Item2;
                if (four1 > four2)
                {
                    return p1;
                }
                else if (four1 < four2)
                {
                    return p2;
                }
                else
                {
                    throw new Exception("Four of a kind is equal, something is wrong");
                }
            case Hand.HighCard:
                //winner is higher card; can be equal
                CardValue p1MaxHighCard = p1.Cards.Max(e => SuitAndValue(e).Item2);
                CardValue p2MaxHighCard = p2.Cards.Max(e => SuitAndValue(e).Item2);

                if (p1MaxHighCard > p2MaxHighCard)
                {
                    return p1;
                }
                else if (p1MaxHighCard < p2MaxHighCard)
                {
                    return p2;
                }
                else
                {
                    return null;
                }
            default:
                return null;
        }
    }

    public Tuple<CardSuit, CardValue> SuitAndValue(Card card)
    {
        CardSuit suit = (CardSuit)Enum.ToObject(typeof(CardSuit), Convert.ToInt32(card) % 4);
        CardValue value = (CardValue)Enum.ToObject(typeof(CardValue), Convert.ToInt32(card) / 4);
        return new Tuple<CardSuit, CardValue>(suit, value);
    }

    public void Shuffle()
    {
        Deck = new Card[0];
        foreach (Card card in Enum.GetValues(typeof(Card)))
        {
            Deck = Deck.Append(card).ToArray();
        }
        Deck = Deck.OrderBy(card => Guid.NewGuid()).ToArray();
    }

    public void Deal()
    {
        switch (Round)
        {
            case 0:
                Shuffle();
                CommunityCards = new Card[0];
                foreach (Player player in Players)
                {
                    player.Cards = new Card[2];
                    player.Cards[0] = Deck[0];
                    player.Cards[1] = Deck[1];
                    Deck = Deck.Skip(2).ToArray();
                }
                break;
            case 1:
                CommunityCards = new Card[3];
                for (int i = 0; i < 3; i++)
                {
                    CommunityCards[i] = Deck[0];
                    Deck = Deck.Skip(1).ToArray();
                }
                break;
            case 2
            or 3:
                CommunityCards = CommunityCards.Append(Deck[0]).ToArray();
                Deck = Deck.Skip(1).ToArray();
                break;

            default:
                return;
        }
    }

    /*
    this is called when everyone has bet and all 5 community cards are dealt
    this is where the showdown happens
    the pot is split between the players
    the players who went all in get their money back
    the players who lost the pot lose money
    the players who won the pot win money

    */
    public void Showdown()
    {
        Player[] players = Players.Where(player => !player.Folded).ToArray();
        Hand[] hands = players
            .Select(player => ScoreHand(player.Cards.Concat(CommunityCards).ToArray()))
            .ToArray();
        Hand maxHand = hands.Max();
        Tuple<Hand, Player>[] winners = hands
            .Zip(players, (hand, player) => new Tuple<Hand, Player>(hand, player))
            .Where((handAndPlayer) => handAndPlayer.Item1 == maxHand)
            .ToArray();

        if (winners.Length == 1)
        {
            winners[0].Item2.Chips += Pot;
        }
        else
        {
            Tuple<Hand, Player>[] realWinners = [winners[0]];
            for (int i = 1; i < winners.Length; i++)
            {
                Player? betterHand = CompareHands(realWinners[^1].Item2, winners[i].Item2);
                if (betterHand == null)
                {
                    realWinners.Append(winners[i]);
                }
                else if (betterHand == winners[i].Item2)
                {
                    realWinners = [winners[i]];
                }
            }
            foreach (Player p in realWinners.Select(tuple => tuple.Item2))
            {
                p.Chips += Pot / realWinners.Length;
            }
        }
    }

}
