# Chkobba - Locked Ruleset (v1.1)

> **v1.1 revision (2026-09-07):** Every round starts with a cut (the cutter alternates between rounds) and a fresh 4-card table layout from a fresh shuffled deck - confirmed by Aymen against real-life play, superseding the v1 "cutting is round 1 only / no new table cards" rules. Within a round, when all hand cards are played, 3 more cards are dealt to each player (no new table cards) until the deck is exhausted; then the round ends with the Final Sweep and scoring.

**Status:** Locked for Phase 1 development
**Scope:** **1v1 (Human vs AI) only.** 2v2 (team mode) is a planned v2 feature and will require additional rules to be researched and documented separately.
**Source of truth:** This document
**Primary reference:** [officialgamerules.org/game-rules/chkobba](https://officialgamerules.org/game-rules/chkobba/) (last updated April 15, 2025)
**Secondary references:** Player's family/Regional Tunisian play tradition; cross-checked with Italian Scopa Wikipedia article

---

## 1. Game Overview

Chkobba is a traditional North African card game, a descendant of Italian Scopa, especially popular in Tunisia. Players capture cards from the center of the table by matching values or summing combinations.

**Players for v1:** 2 (Human vs AI)
**Win condition:** First to reach target score (11 or 21, player-selectable)

---

## 2. The Deck

- **40 cards total** (Italian-suited OR French-suited, both valid)
- If using a standard 52-card French deck: **remove all 8s, 9s, and 10s** (12 cards removed → 40 remain)
- **Suits (4):** Coins/Diamonds, Cups/Hearts, Swords/Spades, Clubs
- For v1 we will use the **French-suited deck** (Diamonds, Hearts, Spades, Clubs) since it's more common in Tunisia and easier to source art for

### Card Values

| Card | Value |
|------|-------|
| Ace (A) | 1 |
| 2–7 | Face value (2, 3, 4, 5, 6, 7) |
| Queen (Q) | 8 |
| Jack (J) | 9 |
| King (K) | 10 |

**Note:** "The Jack and Queen switch roles compared to standard decks" — i.e., Queen is worth 8, Jack is worth 9. This is official Chkobba convention, not a typo.

---

## 3. Setup and Roles

- Two players: one **Dealer**, one **Cutter**
- Roles alternate between rounds (player who was dealer becomes cutter next round, and vice versa)
- For v1: Human is cutter in round 1; AI is dealer. Roles swap each subsequent round.

---

## 4. Cutting Phase (Round 1 Only)

After shuffling, the cutter cuts the deck and reveals one card. The cutter then chooses:

### Option A — Keep the card
1. Cutter keeps the revealed card in their hand
2. Dealer gives cutter **2 more cards** (cutter now has 3 cards total)
3. Dealer places **4 cards face-up** on the table (initial layout)
4. Dealer takes **3 cards** for themselves

### Option B — Reject the card
1. Cutter places the revealed card face-up on the table
2. Dealer adds **3 more cards** to the table (total = 4 cards on table)
3. Dealer gives cutter **3 cards**
4. Dealer takes **3 cards** for themselves

### Initial Layout Reshuffle Rule
If the face-up layout (4 cards on table) contains **3 or 4 cards of the same rank**, the deck is reshuffled and re-dealt. This prevents degenerate opening positions.

---

## 5. Dealing (Rounds 2+)

Each round starts with a fresh shuffled deck and a cut (the cutter alternates between rounds). After the cut resolves:
1. Dealer gives 3 cards to opponent (cutter)
2. Dealer takes 3 cards for themselves
3. **No new cards added to the table** — only hands are replenished

Hands are then played. When all hand cards are played, deal another 3 to each player (no new table cards) and continue. When the deck is exhausted and all cards have been played, the round ends (Final Sweep Rule + scoring) and the next round begins. Rounds repeat until a player reaches 11 points.

---

## 6. Gameplay

### Turn Order
- Play moves **counterclockwise** (traditional direction)
- For 1v1: this means the cutter plays first, then dealer, then cutter, etc.

### On Each Turn
The active player plays **one card** from their hand. Two outcomes:

### Outcome A — Capture (match exists)
The played card can capture if it matches either:
1. **A single card** on the table with the same value, OR
2. **A combination of cards** whose sum equals the played card's value

**Mandatory Single-Card Capture Rule (CRITICAL):**
> If both a single card AND a combination would match the played card, the player **MUST** capture the single card. The player cannot choose the combination.

**Example:**
- Player plays a 7
- Table has: 7, 4, 3
- Player MUST capture the 7 (single match)
- Player CANNOT choose to capture 4+3 (sum match) instead

### Outcome B — No capture (no match exists)
The played card stays on the table, added to the layout. No cards are captured this turn.

### After All 3 Cards Played
- Deal another 3 cards to each player (no new cards to the table)
- Continue until the deck is exhausted, then the round ends: Final Sweep + scoring
- A new round begins (fresh deck, cut, fresh table layout)
- Rounds repeat until a player/team reaches 11 points

---

## 7. Chkobba (Sweep Bonus)

A **Chkobba** occurs when a player captures **all cards** on the table in a single turn (table becomes empty after their capture).

- Each Chkobba = **+1 point** at end of round scoring
- The played card that triggered the Chkobba is placed **face-up** in the player's captured pile (other captured cards are face-down) to mark the Chkobba for counting

### Final Sweep Rule (After Last Round)
- Any cards remaining on the table after the last play go to the **last player/team who made a capture**
- This does NOT count as a Chkobba (no bonus point)
- **Dealer restriction:** The dealer can never score a Chkobba with the last card of the game (prevents dealer advantage)

---

## 8. Scoring

Points are awarded after all cards have been played (end of round). Four scoring categories + Chkobba bonuses:

| # | Category | Points | Description |
|---|----------|--------|-------------|
| 1 | **Kārṭa** (Cards) | 1 | Most total cards captured. No point if tied 20–20. |
| 2 | **Dīnārī** (Diamonds) | 1 | Most diamonds captured. No point if tied 5–5. |
| 3 | **Barmīla** (Primiera) | 1 | Most Sevens captured. If tied, most Sixes breaks the tie. If still tied, no point. |
| 4 | **Sabʿa l-ḥayya** (7 of Diamonds) | 1 | Capturing the 7 of diamonds awards 1 point. |
| 5 | **Chkobba** | +1 each | For each Chkobba achieved during the round. |

**Maximum points per round:** 4 (categories) + N (Chkobbas) where N is typically 0-4

---

## 9. Winning the Game

- Target score is **player-selectable**: 11 (short game) or 21 (long game)
- First player to reach target score wins
- **Tie at target score:** Play one additional round to break the tie. Repeat if still tied.

---

## 10. Edge Cases and Notes

### Reshuffle Triggers
- Initial layout has 3+ cards of same rank → reshuffle and re-deal (see Section 4)

### Dealer Advantage Mitigations
- Dealer cannot score final-card Chkobba
- Roles alternate each round (dealer becomes cutter next round)

### Card Counting Reference
- 40-card deck
- 2 players × 3 cards per deal = 6 cards per deal
- 4 cards initial table layout
- Total table cards over the game: 4 initial + 34 remaining = 38 (after first deal, 34 cards remain)
- Round model (v1.1): each round starts with a fresh 40-card deck (cut + 3/3 hands + 4 table = 10 cards in play, 30 left in the deck). The deck then feeds 3+3 re-deals until exhausted (exactly 5 re-deals, never uneven), which ends the round. Rounds repeat until a player reaches 11 points.

### Memory Aid for AI Implementation
- Track: deck, player hand (3 cards), AI hand (3 cards), table layout (variable 0-N cards), captured piles (player + AI), Chkobba counts
- Card identity must preserve: rank, suit, value (suit only matters for Diamonds scoring and 7♦ bonus)
- Chkobba marker: each captured card needs a flag indicating "this card triggered a Chkobba"

---

## 11. Variations Considered but NOT Included in v1

For future versions (v2+), consider:
- 4-player team mode (2v2)
- 31-point long game variant
- Traditional Italian Primiera scoring (complex point scale: 7=21, 6=18, A=16, 5=15, 4=14, 3=13, 2=12, face=10)
- Pass-and-play local multiplayer
- Online multiplayer

---

## 12. Source Verification Log

| Rule | Source | Verification Status |
|------|--------|---------------------|
| 40-card deck | officialgamerules.org + Wikipedia (Scopa) + GitHub repos | Verified |
| Card values (Q=8, J=9, K=10) | officialgamerules.org + player's family tradition | Verified |
| Cutting mechanic | officialgamerules.org + player's family tradition | Verified (initially undocumented elsewhere) |
| Mandatory single-card capture | officialgamerules.org | Verified |
| Primiera = most 7s, then 6s | officialgamerules.org | Verified (different from Italian Scopa's complex version) |
| Initial layout reshuffle (3+ same rank) | officialgamerules.org | Verified (newly discovered) |
| Counterclockwise play | officialgamerules.org | Verified (newly discovered) |
| Chkobba card marker (face-up) | officialgamerules.org | Verified (newly discovered) |
| Dealer cannot score last-card Chkobba | officialgamerules.org | Verified (newly discovered) |
| Tie at target → extra round | officialgamerules.org | Verified (newly discovered) |
| Target scores 11/21 | officialgamerules.org + player's preference | Verified |
| Missa rule (no penalty for missed capture) | Player's tradition | NOT verified externally — assumed Tunisian variant |

---

## Change Log

| Date | Version | Change | Author |
|------|---------|--------|--------|
| 2026-06-24 | 1.0 | Initial locked ruleset | AI + Player collaboration |

---

**This document is the single source of truth for Chkobba v1 development.**
Any code that contradicts this document is a bug. Any rule change requires updating this document first.
