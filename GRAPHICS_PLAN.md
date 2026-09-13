# Graphics Plan — Chkobba (TunToon)

> Goal: store-level presentation ("UNO-level" polish). Landscape 1v1 first; portrait + 2v2 deferred.
> Status: planning. Aymen picks style direction (Decision 2) before production starts.

## Decision 1 — Where art comes from

| Asset | Source | Why |
|---|---|---|
| Card faces (40) + back | **Programmatic generator** (script draws all cards in one style) | Deterministic consistency — 42 AI-generated faces would never match; a generator guarantees it and restyles the whole deck in minutes |
| Table background (wood + felt + suit ornaments) | AI-generated texture or CC0 pack | Few pieces, style-tolerant |
| Logo + menu backdrop | Typography + suit icons, AI-assist | Needs TunToon identity |
| UI chrome (buttons, panels, banners) | Programmatic 9-slice sprites | Clean, scalable, cheap |
| Avatars (turn indicators) | Simple vector circles v1 → AI portraits later | Low priority |
| Fonts | Google Fonts OFL (Fredoka / Baloo 2) | Replace Unity LegacyRuntime — instant quality jump |

Rule: no AI-generated card FACES in v1 (consistency risk). Cards come from the generator.

## Decision 2 — Style direction (Aymen picks)

**A. Classic Casino** (competitor-like): wood rim, green felt, classic white cards, red/black suits. Familiar to chkobba players, fastest to ship.
**B. TunToon Signature**: bold flat cartoon, custom palette (Tunisian warm tones: terracotta/sea blue/jasmine white), custom suit marks. Most distinctive — the brand play. Most art direction effort.
**C. Modern Minimal**: dark slate table, clean geometry, muted gold accents, thin borders. Elegant, timeless, less "casual game".

Recommendation: **A as the base + a TunToon flavor pass** (brand colors, logo, card back design) — familiar enough for chkobba players, branded enough to be ours. B is the v2 rebrand option.

## Phases (each ends with a build + your verdict)

- **G1 — Style prototypes**: card generator produces sample faces in 2-3 palettes; AI generates 2-3 table/menu backdrops. You pick the direction. (1 session)
- **G2 — Card set production**: generator outputs all 41 PNGs (40 faces + back) into `Assets/Resources/CardSprites/`, replacing Kenney. (1-2 sessions)
- **G3 — Environment**: table background (wood rim + felt band + corner suit ornaments), menu backdrop, logo placement. (1 session)
- **G4 — UI chrome + typography**: button sprites (normal/pressed/disabled), styled score panel + instruction banner + round-end panel, fonts swapped project-wide. (1-2 sessions)
- **G5 — Animation upgrade** (see below). (2+ sessions, spread)
- **Sound** — separate phase after graphics.

## Animations — keep or rebuild?

**Keep the tween system.** It's the engine; UNO-level polish is choreography on top, not a new engine. Upgrade list (additive, in order):

1. Arc trajectories — throws and fly-to-pile follow a parabolic arc instead of straight lines
2. Card flips — AI reveal + pile landing flip (sprite swap → proper flip tween)
3. Deal-in — opening deal flies from the deck corner, staggered per card
4. Capture juice — scale punch + small burst where cards land in the pile
5. Chkobba moment — screen flash + burst + big text (the hype beat)
6. Turn indicator — glowing ring on the active player's side (needs avatar art from G3)
7. Button press micro-interactions — scale-down on press, spring back

Timing rule: never two animations fighting for the eye (same lesson as the capture/reflow fix).

## Deferred (unchanged)

Portrait mode, 2v2, online, store submission prep (icon/screenshots/signing) — after graphics + sound.
