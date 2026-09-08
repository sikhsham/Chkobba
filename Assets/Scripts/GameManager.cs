using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
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

    // Layout tuning
    private const float handArcHeight = 0.3f;
    private const float handTilt = 10f;
    private const float tableArcHeight = 0.4f;
    private const float tableRowGap = 1.6f;

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
    private bool playerIsDealer = false; // player is cutter in round 1, alternates
    private Card cuttingCard; // the card revealed during cutting phase
    private CardObject cuttingDisplayObj; // cached reference to cutting display card
    private const int targetScore = 11;

    // Menu / game-over overlays
    private CanvasGroup menuPanel;
    private CanvasGroup gameOverPanel;
    private Text gameOverText;

    // UI elements
    private Text instructionText;
    private Text scoreText;
    private Button captureButton;
    private Button throwButton;
    private Text captureButtonText;
    private Text throwButtonText;

    // Card fx
    private bool fxRunning;
    private static readonly Vector3 aiPilePos = new Vector3(4.4f, 2.9f, 0);
    private static readonly Vector3 playerPilePos = new Vector3(-4.4f, -2.9f, 0);
    private List<CardObject> playerPileCards = new List<CardObject>();
    private List<CardObject> aiPileCards = new List<CardObject>();
    private const int maxPileVisual = 5;

    // Round results panel
    private CanvasGroup roundPanel;
    private Text roundPanelText;

    // Layout edit mode (debug helper)
    private bool layoutEditMode;
    private CardObject dragCard;

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
        ShowMenu();
    }

    // ---- LAYOUT EDIT MODE (debug helper) ----
    // F2 freezes the game so table cards can be hand-placed with the mouse.
    // F3 prints the current arrangement. F2 again resumes normal play.

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        if (keyboard.f2Key.wasPressedThisFrame)
        {
            layoutEditMode = !layoutEditMode;
            Time.timeScale = layoutEditMode ? 0f : 1f;
            dragCard = null;
            Debug.Log("[LAYOUT] Edit mode " + (layoutEditMode
                ? "ON - drag table cards with the mouse, F3 prints the arrangement"
                : "OFF - game resumed"));
        }

        if (!layoutEditMode) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        mouseWorld.z = 0f;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            var hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
            if (hit.collider != null)
            {
                var card = hit.collider.GetComponent<CardObject>();
                if (card != null && tableCards.Contains(card)) dragCard = card;
            }
        }
        else if (mouse.leftButton.isPressed && dragCard != null)
        {
            dragCard.transform.position = mouseWorld;
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            dragCard = null;
        }

        if (keyboard.f3Key.wasPressedThisFrame)
        {
            Debug.Log("[LAYOUT] Table arrangement (" + tableCards.Count + " cards, offsets from table center):");
            foreach (var c in tableCards)
                Debug.Log("[LAYOUT]   " + c.Card.ValueName + " -> (" + c.transform.position.x.ToString("F2") + ", " + (c.transform.position.y - tableY).ToString("F2") + ")");
        }
    }

    // ---- UI CREATION ----

    void CreateUI()
    {
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null) return;

        // Landscape-friendly canvas scaling (the scene shipped with a portrait 1080x1920 reference)
        var scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Score text (anchored top-left). Destroy any old ScoreText from scene.
        var oldScore = canvasObj.transform.Find("ScoreText");
        if (oldScore != null) Destroy(oldScore.gameObject);

        var scoreObj = new GameObject("ScoreText_G");
        scoreObj.transform.SetParent(canvasObj.transform, false);
        scoreText = scoreObj.AddComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = 24;
        scoreText.alignment = TextAnchor.UpperLeft;
        scoreText.color = Color.white;
        var scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = new Vector2(10, -10);
        scoreRect.sizeDelta = new Vector2(420, 60);
        UpdateScoreDisplay();

        // Instruction text (anchored top-center)
        var textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(canvasObj.transform, false);
        instructionText = textObj.AddComponent<Text>();
        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionText.fontSize = 26;
        instructionText.alignment = TextAnchor.MiddleCenter;
        instructionText.color = Color.white;
        var textRect = instructionText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0, -40);
        textRect.sizeDelta = new Vector2(700, 50);
        instructionText.text = "Click a card to play";

        // CAPTURE button (bottom-right area, left of THROW)
        captureButton = CreateActionBtn(canvasObj, "CaptureBtn", "CAPTURE",
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-480, 70), new Vector2(260, 90),
            OnCaptureClicked);
        captureButtonText = captureButton.GetComponentInChildren<Text>();

        // THROW button (bottom-right corner)
        throwButton = CreateActionBtn(canvasObj, "ThrowBtn", "THROW",
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-170, 70), new Vector2(280, 90),
            OnThrowClicked);
        throwButtonText = throwButton.GetComponentInChildren<Text>();

        CreateMenuUI();
        CreateGameOverUI();
        CreateRoundUI();
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
        txt.fontSize = 28;
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
        if (layoutEditMode) return;
        if (phase == TurnPhase.Cutting && !playerIsDealer) { ApplyCut(true); return; }
        if (phase == TurnPhase.Selecting) { ConfirmCapture(); return; }
    }

    void OnThrowClicked()
    {
        if (layoutEditMode) return;
        if (phase == TurnPhase.Cutting && !playerIsDealer) { ApplyCut(false); return; }
        if (phase == TurnPhase.Selecting) { ThrowCard(); return; }
    }

    // ---- MENU & GAME OVER ----

    CanvasGroup CreateOverlayPanel(GameObject canvas, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(canvas.transform, false);
        var img = obj.AddComponent<Image>();
        img.color = new Color(0f, 0.25f, 0.12f, 0.97f);
        img.raycastTarget = true;
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var group = obj.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    Text MenuText(Transform parent, string name, string msg, int size, FontStyle style, Vector2 pos)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var t = obj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.text = msg;
        var r = obj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(900, size * 2);
        obj.AddComponent<Outline>();
        return t;
    }

    Button MenuButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var img = obj.AddComponent<Image>();
        img.color = color;
        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(obj.transform, false);
        var txt = labelObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 32;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;
        var lr = labelObj.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.sizeDelta = Vector2.zero;
        return btn;
    }

    void CreateMenuUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        menuPanel = CreateOverlayPanel(canvas, "MenuPanel");

        MenuText(menuPanel.transform, "MenuTitle", "CHKOBBA", 80, FontStyle.Bold, new Vector2(0, 150));
        MenuText(menuPanel.transform, "MenuSubtitle", "a TunToon game", 26, FontStyle.Italic, new Vector2(0, 60));
        MenuButton(menuPanel.transform, "PlayBtn", "PLAY", new Vector2(0, -80), new Vector2(340, 90), btnGreen, OnMenuPlay);
        MenuButton(menuPanel.transform, "QuitBtn", "QUIT", new Vector2(0, -200), new Vector2(340, 70), btnGray, OnMenuQuit);
    }

    void CreateGameOverUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        gameOverPanel = CreateOverlayPanel(canvas, "GameOverPanel");

        gameOverText = MenuText(gameOverPanel.transform, "GameOverTitle", "", 56, FontStyle.Bold, new Vector2(0, 120));
        MenuButton(gameOverPanel.transform, "PlayAgainBtn", "PLAY AGAIN", new Vector2(0, -80), new Vector2(340, 90), btnGreen, OnGameOverPlayAgain);
        MenuButton(gameOverPanel.transform, "MenuBtn", "MAIN MENU", new Vector2(0, -200), new Vector2(340, 70), btnOrange, OnGameOverMenu);
    }

    void SetPanelVisible(CanvasGroup panel, bool on)
    {
        panel.alpha = on ? 1f : 0f;
        panel.interactable = on;
        panel.blocksRaycasts = on;
    }

    void ShowMenu()
    {
        SetPanelVisible(menuPanel, true);
        SetPanelVisible(gameOverPanel, false);
        phase = TurnPhase.Idle;
        SetInstruction("");
    }

    void ResetMatch()
    {
        playerScore = 0;
        aiScore = 0;
        roundNumber = 1;
        playerIsDealer = false;
    }

    void OnMenuPlay()
    {
        SetPanelVisible(menuPanel, false);
        ResetMatch();
        StartRound();
    }

    void OnMenuQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ShowGameOver(string result)
    {
        gameOverText.text = result + "\n" + playerScore + " - " + aiScore;
        SetPanelVisible(gameOverPanel, true);
    }

    void OnGameOverPlayAgain()
    {
        SetPanelVisible(gameOverPanel, false);
        ResetMatch();
        StartRound();
    }

    void OnGameOverMenu()
    {
        SetPanelVisible(gameOverPanel, false);
        ShowMenu();
    }

    void CreateRoundUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        roundPanel = CreateOverlayPanel(canvas, "RoundPanel");

        roundPanelText = MenuText(roundPanel.transform, "RoundPanelText", "", 34, FontStyle.Bold, new Vector2(0, 40));
        roundPanelText.rectTransform.sizeDelta = new Vector2(950, 480);
        MenuButton(roundPanel.transform, "RoundContinueBtn", "CONTINUE", new Vector2(0, -250), new Vector2(340, 90), btnGreen, OnRoundContinue);
    }

    void ShowRoundPanel(int pPts, int aPts, List<string> bits)
    {
        var youBits = new List<string>();
        var aiBits = new List<string>();
        foreach (var b in bits)
        {
            if (b.EndsWith("(You)")) youBits.Add(b.Substring(0, b.Length - 6));
            else if (b.EndsWith("(AI)")) aiBits.Add(b.Substring(0, b.Length - 5));
            else if (b.StartsWith("chkobba"))
            {
                if (playerChkobbas > 0) youBits.Add("chkobba +" + playerChkobbas);
                if (aiChkobbas > 0) aiBits.Add("chkobba +" + aiChkobbas);
            }
        }

        roundPanelText.text = "ROUND " + roundNumber + "\n"
            + "You " + pPts + " - " + aPts + " AI\n\n"
            + "You: " + (youBits.Count > 0 ? string.Join(", ", youBits) : "no points") + "\n"
            + "AI: " + (aiBits.Count > 0 ? string.Join(", ", aiBits) : "no points") + "\n\n"
            + "Game score: You " + playerScore + " - " + aiScore + " AI";

        SetPanelVisible(roundPanel, true);
        SetInstruction("");
    }

    void OnRoundContinue()
    {
        SetPanelVisible(roundPanel, false);
        NewRound();
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

        foreach (var c in playerPileCards) Destroy(c.gameObject);
        foreach (var c in aiPileCards) Destroy(c.gameObject);
        playerPileCards.Clear();
        aiPileCards.Clear();

        deck = new Deck();
        deck.Shuffle();

        // Every round starts with the cut -- the cutter alternates between rounds
        StartCuttingPhase();

        UpdateScoreDisplay();
        UpdateButtons();
        RenderAll();
    }

    // ---- CUTTING PHASE (every round, cutter alternates) ----

    void StartCuttingPhase()
    {
        // Reveal one card as cutting card
        cuttingCard = deck.Draw();
        // Place it face-up in the center so player can see it
        cuttingDisplayObj = CreateCardObject(cuttingCard, true);
        cuttingDisplayObj.TweenTo(new Vector3(0, 0.5f, 0), 0.3f);
        cuttingCard = cuttingDisplayObj.Card; // keep the data ref
        phase = TurnPhase.Cutting;

        if (playerIsDealer)
        {
            // AI is the cutter - it decides after a short pause
            Debug.Log("[CUT] Round " + roundNumber + " | AI cuts " + cuttingCard.ValueName);
            SetInstruction("AI cuts: " + cuttingCard.ValueName + " ...");
            UpdateButtons();
            Invoke(nameof(ApplyAiCut), 1.5f);
        }
        else
        {
            Debug.Log("[CUT] Round " + roundNumber + " | You cut " + cuttingCard.ValueName + " - KEEP or REJECT?");
            SetInstruction("Cutting card: " + cuttingCard + "    KEEP or REJECT?");
            UpdateCuttingButtons();
        }
    }

    void ApplyAiCut()
    {
        // Simple heuristic: keep the cut card when it is worth capturing with (value 5+)
        bool keep = cuttingCard.Value >= 5;
        Debug.Log("[CUT] AI " + (keep ? "KEEPS" : "REJECTS") + " " + cuttingCard.ValueName);
        SetInstruction("AI " + (keep ? "kept" : "rejected") + " " + cuttingCard.ValueName);
        ApplyCut(keep);
    }

    void ApplyCut(bool cutterKeeps)
    {
        bool playerCuts = !playerIsDealer;
        var cutterHand = playerCuts ? playerHand : aiHand;
        var dealerHand = playerCuts ? aiHand : playerHand;
        bool cutterFaceUp = playerCuts; // the AI's hand cards stay face-down

        var displayObj = FindCuttingDisplay();
        if (displayObj != null) Destroy(displayObj);

        if (cutterKeeps)
        {
            Debug.Log("[CUT] " + (playerCuts ? "You" : "AI") + " KEPT " + cuttingCard.ValueName);
            // Cutter keeps the cutting card + 2 more = 3 total
            cutterHand.Add(CreateCardObject(cuttingCard, cutterFaceUp));
            cutterHand.Add(CreateCardObject(deck.Draw(), cutterFaceUp));
            cutterHand.Add(CreateCardObject(deck.Draw(), cutterFaceUp));

            // 4 cards face-up on table
            for (int i = 0; i < 4; i++)
                tableCards.Add(CreateCardObject(deck.Draw(), true));
        }
        else
        {
            Debug.Log("[CUT] " + (playerCuts ? "You" : "AI") + " REJECTED " + cuttingCard.ValueName + " - it goes to the table");
            // Rejected card goes to table, +3 more = 4 total
            tableCards.Add(CreateCardObject(cuttingCard, true));
            for (int i = 0; i < 3; i++)
                tableCards.Add(CreateCardObject(deck.Draw(), true));

            // Cutter gets 3 fresh cards
            cutterHand.Add(CreateCardObject(deck.Draw(), cutterFaceUp));
            cutterHand.Add(CreateCardObject(deck.Draw(), cutterFaceUp));
            cutterHand.Add(CreateCardObject(deck.Draw(), cutterFaceUp));
        }

        // Dealer takes 3 cards
        dealerHand.Add(CreateCardObject(deck.Draw(), !cutterFaceUp));
        dealerHand.Add(CreateCardObject(deck.Draw(), !cutterFaceUp));
        dealerHand.Add(CreateCardObject(deck.Draw(), !cutterFaceUp));

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
            Debug.Log("[DEAL] Reshuffle - 3+ cards of same rank on table");
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

        // Valid layout -- start the game (the cutter plays first)
        if (!playerIsDealer)
        {
            phase = TurnPhase.Idle;
            Debug.Log("[DEAL] Round " + roundNumber + " resolved | hands " + playerHand.Count + "/" + aiHand.Count + " | table " + tableCards.Count + " | deck " + deck.Count + " | you play first");
            SetInstruction("Click a card to play");
        }
        else
        {
            phase = TurnPhase.AiTurn;
            Debug.Log("[DEAL] Round " + roundNumber + " resolved | hands " + playerHand.Count + "/" + aiHand.Count + " | table " + tableCards.Count + " | deck " + deck.Count + " | AI plays first");
            SetInstruction("AI turn...");
            Invoke(nameof(AiTurn), 1f);
        }
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

    CardObject CreateCardObject(Card card, bool faceUp)
    {
        var obj = new GameObject();
        var co = obj.AddComponent<CardObject>();
        co.Setup(card, faceUp);
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = faceUp ? CardSpriteLoader.GetSpriteForCard(card) : CardSpriteLoader.GetCardBackSprite();
        obj.transform.localScale = new Vector3(cardScale, cardScale, 1);
        obj.AddComponent<BoxCollider2D>();
        obj.transform.position = new Vector3(8f, 0f, 0); // spawn at the deck corner, tweens into place
        co.InitColor();
        return co;
    }

    // ---- CLICK ROUTING ----

    public void OnCardClicked(CardObject card)
    {
        if (fxRunning || layoutEditMode) return;
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
        Debug.Log("[PLAYER] Played " + card.Card.ValueName);

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
                Debug.Log("[PLAYER] Combo rejected - single match exists, must capture single card");
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
        Debug.Log("[PLAYER] Threw " + playedCard.Card.ValueName + " (no capture)");
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
        }

        playerCaptured.Add(playedCard.Card);
        tableCards.Remove(playedCard);
        lastCapturer = "player";

        if (tableCards.Count == 0)
        {
            bool blocked = deck.Count == 0 && aiHand.Count == 0 && playerIsDealer;
            playedCard.Card.IsChkobba = true;
            if (blocked)
            {
                Debug.Log("[PLAYER] Chkobba BLOCKED - dealer last-card rule (marked, no bonus point)");
            }
            else
            {
                playerChkobbas++;
                Debug.Log("[PLAYER] CHKOBBA! Table swept");
            }
            SetInstruction("CHKOBBA! AI turn...");
        }
        else
        {
            SetInstruction("Captured " + (captures.Count + 1) + " cards. AI turn...");
        }

        string capturedNames = "";
        foreach (var c in captures) capturedNames += c.Card.ValueName + " ";
        Debug.Log("[PLAYER] Captured " + playedCard.Card.ValueName + " + [" + capturedNames.TrimEnd(' ', ',') + "] | pile " + playerCaptured.Count + " vs " + aiCaptured.Count);

        CardObject playedObj = playedCard;

        CleanupSelection();
        UpdateButtons();

        fxRunning = true;
        StartCoroutine(PlayerCaptureSequence(playedObj, captures));
    }

    IEnumerator PlayerCaptureSequence(CardObject playedObj, List<CardObject> captures)
    {
        yield return CaptureFx(playedObj, captures, false);
        RenderAll();
        Invoke(nameof(AiTurn), 0.4f);
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
            }
            aiCaptured.Add(card.Card);
            lastCapturer = "ai";

            if (tableCards.Count == 0)
            {
                bool blocked = deck.Count == 0 && playerHand.Count == 0 && playerIsDealer;
                card.Card.IsChkobba = true;
                if (blocked)
                {
                    Debug.Log("[AI] Chkobba BLOCKED - dealer last-card rule (marked, no bonus point)");
                }
                else
                {
                    aiChkobbas++;
                    Debug.Log("[AI] CHKOBBA! Table swept");
                }
                SetInstruction("AI CHKOBBA! Your turn...");
            }
            else
            {
                SetInstruction("AI captured " + (capture.Count + 1) + " cards. Your turn...");
            }

            string aiCapturedNames = "";
            foreach (var c in capture) aiCapturedNames += c.Card.ValueName + " ";
            Debug.Log("[AI] Captured " + card.Card.ValueName + " + [" + aiCapturedNames.TrimEnd(' ', ',') + "] | pile " + playerCaptured.Count + " vs " + aiCaptured.Count);

            // Visual: land + pulse + fly to pile, THEN the table re-flows (sequenced, not simultaneous)
            fxRunning = true;
            StartCoroutine(AiCaptureSequence(card, new List<CardObject>(capture)));
        }
        else
        {
            tableCards.Add(card);
            SetInstruction("AI played " + card.Card + ". Your turn...");
            Debug.Log("[AI] Threw " + card.Card.ValueName + " (no capture) | table " + tableCards.Count);
            TailOfTurn();
        }
    }

    IEnumerator AiCaptureSequence(CardObject card, List<CardObject> captured)
    {
        yield return CaptureFx(card, captured, true);
        TailOfTurn();
    }

    void TailOfTurn()
    {
        if (playerHand.Count == 0 && aiHand.Count == 0)
        {
            if (deck.Count == 0)
            {
                EndRound();
                return;
            }
            TryReDeal();
        }

        if (phase == TurnPhase.AiTurn)
        {
            phase = TurnPhase.Idle;
        }
        UpdateButtons();
        RenderAll();
    }

    // ---- CARD FX ----

    IEnumerator CaptureFx(CardObject played, List<CardObject> captured, bool aiSide)
    {
        Vector3 pilePos = aiSide ? aiPilePos : playerPilePos;
        var targetPile = aiSide ? aiPileCards : playerPileCards;

        // 1. The played card lands on the group it captured
        if (captured.Count > 0)
        {
            played.TweenTo(captured[0].transform.position, 0.3f);
            yield return new WaitForSeconds(0.35f);
        }

        // 2. Highlight everything being taken
        var group = new List<CardObject>(captured);
        group.Add(played);
        foreach (var c in group) c.SetSelected(true);
        yield return PulseRoutine(group, 0.35f);

        // 3. Fly to the captured pile
        foreach (var c in group) { c.SetSelected(false); c.TweenTo(pilePos, 0.45f); }
        yield return new WaitForSeconds(0.5f);

        // 4. Flip face-down and join the visible captured stack
        foreach (var c in group)
        {
            if (c == null) continue;
            c.ShowBack();
            c.transform.position = PileSlot(targetPile.Count, pilePos, aiSide ? -1f : 1f);
            targetPile.Add(c);
        }
        while (targetPile.Count > maxPileVisual)
        {
            Destroy(targetPile[0].gameObject);
            targetPile.RemoveAt(0);
        }

        Debug.Log("[FX] " + (aiSide ? "AI" : "player") + " pile stacked at " + PileSlot(targetPile.Count, pilePos, aiSide ? -1f : 1f) + " | pile size " + targetPile.Count);

        fxRunning = false;

        if (aiSide && phase == TurnPhase.AiTurn)
        {
            phase = TurnPhase.Idle;
            UpdateButtons();
        }
    }

    Vector3 PileSlot(int index, Vector3 basePos, float xDir)
    {
        // Stacks grow toward the screen center so the newest card stays visible
        return basePos + new Vector3(xDir * index * 0.05f, index * 0.04f, 0);
    }

    IEnumerator PulseRoutine(List<CardObject> cards, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float s = cardScale * (1f + 0.15f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)));
            foreach (var c in cards)
                if (c != null) c.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        foreach (var c in cards)
            if (c != null) c.transform.localScale = new Vector3(cardScale, cardScale, 1f);
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

    // ---- RE-DEAL (hands empty, deck still has cards) ----

    void TryReDeal()
    {
        if (deck.Count == 0) return;

        int toDeal = Mathf.Min(3, deck.Count / 2);
        for (int i = 0; i < toDeal; i++)
        {
            playerHand.Add(CreateCardObject(deck.Draw(), true));
            aiHand.Add(CreateCardObject(deck.Draw(), false));
        }

        Debug.Log("[DEAL] Re-dealt " + toDeal + " cards each | deck remaining " + deck.Count);
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
            Debug.Log("[SCORE] " + (lastCapturer == "player" ? "You" : "AI") + " swept remaining " + tableCards.Count + " table cards (last capturer, not a chkobba)");
        }
        foreach (var tc in tableCards) Destroy(tc.gameObject);
        tableCards.Clear();

        int pPts = 0, aPts = 0;
        var bits = new List<string>();

        // 1. Most cards captured
        if (playerCaptured.Count > aiCaptured.Count) { pPts++; bits.Add("cards " + playerCaptured.Count + "-" + aiCaptured.Count + " (You)"); }
        else if (aiCaptured.Count > playerCaptured.Count) { aPts++; bits.Add("cards " + playerCaptured.Count + "-" + aiCaptured.Count + " (AI)"); }

        // 2. Most diamonds
        int pD = CountSuit(playerCaptured, Card.Suit.Diamonds);
        int aD = CountSuit(aiCaptured, Card.Suit.Diamonds);
        if (pD > aD) { pPts++; bits.Add("diamonds " + pD + "-" + aD + " (You)"); }
        else if (aD > pD) { aPts++; bits.Add("diamonds " + pD + "-" + aD + " (AI)"); }

        // 3. Primiera - most sevens, tiebreak with sixes
        int p7 = CountRank(playerCaptured, Card.Rank.Seven);
        int a7 = CountRank(aiCaptured, Card.Rank.Seven);
        if (p7 > a7) { pPts++; bits.Add("sevens " + p7 + "-" + a7 + " (You)"); }
        else if (a7 > p7) { aPts++; bits.Add("sevens " + p7 + "-" + a7 + " (AI)"); }
        else
        {
            int p6 = CountRank(playerCaptured, Card.Rank.Six);
            int a6 = CountRank(aiCaptured, Card.Rank.Six);
            if (p6 > a6) { pPts++; bits.Add("sevens tie, sixes " + p6 + "-" + a6 + " (You)"); }
            else if (a6 > p6) { aPts++; bits.Add("sevens tie, sixes " + p6 + "-" + a6 + " (AI)"); }
        }

        // 4. Seven of Diamonds
        if (HasSevenOfDiamonds(playerCaptured)) { pPts++; bits.Add("7 of diamonds (You)"); }
        if (HasSevenOfDiamonds(aiCaptured)) { aPts++; bits.Add("7 of diamonds (AI)"); }

        // 5. Chkobba bonuses
        pPts += playerChkobbas;
        aPts += aiChkobbas;
        if (playerChkobbas > 0 || aiChkobbas > 0)
            bits.Add("chkobba +" + playerChkobbas + " You, +" + aiChkobbas + " AI");

        playerScore += pPts;
        aiScore += aPts;

        Debug.Log("[SCORE] Round " + roundNumber + " | You " + pPts + " - AI " + aPts + " | " + string.Join(", ", bits));
        Debug.Log("[SCORE] Game | You " + playerScore + " - AI " + aiScore + " | target " + targetScore);
        UpdateScoreDisplay();

        bool matchOver = false;
        if (playerScore >= targetScore || aiScore >= targetScore)
        {
            if (playerScore > aiScore)
            {
                Debug.Log("[SCORE] GAME OVER - You win " + playerScore + "-" + aiScore);
                ShowGameOver("YOU WIN!");
                matchOver = true;
            }
            else if (aiScore > playerScore)
            {
                Debug.Log("[SCORE] GAME OVER - AI wins " + aiScore + "-" + playerScore);
                ShowGameOver("AI WINS");
                matchOver = true;
            }
            else
            {
                Debug.Log("[SCORE] Tie at " + targetScore + " - playing an extra round");
            }
        }

        if (!matchOver)
        {
            ShowRoundPanel(pPts, aPts, bits);
        }
    }

    void NewRound()
    {
        roundNumber++;
        playerIsDealer = !playerIsDealer;
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
        RenderGroup(playerHand, playerY, true);
        RenderGroup(aiHand, aiY, false);
        RenderTable();
    }

    void RenderTable()
    {
        int n = tableCards.Count;
        if (n == 0) return;

        // Rows of up to 4 cards, stacked and centered; extras go to the top rows.
        // Every row uses the raised-middle arc: 3-card rows raise the middle,
        // 4-card rows raise the middle two, 2-card rows sit flat.
        int rowCount = Mathf.Max(1, Mathf.CeilToInt(n / 4f));
        int baseSize = n / rowCount;
        int extras = n % rowCount;
        var rowSizes = new int[rowCount];
        for (int r = 0; r < rowCount; r++)
            rowSizes[r] = baseSize + (r < extras ? 1 : 0);

        int index = 0;
        for (int r = 0; r < rowCount; r++)
        {
            int k = rowSizes[r];
            float rowY = tableY + tableRowGap * ((rowCount - 1) / 2f - r);
            float startX = -((k - 1) * maxSpacing) / 2f;
            float rowCenter = (k - 1) / 2f;
            float maxD = Mathf.Max(rowCenter, 1f);
            for (int i = 0; i < k; i++)
            {
                float dn = (i - rowCenter) / maxD;
                float y = rowY + tableArcHeight * (1f - dn * dn);
                Vector3 target = new Vector3(startX + i * maxSpacing, y, 0);
                if (Vector3.Distance(tableCards[index].transform.position, target) > 0.01f)
                    tableCards[index].TweenTo(target, 0.25f);
                tableCards[index].GetComponent<SpriteRenderer>().sortingOrder = index;
                index++;
            }
        }
    }

    void RenderGroup(List<CardObject> cards, float yPos, bool lower)
    {
        int n = cards.Count;
        if (n == 0) return;

        // Measure the real card size (sprite-local bounds x scale = world size, rotation-proof)
        var sr0 = cards[0].GetComponent<SpriteRenderer>();
        float cardW = sr0.sprite.bounds.size.x * cards[0].transform.localScale.x;
        float cardH = sr0.sprite.bounds.size.y * cards[0].transform.localScale.y;

        // Outer cards pulled 1/2 card width closer to the middle one, then loosened 10% back out
        float spacing = Mathf.Max(cardW * 0.52f, 0.3f);
        float yShift = lower ? -cardH / 3f : cardH / 3f;
        float baseY = yPos + yShift;
        float startX = -((n - 1) * spacing) / 2f;
        float center = (n - 1) / 2f;
        float maxD = Mathf.Max(center, 1f);
        for (int i = 0; i < n; i++)
        {
            float dn = (i - center) / maxD;
            float y = baseY + handArcHeight * (0.5f - 0.5f * dn * dn);
            float rot = (n >= 2) ? -(dn * handTilt) : 0f;
            if (n == 1) { y = baseY; rot = 0f; }
            Vector3 target = new Vector3(startX + i * spacing, y, 0);
            cards[i].TweenTo(target, 0.25f, rot);
            // Middle card renders on top; outer cards tuck under it symmetrically
            cards[i].GetComponent<SpriteRenderer>().sortingOrder = n - 1 - (int)Mathf.Round(Mathf.Abs(i - center));
        }
    }
}