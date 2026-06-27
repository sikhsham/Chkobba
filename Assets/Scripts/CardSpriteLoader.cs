using UnityEngine;

/// <summary>
/// Helper class that loads the correct Sprite for a given Card.
/// Looks up sprites from Resources/CardSprites/ by filename.
/// </summary>
public static class CardSpriteLoader
{
    /// <summary>
    /// Loads the Sprite that matches the given Card.
    /// Returns null if the sprite can't be found.
    /// </summary>
    public static Sprite GetSpriteForCard(Card card)
    {
        if (card == null)
        {
            Debug.LogWarning("CardSpriteLoader: card is null");
            return null;
        }

        string spriteName = GetSpriteNameForCard(card);
        Sprite sprite = Resources.Load<Sprite>($"CardSprites/{spriteName}");

        if (sprite == null)
        {
            Debug.LogError($"CardSpriteLoader: Could not find sprite '{spriteName}' for card {card}");
        }

        return sprite;
    }

    /// <summary>
    /// Loads the card back sprite (face-down card).
    /// </summary>
    public static Sprite GetCardBackSprite()
    {
        Sprite sprite = Resources.Load<Sprite>("CardSprites/card_back");

        if (sprite == null)
        {
            Debug.LogError("CardSpriteLoader: Could not find card_back sprite");
        }

        return sprite;
    }

    /// <summary>
    /// Builds the filename for a card's sprite.
    /// Example: 7 of Diamonds → "card_diamonds_07"
    ///          Ace of Clubs → "card_clubs_A"
    ///          Queen of Hearts → "card_hearts_Q"
    /// </summary>
    private static string GetSpriteNameForCard(Card card)
    {
        // Suit name (lowercase): clubs, diamonds, hearts, spades
        string suitName = card.CardSuit.ToString().ToLower();

        // Rank part of the filename
        string rankPart;
        switch (card.CardRank)
        {
            case Card.Rank.Ace:
                rankPart = "A";
                break;
            case Card.Rank.Queen:
                rankPart = "Q";
                break;
            case Card.Rank.Jack:
                rankPart = "J";
                break;
            case Card.Rank.King:
                rankPart = "K";
                break;
            default:
                // Number cards: 02, 03, 04, 05, 06, 07 (zero-padded)
                rankPart = ((int)card.CardRank).ToString("00");
                break;
        }

        return $"card_{suitName}_{rankPart}";
    }
}