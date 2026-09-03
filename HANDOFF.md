# Chkobba Project - AI Handoff Document

> **Purpose:** Give a new AI assistant full context to continue developing this Unity card game project.
> **Last updated:** 2026-07-02
> **Previous AI:** Jarvis (AutoClaw/OpenClaw), preceded by Super Z (Z AI/chat.z.ai)

---

## 1. Project Overview

**Game:** Chkobba (Tunisian card game, also known as Scopa in Italy)
**Type:** 1v1 Human vs AI, capture-based scoring, 40-card deck
**Engine:** Unity 6.5 (version 6000.5.0f1), URP 2D template
**Platform target:** Android (Play Store) first, iOS later
**Orientation:** Landscape (inspired by UNO mobile UI)
**Project path:** `C:\Users\ELITE\Documents\unity game\Chkobba`
**Rules doc:** `C:\Users\ELITE\Documents\unity game\Chkobba\RULES.md` (locked v1, read this for full game rules)

### Deck composition
- 4 suits: Clubs, Diamonds, Hearts, Spades
- 10 ranks per suit: Ace(1), 2-7(face value), Queen(8), Jack(9), King(10)
- Total: 40 cards
- Card art: 41 Kenney CC0 PNGs in `Assets/Resources/CardSprites/` (40 face cards + 1 card_back.png)

---

## 2. Developer Profile

- **Name:** Aymen
- **Location:** Tunisia, GMT+1
- **Language:** English only for all responses (no Chinese, no Arabic)
- **Experience:** Complete beginner to game development, learning at his own pace
- **Communication preference:** Small steps, one change per message, verify before moving on
- **Machine:** Windows, RTX 5070
- **No emojis** in responses
- **Vibe:** Direct, no fluff, no "Great question!" type filler

---

## 3. Coding Standards (Mandatory)

### Mobile-first input
All Unity input code must target Android/touch first. **Never use:**
- `OnMouseDown`, `OnMouseUp`, `OnMouseEnter`
- `Input.GetMouseButtonDown` or manual raycasting in `Update()`
- Any PC-mouse-oriented input approach

**Always use:**
- Unity EventSystem + `IPointerClickHandler` / `IPointerDownHandler` / `IPointerUpHandler`
- `Physics2DRaycaster` on camera for 2D sprite clicks
- `EventSystem` + `StandaloneInputModule` in scene
- These work identically for touch and mouse

### File writing from PowerShell
**Never use `Set-Content` or here-strings (`@"..."@`) to write C# files.** PowerShell escapes `"` as `""`, causing CS1003 syntax errors. Use `[System.IO.File]::WriteAllText()` instead, which writes verbatim.

### Other rules
- Prefer fewer lines, reuse over writing, stdlib first (YAGNI/ponytail philosophy)
- Git commit after each working component
- Label pseudocode explicitly
- Never invent APIs, SDK functions, package names, or GitHub repos

---

## 4. Accuracy Protocol

Aymen enforces a strict accuracy protocol. Full text in `ACCURACY_PROTOCOL.md`. Key points:
- Research before answering when current info could matter (software/AI/tools evolve fast)
- Freshness over memorization -- assume ecosystem changes weekly
- Verify before recommending -- check if repos are actively maintained
- Never invent APIs, SDK functions, package names, GitHub repos
- Be explicit about uncertainty -- "I couldn't verify this" is better than a confident wrong answer
- Version awareness -- always determine relevant version (Unity, SDK, etc.)
- Ask before assuming defaults

---

## 5. File Structure

### Scripts (in `Assets/Scripts/`)

| File | Lines | Purpose |
|------|-------|---------|
| `Card.cs` | ~60 | Data class: Suit enum, Rank enum, computed Value, constructor, ToString |
| `Deck.cs` | ~80 | 40-card deck, Fisher-Yates shuffle, Draw method, Count property |
| `CardSpriteLoader.cs` | ~85 | Static helper: maps Card to PNG sprite via `Resources.Load<Sprite>("CardSprites/{name}")`. Naming: `card_{suit}_{rank}` e.g. `card_diamonds_07`, `card_clubs_A`, `card_hearts_Q` |
| `CardObject.cs` | ~75 | MonoBehaviour: holds Card data, IPointerClickHandler for touch/click, visual states (normal/selected/dim), Reveal() method, InitColor() |
| `GameManager.cs` | ~430 | Main game logic: singleton, dealing, turn flow, manual capture, AI turn, rendering, UI buttons, instruction text. **This is the core file.** |
| `TouchInputSetup.cs` | ~25 | Bootstrap: adds Physics2DRaycaster to camera + EventSystem to scene if missing. Attach to Main Camera. Runs in Awake with `[DefaultExecutionOrder(-100)]` |
| `CardTester.cs` | ~50 | Throwaway test script (disabled). Can be deleted. |

