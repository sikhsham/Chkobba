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
    // Value is derived from Rank — no separate backing field
    public int Value => (int)CardRank;

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