using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Manages the Chkobba game state, dealing, card interaction, and manual capture.
///
/// Turn flow:
/// 1. Player clicks hand card -> card moves to table, highlighted yellow
/// 2. Player clicks table cards to select/deselect (yellow tint)
/// 3. CAPTURE button (green) or THROW button (orange) to confirm action
/// 4. CAPTURE: valid combo removed. THROW: card stays on table (no capture).
/// 5. AI turn (automatic)
///
/// Player can click the played card again to deselect it and pick a different card.
///
/// Buttons are always visible, anchored to bottom-right:
/// - CAPTURE: green when selection sums to played card value, gray otherwise
/// - THROW: orange during selecting phase, gray otherwise
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

    private const float visibleWidth = 9f;

    // State
    private enum TurnPhase { Cutting, Idle, Selecting, AiTurn, RoundEnd }
    private TurnPhase phase = TurnPhase.Idle;

    private Deck deck;
    private List<CardObject> playerHand = new List<CardObject>();
    private List<CardObject> aiHand = new List<CardObject>();
    private List<CardObject> tableCards = new List<CardObject>();
    private List<CardObject> selectedTable = new List<CardObject>();

    private CardObject playedCard;

    private List<Card> playerCaptured = new List<Card>();
    private List<Card> aiCaptured = new List<Card>();
    private int playerChkobbas;
    private int aiChkobbas;

    // Scoring
    private int playerScore;
    private int aiScore;
    private string lastCapturer = ""; // "player" or "ai"
    private int roundNumber = 1;
    private Card cuttingCard; // the card revealed during cutting phase
    private CardObject cuttingDisplayObj; // cached reference to cutting display card
    private const int targetScore = 11;

    // UI elements
    private Text instructionText;
    private Text scoreText;
    private Button captureButton;
    private Button throwButton;
    private Text captureButtonText;
    private Text throwButtonText;

    private static readonly Color btnGreen  = new Color(0.2f, 0.8f, 0.2f);
    private static readonly Color btnOrange = new Color(1f, 0.6f, 0.1f);
    private static readonly Color btnGray   = new Color(0.4f, 0.4f, 0.4f);

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateUI();
        UpdateButtons();
        StartRound();
    }

    // ---- UI CREATION ----

    void CreateUI()
    {
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null) return;

        // Score text (anchored top-left). Destroy any old ScoreText from scene.
        var oldScore = canvasObj.transform.Find("ScoreText");
        if (oldScore != null) Destroy(oldScore.gameObject);

        var scoreObj = new GameObject("ScoreText_G");
        scoreObj.transform.SetParent(canvasObj.transform, false);
        scoreText = scoreObj.AddComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = 20;
        scoreText.alignment = TextAnchor.UpperLeft;
        scoreText.color = Color.white;
        var scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = new Vector2(10, -10);
        scoreRect.sizeDelta = new Vector2(300, 60);
        UpdateScoreDisplay();

        // Instruction text (anchored top-center)
        var textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(canvasObj.transform, false);
        instructionText = textObj.AddComponent<Text>();
        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionText.fontSize = 22;
        instructionText.alignment = TextAnchor.MiddleCenter;
        instructionText.color = Color.white;
        var textRect = instructionText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0, -40);
        textRect.sizeDelta = new Vector2(500, 40);
        instructionText.text = "Click a card to play";

        // CAPTURE button (bottom-right area, left of THROW)
        captureButton = CreateActionBtn(canvasObj, "CaptureBtn", "CAPTURE",
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-290, 30), new Vector2(140, 50),
            OnCaptureClicked);
        captureButtonText = captureButton.GetComponentInChildren<Text>();

        // THROW button (bottom-right corner)
        throwButton = CreateActionBtn(canvasObj, "ThrowBtn", "THROW",
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-120, 30), new Vector2(160, 50),
            OnThrowClicked);
        throwButtonText = throwButton.GetComponentInChildren<Text>();
    }

    Button CreateActionBtn(GameObject canvas, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pos, Vector2 size,
        UnityEngine.Events.UnityAction onClick)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(canvas.transform, false);

        var img = btnObj.AddComponent<Image>();
        img.color = btnGray;
        img.raycastTarget = true;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        var txt = labelObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;
        var labelRect = txt.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        return btn;
    }

    void SetInstruction(string text)
    {
        if (instructionText != null)
            instructionText.text = text;
    }

    void UpdateButtons()
    {
        if (captureButton == null || throwButton == null) return;
        if (phase == TurnPhase.Cutting) return; // cutting has its own button setup

        if (phase == TurnPhase.Selecting && playedCard != null)
        {
            throwButton.GetComponent<Image>().color = btnOrange;
            throwButton.interactable = true;

            int sum = 0;
            foreach (var c in selectedTable) sum += c.Card.Value;
            bool canCapture = sum == playedCard.Card.Value && selectedTable.Count > 0;

            captureButton.GetComponent<Image>().color = canCapture ? btnGreen : btnGray;
            captureButton.interactable = canCapture;

            captureButtonText.text = sum > 0
                ? "CAPTURE " + sum + "/" + playedCard.Card.Value
                : "CAPTURE";
        }
        else
        {
            captureButton.GetComponent<Image>().color = btnGray;
            captureButton.interactable = false;
            captureButtonText.text = "CAPTURE";

            throwButton.GetComponent<Image>().color = btnGray;
            throwButton.interactable = false;
        }
    }

    // ---- BUTTON HANDLERS ----

    void OnCaptureClicked()
    {
        if (phase == TurnPhase.Cutting) { ApplyKeepChoice(); return; }
        if (phase == TurnPhase.Selecting) { ConfirmCapture(); return; }
    }

    void OnThrowClicked()
    {
        if (phase == TurnPhase.Cutting) { ApplyRejectChoice(); return; }
        if (phase == TurnPhase.Selecting) { ThrowCard(); return; }
    }

    // ---- SETUP ----

    void StartRound()
    {
        // Clear previous round objects
        foreach (var c in playerHand) Destroy(c.gameObject);
        foreach (var c in aiHand) Destroy(c.gameObject);
        foreach (var c in tableCards) Destroy(c.gameObject);
        playerHand.Clear();
        aiHand.Clear();
        tableCards.Clear();
        selectedTable.Clear();
        playedCard = null;
        playerCaptured.Clear();
        aiCaptured.Clear();
        playerChkobbas = 0;
        aiChkobbas = 0;
        lastCapturer = "";
        cuttingCard = null;
        cuttingDisplayObj = null;

        deck = new Deck();
        deck.Shuffle();

        if (roundNumber == 1)
        {
            // Round 1: cutting phase
            StartCuttingPhase();
        }
        else
        {
            // Rounds 2+: deal hands only, no new table cards
            DealHands();
            phase = TurnPhase.Idle;
            Debug.Log("Round " + roundNumber + " dealt: " + playerHand.Count + " player, " + aiHand.Count + " AI, table=" + tableCards.Count + ", deck=" + deck.Count);
            SetInstruction("Click a card to play");
        }

        UpdateScoreDisplay();
        UpdateButtons();
        RenderAll();
    }

    // ---- CUTTING PHASE (Round 1) ----

    void StartCuttingPhase()
    {
        // Reveal one card as cutting card
        cuttingCard = deck.Draw();
        // Place it face-up in the center so player can see it
        cuttingDisplayObj = CreateCardObject(cuttingCard, true);
        cuttingDisplayObj.transform.position = new Vector3(0, 0.5f, 0);
        cuttingCard = cuttingDisplayObj.Card; // keep the data ref
        phase = TurnPhase.Cutting;

        SetInstruction("Cutting card: " + cuttingCard + "    KEEP or REJECT?");
        UpdateCuttingButtons();
    }

    void ApplyKeepChoice()
    {
        // Destroy the cutting display card
        var displayObj = FindCuttingDisplay();
        if (displayObj != null) Destroy(displayObj);

        // Player keeps the cutting card + 2 more = 3 total
        playerHand.Add(CreateCardObject(cuttingCard, true));
        playerHand.Add(CreateCardObject(deck.Draw(), true));
        playerHand.Add(CreateCardObject(deck.Draw(), true));

        // 4 cards face-up on table
        for (int i = 0; i < 4; i++)
            tableCards.Add(CreateCardObject(deck.Draw(), true));

        // AI gets 3 cards
        for (int i = 0; i < 3; i++)
            aiHand.Add(CreateCardObject(deck.Draw(), false));

        TryReshuffleAndTransition();
    }

    void ApplyRejectChoice()
    {
        var displayObj = FindCuttingDisplay();
        if (displayObj != null)
        {
            // Keep the display card but fold it into table cards properly
            Destroy(displayObj);
        }

        // Rejected card goes to table
        tableCards.Add(CreateCardObject(cuttingCard, true));

        // +3 more to table = 4 total
        for (int i = 0; i < 3; i++)
            tableCards.Add(CreateCardObject(deck.Draw(), true));

        // Player gets 3 cards
        for (int i = 0; i < 3; i++)
            playerHand.Add(CreateCardObject(deck.Draw(), true));

        // AI gets 3 cards
        for (int i = 0; i < 3; i++)
            aiHand.Add(CreateCardObject(deck.Draw(), false));

        TryReshuffleAndTransition();
    }

    GameObject FindCuttingDisplay()
    {
        if (cuttingDisplayObj != null) return cuttingDisplayObj.gameObject;
        return null;
    }

    void TryReshuffleAndTransition()
    {
        // Reshuffle rule: if 3+ table cards share the same rank, reshuffle and re-deal
        if (HasThreeSameRank())
        {
            Debug.Log("Reshuffle: 3+ cards of same rank on table");
            // Clean up everything and redo cutting
            foreach (var c in playerHand) Destroy(c.gameObject);
            foreach (var c in aiHand) Destroy(c.gameObject);
            foreach (var c in tableCards) Destroy(c.gameObject);
            playerHand.Clear();
            aiHand.Clear();
            tableCards.Clear();

            deck = new Deck();
            deck.Shuffle();
            StartCuttingPhase();
            RenderAll();
            return;
        }

        // Valid layout -- start the game
        phase = TurnPhase.Idle;
        Debug.Log("Dealt (cut phase): " + playerHand.Count + " player, " + aiHand.Count + " AI, " + tableCards.Count + " table, " + deck.Count + " left");
        SetInstruction("Click a card to play");
        UpdateButtons();
        RenderAll();
    }

    bool HasThreeSameRank()
    {
        var rankCounts = new Dictionary<Card.Rank, int>();
        foreach (var tc in tableCards)
        {
            int count;
            rankCounts.TryGetValue(tc.Card.CardRank, out count);
            rankCounts[tc.Card.CardRank] = count + 1;
            if (count + 1 >= 3) return true;
        }
        return false;
    }

    void UpdateCuttingButtons()
    {
        if (captureButton == null || throwButton == null) return;

        // Repurpose buttons: CAPTURE -> KEEP, THROW -> REJECT
        captureButton.GetComponent<Image>().color = btnGreen;
        captureButton.interactable = true;
        captureButtonText.text = "KEEP";

        throwButton.GetComponent<Image>().color = btnOrange;
        throwButton.interactable = true;
        throwButtonText.text = "REJECT";
    }

    // ---- DEALING (Rounds 2+) ----

    void DealHands()
    {
        for (int i = 0; i < 3; i++)
            playerHand.Add(CreateCardObject(deck.Draw(), true));
        for (int i = 0; i < 3; i++)
            aiHand.Add(CreateCardObject(deck.Draw(), false));
        // No new table cards in rounds 2+
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
        co.InitColor();
        return co;
    }

    // ---- CLICK ROUTING ----

    public void OnCardClicked(CardObject card)
    {
        if (phase == TurnPhase.Selecting)
        {
            // Click the played card again -> deselect, go back to idle, return card to hand
            if (card == playedCard)
            {
                CancelPlayedCard();
                return;
            }

            // Click table card -> toggle selection
            if (tableCards.Contains(card))
            {
                card.SetSelected(!card.Selected);
                if (card.Selected)
                    selectedTable.Add(card);
                else
                    selectedTable.Remove(card);

                int sum = 0;
                foreach (var c in selectedTable) sum += c.Card.Value;
                SetInstruction("Selected: " + sum + " / need: " + playedCard.Card.Value);
                UpdateButtons();
            }
            return;
        }

        if (phase == TurnPhase.Idle && playerHand.Contains(card))
        {
            PlayPlayerCard(card);
        }
    }

    // ---- PLAYER TURN ----

    void PlayPlayerCard(CardObject card)
    {
        playerHand.Remove(card);
        tableCards.Add(card);
        playedCard = card;
        card.SetSelected(true);
        selectedTable.Clear();
        phase = TurnPhase.Selecting;

        foreach (var c in playerHand) c.SetDim(true);
        foreach (var c in aiHand) c.SetDim(true);

        SetInstruction("Select cards to capture, or THROW. Click played card to cancel.");
        UpdateButtons();
        RenderAll();
    }

    void CancelPlayedCard()
    {
        tableCards.Remove(playedCard);
        playedCard.SetSelected(false);
        playedCard.SetDim(false);
        playerHand.Add(playedCard);
        playedCard = null;

        foreach (var c in selectedTable) c.SetSelected(false);
        selectedTable.Clear();

        foreach (var c in playerHand) c.SetDim(false);
        foreach (var c in aiHand) c.SetDim(false);

        phase = TurnPhase.Idle;
        SetInstruction("Click a card to play");
        UpdateButtons();
        RenderAll();
    }

    void ConfirmCapture()
    {
        int sum = 0;
        foreach (var c in selectedTable) sum += c.Card.Value;

        if (sum != playedCard.Card.Value || selectedTable.Count == 0)
        {
            SetInstruction("Invalid selection. Select matching cards or THROW.");
            return;
        }

        // Enforce mandatory single-card capture rule
        if (selectedTable.Count > 1)
        {
            bool singleExists = false;
            foreach (var tc in tableCards)
            {
                if (tc != playedCard && tc.Card.Value == playedCard.Card.Value)
                {
                    singleExists = true;
                    break;
                }
            }
            if (singleExists)
            {
                Debug.Log("Cannot combo: single match exists (mandatory single-card capture)");
                SetInstruction("Single match exists! Select just one card.");
                foreach (var c in selectedTable) c.SetSelected(false);
                selectedTable.Clear();
                UpdateButtons();
                return;
            }
        }

        ExecutePlayerCapture();
    }

    void ThrowCard()
    {
        phase = TurnPhase.AiTurn;
        CleanupSelection();
        SetInstruction("Thrown! AI turn...");
        UpdateButtons();
        RenderAll();
        Invoke(nameof(AiTurn), 1f);
    }

    void ExecutePlayerCapture()
    {
        phase = TurnPhase.AiTurn;
        var captures = new List<CardObject>(selectedTable);

        foreach (var tc in captures)
        {
            playerCaptured.Add(tc.Card);
            tableCards.Remove(tc);
            Destroy(tc.gameObject);
        }

        playerCaptured.Add(playedCard.Card);
        tableCards.Remove(playedCard);
        Destroy(playedCard.gameObject);
        lastCapturer = "player";

        if (tableCards.Count == 0)
        {
            playerChkobbas++;
            Debug.Log("Player CHKOBBA!");
            SetInstruction("CHKOBBA! AI turn...");
        }
        else
        {
            SetInstruction("Captured " + (captures.Count + 1) + " cards. AI turn...");
        }

        CleanupSelection();
        Debug.Log("Player captured. Pile: player=" + playerCaptured.Count + " AI=" + aiCaptured.Count);
        UpdateButtons();
        RenderAll();
        Invoke(nameof(AiTurn), 1f);
    }

    void CleanupSelection()
    {
        foreach (var c in selectedTable) c.SetSelected(false);
        if (playedCard != null) playedCard.SetSelected(false);
        selectedTable.Clear();
        playedCard = null;
        foreach (var c in playerHand) c.SetDim(false);
        foreach (var c in aiHand) c.SetDim(false);
    }

    // ---- AI TURN ----

    void AiTurn()
    {
        if (aiHand.Count == 0)
        {
            if (deck.Count == 0 && playerHand.Count == 0)
            {
                EndRound();
                return;
            }
            TryReDeal();
            if (aiHand.Count == 0)
            {
                phase = TurnPhase.Idle;
                SetInstruction("Click a card to play");
                UpdateButtons();
                return;
            }
        }

        var card = FindBestAiPlay();
        aiHand.Remove(card);
        card.Reveal();

        var capture = FindAutoCapture(card.Card);
        if (capture != null)
        {
            foreach (var tc in capture)
            {
                aiCaptured.Add(tc.Card);
                tableCards.Remove(tc);
                Destroy(tc.gameObject);
            }
            aiCaptured.Add(card.Card);
            Destroy(card.gameObject);
            lastCapturer = "ai";

            if (tableCards.Count == 0)
            {
                aiChkobbas++;
                Debug.Log("AI CHKOBBA!");
                SetInstruction("AI CHKOBBA! Your turn...");
            }
            else
            {
                SetInstruction("AI captured " + (capture.Count + 1) + " cards. Your turn...");
            }

            Debug.Log("AI captured. Pile: player=" + playerCaptured.Count + " AI=" + aiCaptured.Count);
        }
        else
        {
            tableCards.Add(card);
            SetInstruction("AI played " + card.Card + ". Your turn...");
            Debug.Log("AI played " + card.Card + " -- no capture. Table: " + tableCards.Count);
        }

        if (playerHand.Count == 0 && aiHand.Count == 0)
        {
            if (deck.Count == 0)
            {
                EndRound();
                return;
            }
            TryReDeal();
        }

        phase = TurnPhase.Idle;
        UpdateButtons();
        RenderAll();
    }

    // ---- AI CAPTURE LOGIC ----

    List<CardObject> FindAutoCapture(Card played)
    {
        foreach (var tc in tableCards)
        {
            if (tc.Card.Value == played.Value)
                return new List<CardObject> { tc };
        }

        for (int size = 2; size <= tableCards.Count; size++)
        {
            var result = new List<CardObject>();
            if (TryCombo(tableCards, 0, played.Value, size, result))
                return result;
        }
        return null;
    }

    bool TryCombo(List<CardObject> pool, int startIdx, int remaining, int slotsLeft, List<CardObject> result)
    {
        if (slotsLeft == 0) return remaining == 0;
        if (remaining <= 0) return false;

        for (int i = startIdx; i < pool.Count; i++)
        {
            if (pool[i].Card.Value > remaining) continue;
            result.Add(pool[i]);
            if (TryCombo(pool, i + 1, remaining - pool[i].Card.Value, slotsLeft - 1, result))
                return true;
            result.RemoveAt(result.Count - 1);
        }
        return false;
    }

    CardObject FindBestAiPlay()
    {
        foreach (var c in aiHand)
        {
            if (FindAutoCapture(c.Card) != null)
                return c;
        }
        return aiHand[0];
    }

    // ---- RE-DEAL ----

    void TryReDeal()
    {
        if (deck.Count == 0) return;

        int toDeal = Mathf.Min(3, deck.Count);
        for (int i = 0; i < toDeal; i++)
        {
            playerHand.Add(CreateCardObject(deck.Draw(), true));
            aiHand.Add(CreateCardObject(deck.Draw(), false));
        }

        Debug.Log("Re-dealt " + toDeal + " cards each. Deck remaining: " + deck.Count);
        RenderAll();
    }

    // ---- SCORING ----

    void EndRound()
    {
        phase = TurnPhase.RoundEnd;

        // Last capturer gets remaining table cards (not a chkobba)
        if (tableCards.Count > 0 && lastCapturer != "")
        {
            var targetPile = lastCapturer == "player" ? playerCaptured : aiCaptured;
            foreach (var tc in tableCards) targetPile.Add(tc.Card);
        }
        foreach (var tc in tableCards) Destroy(tc.gameObject);
        tableCards.Clear();

        int pPts = 0, aPts = 0;

        // 1. Most cards captured
        if (playerCaptured.Count > aiCaptured.Count) pPts++;
        else if (aiCaptured.Count > playerCaptured.Count) aPts++;

        // 2. Most diamonds
        int pD = CountSuit(playerCaptured, Card.Suit.Diamonds);
        int aD = CountSuit(aiCaptured, Card.Suit.Diamonds);
        if (pD > aD) pPts++;
        else if (aD > pD) aPts++;

        // 3. Primiera ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â most sevens, tiebreak with sixes
        int p7 = CountRank(playerCaptured, Card.Rank.Seven);
        int a7 = CountRank(aiCaptured, Card.Rank.Seven);
        if (p7 > a7) pPts++;
        else if (a7 > p7) aPts++;
        else
        {
            int p6 = CountRank(playerCaptured, Card.Rank.Six);
            int a6 = CountRank(aiCaptured, Card.Rank.Six);
            if (p6 > a6) pPts++;
            else if (a6 > p6) aPts++;
        }

        // 4. Seven of Diamonds
        if (HasSevenOfDiamonds(playerCaptured)) pPts++;
        if (HasSevenOfDiamonds(aiCaptured)) aPts++;

        // 5. Chkobba bonuses
        pPts += playerChkobbas;
        aPts += aiChkobbas;

        playerScore += pPts;
        aiScore += aPts;

        Debug.Log("Round score: You " + pPts + " - AI " + aPts);
        Debug.Log("Game score: You " + playerScore + " - AI " + aiScore);
        UpdateScoreDisplay();

        if (playerScore >= targetScore || aiScore >= targetScore)
        {
            if (playerScore > aiScore)
                SetInstruction("YOU WIN!  Final: You " + playerScore + " - AI " + aiScore);
            else if (aiScore > playerScore)
                SetInstruction("AI WINS!  Final: You " + playerScore + " - AI " + aiScore);
            else
            {
                SetInstruction("Tie at " + targetScore + "! Extra round...");
                Invoke(nameof(NewRound), 2f);
            }
        }
        else
        {
            SetInstruction("Round: You " + pPts + " - AI " + aPts
                + "  |  New round in 3...");
            Invoke(nameof(NewRound), 3f);
        }
    }

    void NewRound()
    {
        roundNumber++;
        StartRound();
    }

    int CountSuit(List<Card> pile, Card.Suit suit)
    {
        int count = 0;
        foreach (var c in pile)
            if (c.CardSuit == suit) count++;
        return count;
    }

    int CountRank(List<Card> pile, Card.Rank rank)
    {
        int count = 0;
        foreach (var c in pile)
            if (c.CardRank == rank) count++;
        return count;
    }

    bool HasSevenOfDiamonds(List<Card> pile)
    {
        foreach (var c in pile)
            if (c.CardSuit == Card.Suit.Diamonds && c.CardRank == Card.Rank.Seven)
                return true;
        return false;
    }

    void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = "You " + playerScore + " - AI " + aiScore
                + "  |  Target: " + targetScore;
    }

    // ---- RENDERING ----

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
            cards[i].GetComponent<SpriteRenderer>().sortingOrder = i;
        }
    }

    float CalcCompressedSpacing(int count)
    {
        if (count <= 1) return maxSpacing;
        float needed = (count - 1) * maxSpacing;
        if (needed <= visibleWidth) return maxSpacing;
        float minSpacing = 0.4f;
        float fitSpacing = visibleWidth / (count - 1);
        return Mathf.Max(fitSpacing, minSpacing);
    }
}