### Card.cs
- `Card.Suit` enum: Clubs, Diamonds, Hearts, Spades
- `Card.Rank` enum: Ace=1, Two=2...Seven=7, Queen=8, Jack=9, King=10
- `Card.Value` computed from Rank (no backing field)
- Constructor: `Card(Suit suit, Rank rank)`
- `ToString()`: `"{Rank} of {Suit} (value={Value})"`

### CardObject.cs
- Implements `IPointerClickHandler` (mobile-first, no OnMouseDown)
- `Setup(Card card, bool faceUp)` -- sets data, call before adding sprite
- `Reveal()` -- flips face-up, updates sprite
- `OnPointerClick(PointerEventData)` -- calls `GameManager.Instance.OnCardClicked(this)` if face-up
- `SetSelected(bool)` -- yellow tint for selection
- `SetDim(bool)` -- gray tint for dimming non-selectable cards
- `InitColor()` -- stores original sprite color, call once after sprite assigned
- `Selected` property (read-only)
- `Card` property (read-only)
- `IsFaceUp` property (read-only, set via Reveal())

### GameManager.cs (the big one)
- Singleton: `Instance` property
- Turn flow: `TurnPhase` enum with `Idle`, `Selecting`, `AiTurn`
- **Player turn:**
  1. `Idle` phase: player clicks a hand card -> `PlayPlayerCard()` moves it to table, highlights yellow, enters `Selecting`
  2. `Selecting` phase: player clicks table cards to select/deselect (yellow tint). Can click played card again to cancel -> `CancelPlayedCard()` returns it to hand, back to `Idle`
  3. CAPTURE button (green when valid) or THROW button (orange) to resolve
- **CAPTURE:** validates sum of selected == played card value. Enforces mandatory single-card rule (if a single match exists, combos are rejected). Valid -> `ExecutePlayerCapture()` destroys captured cards + played card, adds to `playerCaptured` list. Chkobba check (table empty = bonus).
- **THROW:** `ThrowCard()` -- deselects table cards, played card stays on table, moves to AI turn.
- **AI turn:** `AiTurn()` -> `FindBestAiPlay()` (first card that can capture), `FindAutoCapture()` (single match first, then smallest combo via `TryCombo()`). Auto-resolves. 1-second delay via `Invoke`.
- **Re-deal:** `TryReDeal()` when both hands empty, deals `min(3, deck.Count)` cards each.
- **Rendering:** `RenderAll()` -> `RenderGroup()` for each group. Table cards compress with overlap when exceeding `visibleWidth`. `sortingOrder = i` for correct overlap rendering.
- **UI:** Created in code via `CreateUI()`:
  - Instruction text: anchored top-center `(0.5, 1)`, offset `(0, -40)`, font size 22
  - CAPTURE button: anchored bottom-right `(1, 0)`, offset `(-290, 30)`, size `(140, 50)`, green when active
  - THROW button: anchored bottom-right `(1, 0)`, offset `(-120, 30)`, size `(160, 50)`, orange when active
  - Both buttons gray + non-interactable when inactive
  - CAPTURE label shows "CAPTURE {sum}/{value}" when cards selected

### Layout constants
- `maxSpacing` = 1.2 (world units between cards)
- `cardScale` = 2
- `playerY` = -2 (bottom of screen)
- `aiY` = 2 (top of screen)
- `tableY` = 0 (center)
- `visibleWidth` = 9 (units; table cards compress below 1.2 spacing when total width exceeds this)
- Compression min spacing: 0.4 units

---

## 6. Scene Setup (GameScene.unity)

