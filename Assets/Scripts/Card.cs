using UnityEngine;

/// <summary>
/// Represents a single card in the Chkobba deck.
/// A card has a suit, a rank, and a value derived from the rank.
/// </summary>
public class Card
{
    // ---- SUIT ENUM ----
    public enum Suit
    {
        Clubs,
        Diamonds,
        Hearts,
        Spades
    }

    // ---- RANK ENUM ----
    // Order matters: Ace=1, 2-7=face value, Queen=8, Jack=9, King=10
    public enum Rank
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Queen = 8,
        Jack = 9,
        King = 10
    }

    // ---- PROPERTIES ----
    public Suit CardSuit { get; private set; }
    public Rank CardRank { get; private set; }

    // ---- COMPUTED VALUE ----
    // Value is derived from Rank - no separate backing field
    public int Value => (int)CardRank;
    public bool IsChkobba { get; set; }

    /// <summary>
    /// Short display name for logs: rank + suit symbol, e.g. "7\u2666".
    /// Suit symbols use unicode escapes so this file stays pure ASCII.
    /// </summary>
    public string ValueName
    {
        get
        {
            string r = CardRank == Card.Rank.Ace ? "A"
                     : CardRank == Card.Rank.Queen ? "Q"
                     : CardRank == Card.Rank.Jack ? "J"
                     : CardRank == Card.Rank.King ? "K"
                     : ((int)CardRank).ToString();
            char s = CardSuit == Card.Suit.Diamonds ? '\u2666'
                   : CardSuit == Card.Suit.Hearts ? '\u2665'
                   : CardSuit == Card.Suit.Clubs ? '\u2663'
                   : '\u2660';
            return r + s;
        }
    }

    // ---- CONSTRUCTOR ----
    public Card(Suit suit, Rank rank)
    {
        CardSuit = suit;
        CardRank = rank;
    }

    // ---- TO STRING (for debugging) ----
    public override string ToString()
    {
        return $"{CardRank} of {CardSuit} (value={Value})";
    }
}