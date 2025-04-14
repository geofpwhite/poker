namespace Poker.Models;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc.Rendering;

public class Poker
{
    public Player[] Players { get; set; } = [];
    public Card[] Deck { get; set; } = [];

    public int Round { get; set; } = 0;
    public int Dealer { get; set; } = 0;
    public int SmallBlind { get; set; } = 1;
    public int BigBlind { get; set; } = 2;
    public int Pot { get; set; } = 0;
    public int CurrentBet { get; set; } = 0;
    public int Turn { get; set; } = 0;
    public Card[] CommunityCards { get; set; } = [];

    public Poker(Player[]? players = null)
    {
        Players = players ?? [];
        Deck = new Card[0];
        foreach (Card card in Enum.GetValues(typeof(Card)))
        {
            Deck = Deck.Append(card).ToArray();
        }
        Shuffle();
        Console.WriteLine(Deck.Length);
        //print deck
        Round = 0;
        Turn = 0;
        CommunityCards = new Card[0];
    }


    public int ScoreHand(Card[] cards)
    {
        if (cards.Length != 7)
        {
            return -1;
        }
        cards = cards.OrderBy(card => card).ToArray();
        int returnScore = 0;
        Dictionary<CardValue, int> values = [];
        Dictionary<CardSuit, int> suits = [];
        Dictionary<CardSuit, CardValue[]> valueSuits = [];
        /*
         * 
         * high card 0 
         * pair 1
         * two pair 2
         * three of a kind 3
         * straight 4
         * flush 5
         * full house 6
         * four of a kind 7
         * straight flush 8
         */
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
                valueSuits[suitAndValue.Item1] = valueSuits[suitAndValue.Item1].Concat([suitAndValue.Item2]).ToArray();
            }

            // Check for card combinations using nested switches
            switch (values[suitAndValue.Item2])
            {
                case 2:
                    switch (returnScore)
                    {
                        case 0:
                            returnScore = 1; // Pair
                            break;
                        case 1:
                            returnScore = 2; // Two pair
                            break;
                        case 3:
                            returnScore = 6; // Full house
                            break;
                    }
                    break;
                case 3:
                    switch (returnScore)
                    {
                        case <= 1:
                            returnScore = 3; // Three of a kind
                            break;
                        case > 1 and < 6:
                            returnScore = 6; // Full house
                            break;
                    }
                    break;
                case 4:
                    returnScore = 7; // Four of a kind
                    return returnScore;
            }
        }
        foreach (CardSuit suit in suits.Keys)
        {
            if (suits[suit] >= 5 && returnScore < 5)
            {
                //flush
                returnScore = 5;
            }
        }

        int len = values.Keys.ToArray().Length;
        if (len < 5)
        {
            return returnScore;
        }
        for (int i = 0; i < len - 5; i++)
        {
            CardValue[] cardsToCheck = values.Keys.OrderBy(value => value).Skip(i).Take(5).ToArray();
            bool sequential = cardsToCheck.Zip(cardsToCheck.Skip(1), (current, next) => current + 1 == next).All(isSequential => isSequential);
            foreach (CardSuit suit in valueSuits.Keys)
            {
                if (cardsToCheck.All(value => valueSuits[suit].Any(suit2 => suit2 == value)))
                {
                    if (sequential)
                    {
                        returnScore = 8;
                        return returnScore;
                    }
                }
            }

            if (sequential && returnScore < 4)
            {
                returnScore = 4;
                break;
            }
        }
        
        //handle if ace is low part of straight
        foreach(CardSuit suit in valueSuits.Keys){
            if (valueSuits[suit].Contains(CardValue.Ace) && valueSuits[suit].Contains(CardValue.Two) && valueSuits[suit].Contains(CardValue.Three) && valueSuits[suit].Contains(CardValue.Four) && valueSuits[suit].Contains(CardValue.Five)){
                returnScore = 8;
                return returnScore;
            }
        }
        if (values.Keys.Contains(CardValue.Ace) && values.Keys.Contains(CardValue.Two) && values.Keys.Contains(CardValue.Three) && values.Keys.Contains(CardValue.Four) && values.Keys.Contains(CardValue.Five))
        {
            returnScore = 4;
        }

        //check for straight flush

        return returnScore;
    }


    public Player? CompareHands(Player p1, Player p2)
    {
        int p1Score = ScoreHand(p1.Cards.Concat(CommunityCards).ToArray());
        int p2Score = ScoreHand(p2.Cards.Concat(CommunityCards).ToArray());
        if (p1Score == p2Score)
        {
            //could be any hand
        }
        return null;
    }

    public Tuple<CardSuit, CardValue> SuitAndValue(Card card)
    {
        CardSuit suit;
        CardValue value;
        suit = (CardSuit)Enum.ToObject(typeof(CardSuit), Convert.ToInt32(card) % 4);
        value = (CardValue)Enum.ToObject(typeof(CardValue), Convert.ToInt32(card) / 4);
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
                foreach (var player in Players)
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
            case 2 or 3:
                CommunityCards = CommunityCards.Append(Deck[0]).ToArray();
                Deck = Deck.Skip(1).ToArray();
                break;

        }
    }
    public void Showdown()
    {

    }


    // go through players until bet is settled, add cards to community cards
    public void Bet()
    {
        Players[Dealer + 1 % Players.Length].Chips -= SmallBlind;
        Pot += SmallBlind;
        Players[Dealer + 2 % Players.Length].Chips -= BigBlind;
        Pot += BigBlind;
        
        CurrentBet = BigBlind;
        int lastRaiserIndex = Dealer+2 % Players.Length;
    }
}