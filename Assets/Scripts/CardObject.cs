using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Makes a card GameObject clickable (touch + mouse) and tracks its data.
/// Supports selection visual feedback for manual capture flow.
/// </summary>
public class CardObject : MonoBehaviour, IPointerClickHandler
{
    public Card Card { get; private set; }
    public bool IsFaceUp { get; private set; }

    // Visual feedback
    private SpriteRenderer sr;
    private Color originalColor;
    private static readonly Color selectedColor = new Color(1f, 1f, 0.5f, 1f); // yellow tint
    private static readonly Color dimColor = new Color(0.6f, 0.6f, 0.6f, 1f);   // gray dim

    private bool selected;

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

    // ---- Touch/Mouse input via EventSystem ----

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsFaceUp) return;
        GameManager.Instance.OnCardClicked(this);
    }

    // ---- Selection visual ----

    public void SetSelected(bool on)
    {
        selected = on;
        sr = GetComponent<SpriteRenderer>();
        sr.color = on ? selectedColor : originalColor;
    }

    public bool Selected => selected;

    public void SetDim(bool on)
    {
        sr = GetComponent<SpriteRenderer>();
        sr.color = on ? dimColor : originalColor;
    }

    /// <summary>
    /// Stores the original sprite color. Call once after sprite is assigned.
    /// </summary>
    public void InitColor()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }
}
