using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

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

    // ---- Position tweening (smooth transitions) ----

    private Coroutine tweenRoutine;

    /// <summary>
    /// Glides the card to a world position. A newer call replaces the older tween.
    /// </summary>
    public void TweenTo(Vector3 worldPos, float duration = 0.25f)
    {
        if (tweenRoutine != null) StopCoroutine(tweenRoutine);
        tweenRoutine = StartCoroutine(TweenRoutine(worldPos, duration));
    }

    private IEnumerator TweenRoutine(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }
        transform.position = target;
        tweenRoutine = null;
    }
}