### GameObjects
- **Main Camera:** Position (0,0,-10), orthographic, **size 5** (needs changing to ~3 for landscape). Has `TouchInputSetup` component attached. Screen Space Overlay Canvas.
- **Background:** Position (0,0,10), Scale (30,20,1), green sprite. No change needed -- covers any camera size.
- **GameManager:** Position (0.42, 1.84, 0) -- **BUG: should be (0,0,0)**. Has GameManager component.
- **Canvas:** Screen Space Overlay, Canvas Scaler reference resolution 1080x1920 (portrait set, may need landscape update).
- **ScoreText:** On Canvas, position (0,0,0) -- needs repositioning (not yet used).
- **DividerLine:** Position (0,0,5), Scale (14, 0.05, 1). Visual separator between player and AI areas.
- **PlayerHandArea, AIHandArea, TableArea:** Empty GameObjects under Table (not currently used for positioning -- GameManager handles layout in code).

### Manual changes Aymen still needs to do in Unity Editor:
1. **Screen orientation:** Project Settings > Player > Android > Resolution > defaultScreenOrientation: change from Portrait (4) to Landscape Left (3)
2. **Camera orthographic size:** Change from 5 to ~3 for landscape view
3. **GameManager position:** Change from (0.42, 1.84, 0) to (0, 0, 0) -- cosmetic but should be fixed

---

## 7. Git Status

- **Repo location:** `C:\Users\ELITE\Documents\unity game\Chkobba` (local git repo, no remote)
- **First commit:** `17f495a` -- "Step 13: clickable cards + landscape layout + table compression" (226 files, root commit)
- **Uncommitted changes (as of 2026-07-02):**
  - Modified: `GameManager.cs` (two-button CAPTURE/THROW system, cancel played card, yellow tint fix, button repositioning)
  - Modified: `CardObject.cs` (IPointerClickHandler replacing OnMouseDown)
  - Modified: `GameScene.unity` (TouchInputSetup component added)
  - Modified: `ProjectSettings.asset` (possibly orientation change)
  - Modified: `Chkobba.slnx`
  - New (untracked): `TouchInputSetup.cs` + `.meta`
- **.gitignore:** Covers Library/, Temp/, Obj/, Build/, VS/VSCode files, OS junk, build artifacts
- **No GitHub remote configured yet** -- Aymen can set this up when ready

---

## 8. What Works

- Card data model (Card.cs, Deck.cs) -- complete and working
- Sprite loading (CardSpriteLoader.cs) -- complete and working
- Deck shuffle and deal -- working
- Player can click a hand card to play it (IPointerClickHandler, mobile-first)
- Played card highlights yellow, moves to table
- Player can select/deselect table cards (yellow tint toggle)
- Player can click played card again to cancel and return it to hand
- CAPTURE button: green when valid selection, shows sum progress "CAPTURE 7/7"
- THROW button: orange, leaves card on table
- Mandatory single-card capture rule enforced
- AI turn: auto-plays, auto-captures (single first, then smallest combo), 1s delay
- Chkobba detection (table cleared = bonus)
- Re-deal when hands empty
- Table card compression (overlap when too many cards)
- Instruction text guides player at each step
- TouchInputSetup bootstraps EventSystem + Physics2DRaycaster

## 9. What Doesn't Work / Known Bugs

- **GameManager position** is (0.42, 1.84, 0) instead of (0,0,0) -- cosmetic, doesn't affect gameplay since GameManager doesn't use its own transform
- **Camera size still 5** -- Aymen needs to manually change to ~3 in Unity Editor for proper landscape view
- **Screen orientation** -- Aymen needs to manually change to Landscape Left in Project Settings
- **Canvas scaler** is set to 1080x1920 (portrait) -- may need updating to 1920x1080 for landscape
- **ScoreText** exists but not used yet -- no scoring system implemented
- **CardTester.cs** is a disabled throwaway script -- can be deleted

---

## 10. Next Steps (Priority Order)

