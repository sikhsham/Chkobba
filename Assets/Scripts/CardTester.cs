using UnityEngine;

public class CardTester : MonoBehaviour
{
    void Start()
    {
        TestCardDisplay();
    }

    void TestCardDisplay()
    {
        // Create test cards
        Card[] testCards = new Card[]
        {
            new Card(Card.Suit.Diamonds, Card.Rank.Seven),
            new Card(Card.Suit.Clubs, Card.Rank.Ace),
            new Card(Card.Suit.Hearts, Card.Rank.Queen),
            new Card(Card.Suit.Spades, Card.Rank.King),
            new Card(Card.Suit.Diamonds, Card.Rank.Jack)
        };

        // Display each card on screen, spaced horizontally
        float spacing = 2.5f;
        float startX = -((testCards.Length - 1) * spacing) / 2f; // center them

        for (int i = 0; i < testCards.Length; i++)
        {
            Card card = testCards[i];
            Sprite sprite = CardSpriteLoader.GetSpriteForCard(card);

            if (sprite != null)
            {
                // Create a new GameObject to display this card
                GameObject cardObj = new GameObject($"Card_{card.CardRank}_{card.CardSuit}");
                
                // Add a SpriteRenderer component (displays 2D images)
                SpriteRenderer renderer = cardObj.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;

                // Position the card on screen
                cardObj.transform.position = new Vector3(startX + i * spacing, 0, 0);

                // Scale it up a bit (Kenney cards are small PNGs)
                cardObj.transform.localScale = new Vector3(2f, 2f, 1f);

                Debug.Log($"Displayed: {card} at position {cardObj.transform.position}");
            }
        }
    }
}