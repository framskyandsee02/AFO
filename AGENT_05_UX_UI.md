# AGENT 05 — UX / UI

> **Read `MASTER_BUILD_PROMPT.md` first.** Current state is §2, the events you
> subscribe to are §3, and §0 holds the three rules that override everything.
> This file is your slice of §7 (Track C). Where the two disagree, MASTER wins.

**Role:** make the game understandable without a human explaining it.
**Project root:** `E:\!for WORK\all for one`

---

## Scope

| You own | You must not touch |
|---|---|
| `Assets/05_UI/HUDDisplay.cs` | `Assets/01_GameSystems/*` (Agent 01) |
| The entire Canvas hierarchy | `Assets/02_SupportSystems/*.cs` (Agent 02) |
| `HintTrigger` placement and wording | `Assets/04_VFX/*` (Agent 04) |
| Any pause panel, if approved | Layout and models (Agent 03) |

**`HintTextManager.cs` is Agent 02's file**, but its *display* — the label, the
CanvasGroup — is yours. You build what it drives; you do not edit it.

**Depends on:** Agent 01 Task 1 (a HUD must reflect systems that work) and
Agent 02 Task 4 (the pause decision).
**Blocks:** nobody, but the slice is unplayable-by-a-stranger without you.

---

## Task 1 — Build the Canvas

`HUDDisplay` and `HintTextManager` are written, attached to `Canvas`, and
**every serialized field is empty.** The Canvas has no children. Nothing appears
on screen. This is the single largest visible gap in the project.

### `HUDDisplay` expects

| Field | Type | Notes |
|---|---|---|
| `energyFill` | `Image` | Set Image Type to **Filled**, Fill Method Horizontal |
| `energyLabel` | `TextMeshProUGUI` | Shows `current / max` |
| `targetPanel` | `GameObject` | Shown only while aiming at something; hidden otherwise |
| `targetNameLabel` | `TextMeshProUGUI` | `TimeTarget.displayName` |
| `targetStateLabel` | `TextMeshProUGUI` | Current state name, or `Locked` when gated |
| `puzzleProgressLabel` | `TextMeshProUGUI` | `matched / total` |
| `crosshair` | any `Graphic` | **Recolours by state** — see below |

### `HintTextManager` expects

| Field | Type | Notes |
|---|---|---|
| `hintLabel` | `TextMeshProUGUI` | The game's only narrator |
| `hintGroup` | `CanvasGroup` | On the hint's parent. **Must start at alpha 0** |

### The crosshair carries the gating rule
Three colours are already wired in `HUDDisplay`: idle (nothing aimed at), active
(usable target), gated (target exists but is locked behind its prerequisite).

**That colour change is the player's first signal that the bridge is not yet
fixable.** It fires before they press anything. Choose colours that read
instantly — a gated target must not look merely "slightly different" from a usable
one.

### Build notes
- Canvas Scaler: Scale With Screen Size, 1920×1080 reference, Match 0.5
- **Every field is optional and null-guarded** — build in pieces and test as you go, a half-built Canvas still runs
- TextMeshPro ships with uGUI 2.0.0 (MASTER §2) — **no package install needed**
- Test at 16:9 and 16:10

---

## Task 2 — Teach Q and E without a manual

The brief forbids NPCs, voice and cutscenes. **Text is all there is.**

Place `HintTrigger` volumes (the component exists — `Assets/02_SupportSystems/HintTrigger.cs`,
filters on tag `Player`, fires once by default):

| Where | Teaches |
|---|---|
| Near spawn | The gauntlet exists; aim at something |
| Approaching the tree | Q winds back |
| First approach to the bridge | Nothing — **let the gated refusal be the lesson** |

Keep each line under about 12 words.

**Diegetic beats instructional** — *"The tree remembers being younger"* reads better
than *"Press Q to rewind"*. But clarity wins: if testing shows players are lost,
say so and make it explicit. An elegant hint nobody understands is a failed hint.

Coordinate with Agent 03 — trigger placement depends on their layout.

---

## Task 3 — The restoration moment

Shared with Agent 04 Task 3. **Coordinate before either of you touches the camera.**

Your half: the hint line that marks the moment, its timing, and how it fades.
`RestorationController` calls `HintTextManager.ShowHint(restorationMessage, 4f)`
already — the message is a serialized string on that component, which Agent 04 owns.

**Agree on who writes the line** rather than both setting it.

---

## Task 4 — Pause panel ⛔ blocked on Agent 02's Q5

`PlayerCameraRig` frees the cursor on Escape and relocks on left click. There is
no menu. Q and E still work while the cursor is free.

**Do not build a panel until Q5 is answered** (Agent 02 Task 4). If the answer is
"yes, build it": Resume / Restart / Quit, and `HintTextManager.ClearHint()` on
open so a stale hint does not sit behind the menu.

---

## Acceptance

- [ ] Energy visibly drops on every gauntlet use
- [ ] Aiming shows name and state; **a gated target reads unmistakably differently from a usable one**
- [ ] Puzzle progress visible
- [ ] Rejection reasons appear **on screen**, not only in the console
- [ ] **A first-time player discovers Q and E without being told** — this is the real test of this track
- [ ] The restoration moment registers as a payoff
- [ ] Layout holds at 16:9 and 16:10
- [ ] No `NullReferenceException` from any partially assigned field
- [ ] **Observed in Play mode**, not just authored in the editor

---

## Ask, do not decide

**Ask:** the pause panel (blocked on Q5) · any camera control during restoration
(coordinate with Agent 04) · anything that changes a script outside `Assets/05_UI/`.

**Decide alone:** Canvas hierarchy, anchoring, fonts, colours (including the three
crosshair states), hint wording, fade timings, panel layout once approved.

---

## Report

Verdict · what you ran and saw in Play mode · files and GameObjects created ·
**screenshots at both aspect ratios if you can produce them** · findings by
severity · questions blocking you · **anything in MASTER §2 that turned out to be
wrong.**
