using UnityEngine;

/// <summary>
/// Makes a card GameObject clickable and tracks its data.
/// </summary>
public class CardObject : MonoBehaviour
{
    public Card Card { get; private set; }
    public bool IsFaceUp { get; private set; }

    public void Setup(Card card, bool faceUp)
    {
        Card = card;
        IsFaceUp = faceUp;
    }

    public void Reveal()
    {
        IsFaceUp = true;
        GetComponent<SpriteRenderer>().sprite = CardSpriteLoader.GetSpriteForCard(Card);
    }

    void OnMouseDown()
    {
        if (!IsFaceUp) return;
        GameManager.Instance.PlayCard(this);
    }
}
