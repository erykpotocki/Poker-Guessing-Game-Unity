using System;
using UnityEngine;

public enum CardSuit
{
    Kier,
    Karo,
    Pik,
    Trefl
}

public enum CardRank
{
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace
}

[Serializable]
public class CardSpriteEntry
{
    public CardSuit suit;
    public CardRank rank;
    public Sprite sprite;
}

public class CardDatabase : MonoBehaviour
{
    public CardSpriteEntry[] cards;
    [SerializeField] private CardSpriteEntry[] multiplayerCards;

    public CardSpriteEntry[] GetMultiplayerCards()
    {
        return multiplayerCards != null && multiplayerCards.Length > 0
            ? multiplayerCards
            : cards;
    }

    public Sprite GetCardSprite(CardSuit suit, CardRank rank)
    {
        if (cards == null || cards.Length == 0)
            return null;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i].suit == suit && cards[i].rank == rank)
                return cards[i].sprite;
        }

        return null;
    }
}
