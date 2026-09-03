using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class HotSeatHandEvaluatorTests
{
    private MethodInfo evaluateMethod;

    [SetUp]
    public void SetUp()
    {
        evaluateMethod = typeof(HotSeatSetupUI).GetMethod(
            "EvaluateCardsForHand", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(evaluateMethod, Is.Not.Null);
    }

    [Test]
    public void ThreeAces_AreRecognizedEvenWithExtraCards()
    {
        List<CardSpriteEntry> cards = new List<CardSpriteEntry>
        {
            Card(CardSuit.Kier, CardRank.Ace),
            Card(CardSuit.Karo, CardRank.Ace),
            Card(CardSuit.Pik, CardRank.Ace),
            Card(CardSuit.Trefl, CardRank.Nine)
        };

        Assert.That(Evaluate("TRIPS_A", cards), Is.True);
    }

    [Test]
    public void EveryCatalogHand_AcceptsMatchingCards_AndRejectsIncompleteCards()
    {
        foreach (string handId in HandRankCatalog.GetAllIds())
        {
            List<CardSpriteEntry> matching = BuildMatchingCards(handId);
            Assert.That(Evaluate(handId, matching), Is.True,
                "Matching cards rejected for " + handId);

            matching.RemoveAt(matching.Count - 1);
            Assert.That(Evaluate(handId, matching), Is.False,
                "Incomplete cards accepted for " + handId);
        }
    }

    private bool Evaluate(string handId, List<CardSpriteEntry> cards)
    {
        return (bool)evaluateMethod.Invoke(null, new object[] { handId, cards });
    }

    private static List<CardSpriteEntry> BuildMatchingCards(string handId)
    {
        string[] parts = handId.Split('_');
        List<CardSpriteEntry> cards = new List<CardSpriteEntry>();

        if (handId.StartsWith("HIGH_"))
            AddCopies(cards, Rank(parts[1]), 1);
        else if (handId.StartsWith("PAIR_"))
            AddCopies(cards, Rank(parts[1]), 2);
        else if (handId.StartsWith("TRIPS_"))
            AddCopies(cards, Rank(parts[1]), 3);
        else if (handId.StartsWith("QUADS_"))
            AddCopies(cards, Rank(parts[1]), 4);
        else if (handId.StartsWith("TWOPAIR_"))
        {
            AddCopies(cards, Rank(parts[1]), 2);
            AddCopies(cards, Rank(parts[2]), 2);
        }
        else if (handId.StartsWith("FULL_"))
        {
            AddCopies(cards, Rank(parts[1]), 3);
            AddCopies(cards, Rank(parts[2]), 2);
        }
        else if (handId == "STRAIGHT_SMALL")
            AddRanks(cards, CardSuit.Kier, CardRank.Nine, CardRank.Ten,
                CardRank.Jack, CardRank.Queen, CardRank.King);
        else if (handId == "STRAIGHT_BIG")
            AddRanks(cards, CardSuit.Kier, CardRank.Ten, CardRank.Jack,
                CardRank.Queen, CardRank.King, CardRank.Ace);
        else if (handId.StartsWith("FLUSH_"))
            AddRanks(cards, Suit(parts[1]), CardRank.Nine, CardRank.Ten,
                CardRank.Jack, CardRank.Queen, CardRank.King);
        else if (handId.StartsWith("POKER_SMALL_"))
            AddRanks(cards, Suit(parts[2]), CardRank.Nine, CardRank.Ten,
                CardRank.Jack, CardRank.Queen, CardRank.King);
        else if (handId.StartsWith("POKER_BIG_"))
            AddRanks(cards, Suit(parts[2]), CardRank.Ten, CardRank.Jack,
                CardRank.Queen, CardRank.King, CardRank.Ace);

        return cards;
    }

    private static void AddCopies(List<CardSpriteEntry> cards, CardRank rank, int count)
    {
        for (int i = 0; i < count; i++)
            cards.Add(Card((CardSuit)i, rank));
    }

    private static void AddRanks(
        List<CardSpriteEntry> cards, CardSuit suit, params CardRank[] ranks)
    {
        foreach (CardRank rank in ranks)
            cards.Add(Card(suit, rank));
    }

    private static CardSpriteEntry Card(CardSuit suit, CardRank rank)
    {
        return new CardSpriteEntry { suit = suit, rank = rank };
    }

    private static CardRank Rank(string value)
    {
        switch (value)
        {
            case "9": return CardRank.Nine;
            case "10": return CardRank.Ten;
            case "J": return CardRank.Jack;
            case "Q": return CardRank.Queen;
            case "K": return CardRank.King;
            case "A": return CardRank.Ace;
            default: throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }
    }

    private static CardSuit Suit(string value)
    {
        switch (value)
        {
            case "HEART": return CardSuit.Kier;
            case "DIAMOND": return CardSuit.Karo;
            case "SPADE": return CardSuit.Pik;
            case "CLUB": return CardSuit.Trefl;
            default: throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }
    }
}