1. **Aymen manual Unity Editor changes:** camera size 5->3, screen orientation to Landscape Left, GameManager position to (0,0,0)
2. **Git commit** the current working state (two-button system, IPointerClickHandler, tint fix, button positions)
3. **Scoring system:** Calculate points from captured cards + Chkobba bonuses (see RULES.md for scoring rules)
4. **Multi-capture edge cases:** What if multiple singles match? What if player wants strategic miss?
5. **Visual feedback for missed captures** (vibration/highlight) -- Aymen suggested, deferred
6. **Cutting phase** (player cuts deck after AI deals) -- Chkobba tradition, not yet implemented
7. **Round end / game end detection** -- when deck is empty and all cards played
8. **Score display UI** -- ScoreText exists but not wired up
9. **GitHub remote** -- set up and push when Aymen is ready
10. **Build for Android** -- configure build settings, test on phone

---

## 11. Key Decisions Made

- **Landscape orientation** chosen over portrait, inspired by UNO mobile
- **Two-button UI** (CAPTURE + THROW) replaces older "click played card to confirm" mechanic. Both buttons always visible, dimmed when inactive.
- **Manual capture selection** (not automatic) -- player picks which table cards to capture
- **Mandatory single-card capture** -- if both single and combo match, player MUST take the single (Chkobba rule)
- **Missa rule (no penalty for missed capture)** -- per RULES.md, Tunisian variant. Not externally verified.
- **Skip cutting phase for now** -- add later
- **No Hand/TableSlot abstractions** -- raw Lists suffice at this stage
- **Procedural card rendering** -- GameObjects created at runtime with SpriteRenderer, no prefabs
- **1-second Invoke delay for AI turn** -- lets player see what happened
- **AI plays first capture-capable card** (no strategy yet)
- **Ponytail philosophy:** YAGNI, reuse before writing, stdlib first, fewest lines possible, no over-engineering

---

## 12. Gotchas / Things to Watch Out For

- **PowerShell here-strings corrupt C# quotes:** `@"..."@` escapes `"` as `""`. Always use `[System.IO.File]::WriteAllText()` to write C# files from PowerShell.
- **Sandbox restrictions:** Some tools can only read from `~/.openclaw-autoclaw/workspace/`. Use `exec` with `Get-Content` to read project files outside sandbox.
- **`FindObjectOfType<T>()` is deprecated in Unity 6.5:** Use `FindAnyObjectByType<T>()` instead (CS0618 warning).
- **Physics2DRaycaster is required on camera** for IPointerClickHandler to fire on 2D sprites. Without it, clicks never register. TouchInputSetup.cs auto-adds it.
- **EventSystem + StandaloneInputModule** required for any EventSystem-based input. Unity auto-creates when Canvas is added, but our cards are 2D sprites (not UI), so TouchInputSetup.cs ensures it exists.
- **BoxCollider2D** must be added to card GameObjects for Physics2DRaycaster to hit them.
- **`playedCard.SetSelected(false)` must be called before nulling** -- otherwise the card GameObject stays yellow forever. This was a real bug.
- **Card sprite naming:** `card_{suit}_{rank}` where suit is lowercase (clubs/diamonds/hearts/spades) and rank is `A`, `Q`, `J`, `K`, or zero-padded number `02`-`07`. Example: `card_diamonds_07`, `card_clubs_A`.

---

## 13. Installed Skills (OpenClaw/AutoClaw)

These were installed on the previous AI (Jarvis). If migrating to another OpenClaw instance, they can be reinstalled:
- **ponytail** (from `dietrichgebert/ponytail`) -- YAGNI philosophy, do-less coding skill
- **ponytail-review** (from `dietrichgebert/ponytail`) -- code review with YAGNI lens
- **systematic-debugging** (from `obra/superpowers`) -- structured debugging methodology
- **verification-before-completion** (from `obra/superpowers`) -- verify before claiming done
- **writing-plans** (from `obra/superpowers`) -- structured planning before implementation

If migrating to a non-OpenClaw AI, these are just philosophy guides -- ponytail = minimize code, YAGNI; verification = test before claiming done; systematic debugging = reproduce, isolate, fix, verify.

---

## 14. How to Use This Document

1. Read this entire document first
2. Read `RULES.md` in the project folder for full Chkobba rules
3. Read the actual script files in `Assets/Scripts/` to see current code
4. Ask Aymen to test any changes in Unity and report what he sees
5. Make one change at a time, verify it works, then move on
6. Commit to git after each working component
7. Use small steps -- Aymen is learning and prefers incremental progress
8. Never use OnMouseDown or PC-mouse input -- always EventSystem/IPointerClickHandler
