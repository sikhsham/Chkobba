using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the Chkobba game state, dealing, and card interaction.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Card Layout")]
    [SerializeField] private float maxSpacing = 1.2f;
    [SerializeField] private float cardScale = 2f;
    [SerializeField] private float playerY = -2f;
    [SerializeField] private float aiY = 2f;
    [SerializeField] private float tableY = 0f;

    // How much screen width to use (world units). Camera size 3 at 16:9 = 10.67 wide, leave margin
    private const float visibleWidth = 9f;

    private Deck deck;
    private List<CardObject> playerHand = new List<CardObject>();
    private List<CardObject> aiHand = new List<CardObject>();
    private List<CardObject> tableCards = new List<CardObject>();

    private bool isPlayerTurn = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartRound();
    }

    void StartRound()
    {
        deck = new Deck();
        deck.Shuffle();

        for (int i = 0; i < 3; i++) playerHand.Add(CreateCardObject(deck.Draw(), true));
        for (int i = 0; i < 3; i++) aiHand.Add(CreateCardObject(deck.Draw(), false));
        for (int i = 0; i < 4; i++) tableCards.Add(CreateCardObject(deck.Draw(), true));

        Debug.Log($"Dealt: {playerHand.Count} player, {aiHand.Count} AI, {tableCards.Count} table, {deck.Count} left");
        RenderAll();
    }

    CardObject CreateCardObject(Card card, bool faceUp)
    {
        var obj = new GameObject();
        var co = obj.AddComponent<CardObject>();
        co.Setup(card, faceUp);
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = faceUp ? CardSpriteLoader.GetSpriteForCard(card) : CardSpriteLoader.GetCardBackSprite();
        obj.transform.localScale = new Vector3(cardScale, cardScale, 1);
        obj.AddComponent<BoxCollider2D>();
        return co;
    }

    public void PlayCard(CardObject card)
    {
        if (!isPlayerTurn) return;
        if (!playerHand.Contains(card)) return;

        playerHand.Remove(card);
        tableCards.Add(card);

        Debug.Log($"Player played {card.Card}. Table: {tableCards.Count}");
        RenderAll();

        isPlayerTurn = false;
        Invoke(nameof(AiTurn), 1f);
    }

    void AiTurn()
    {
        if (aiHand.Count == 0) return;
        var card = aiHand[0];
        aiHand.RemoveAt(0);
        tableCards.Add(card);
        card.Reveal();

        Debug.Log($"AI played {card.Card}. Table: {tableCards.Count}");
        RenderAll();

        isPlayerTurn = true;
    }

    void RenderAll()
    {
        RenderGroup(playerHand, playerY, maxSpacing);
        RenderGroup(aiHand, aiY, maxSpacing);
        RenderGroup(tableCards, tableY, true);
    }

    void RenderGroup(List<CardObject> cards, float yPos, bool compress)
    {
        float spacing = compress ? CalcCompressedSpacing(cards.Count) : maxSpacing;
        RenderGroup(cards, yPos, spacing);
    }

    void RenderGroup(List<CardObject> cards, float yPos, float spacing)
    {
        if (cards.Count == 0) return;
        float startX = -((cards.Count - 1) * spacing) / 2f;
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.position = new Vector3(startX + i * spacing, yPos, 0);
            // Later cards render on top (overlap = right card on top of left)
            cards[i].GetComponent<SpriteRenderer>().sortingOrder = i;
        }
    }

    /// <summary>
    /// Compress spacing so all cards fit in visibleWidth.
    /// Minimum spacing = half a card (--~0.5 world unit scaled, but clamp to 0.4 for safety).
    /// </summary>
    float CalcCompressedSpacing(int count)
    {
        if (count <= 1) return maxSpacing;
        float needed = (count - 1) * maxSpacing;
        if (needed <= visibleWidth) return maxSpacing;

        // Calculate spacing to fit, but never compress below 0.4
        float minSpacing = 0.4f;
        float fitSpacing = visibleWidth / (count - 1);
        return Mathf.Max(fitSpacing, minSpacing);
    }
}
