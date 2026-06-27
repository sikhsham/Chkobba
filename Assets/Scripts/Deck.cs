using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the 40-card Chkobba deck.
/// Can be shuffled and drawn from.
/// </summary>
public class Deck
{
    // The cards in the deck, in draw order (top = last item)
    private List<Card> _cards = new List<Card>();

    /// <summary>
    /// Number of cards remaining in the deck.
    /// </summary>
    public int Count => _cards.Count;

    /// <summary>
    /// Creates a fresh, unshuffled Chkobba deck with all 40 cards in suit-rank order.
    /// </summary>
    public Deck()
    {
        // Loop through all 4 suits
        foreach (Card.Suit suit in System.Enum.GetValues(typeof(Card.Suit)))
        {
            // Loop through all 10 ranks (Ace through King)
            foreach (Card.Rank rank in System.Enum.GetValues(typeof(Card.Rank)))
            {
                _cards.Add(new Card(suit, rank));
            }
        }
    }

    /// <summary>
    /// Shuffles the deck using the Fisher-Yates algorithm.
    /// After this, cards are in random order.
    /// </summary>
    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            // Pick a random index from 0 to i
            int j = Random.Range(0, i + 1);
            // Swap cards at i and j
            Card temp = _cards[i];
            _cards[i] = _cards[j];
            _cards[j] = temp;
        }
    }

    /// <summary>
    /// Draws (removes and returns) the top card of the deck.
    /// Returns null if the deck is empty.
    /// </summary>
    public Card Draw()
    {
        if (_cards.Count == 0)
        {
            return null;
        }

        // Take the last card (top of deck)
        int lastIndex = _cards.Count - 1;
        Card drawnCard = _cards[lastIndex];
        _cards.RemoveAt(lastIndex);
        return drawnCard;
    }

    /// <summary>
    /// For debugging: prints all remaining cards in the deck.
    /// </summary>
    public void PrintDeck()
    {
        Debug.Log($"Deck has {Count} cards:");
        for (int i = 0; i < _cards.Count; i++)
        {
            Debug.Log($"  [{i}] {_cards[i]}");
        }
    }
}