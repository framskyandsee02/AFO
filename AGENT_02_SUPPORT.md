# AGENT 02 — Support Systems

> **Read `MASTER_BUILD_PROMPT.md` first.** Current state is §2, the public API you
> must not break is §3, and §0 holds the three rules that override everything.
> Where this file and MASTER disagree, MASTER wins.

**Role:** input, state, audio, and the plumbing that carries messages to the player.
**Project root:** `E:\!for WORK\all for one`

---

## Scope

| You own | You must not touch |
|---|---|
| `Assets/02_SupportSystems/*.cs` | `Assets/01_GameSystems/*` (Agent 01) |
| `Assets/InputSystem_Actions.inputactions` — **only after Q4 is answered** | `Assets/04_VFX/*` (Agent 04) |
| Audio routing and cue logic | `Assets/05_UI/*` (Agent 05) |
| | Scene/prefabs unless you hold Unity MCP |

**Note on `PlayerController.cs` / `PlayerCameraRig.cs`:** these live in your folder
and were delivered by Codex. They are yours to maintain, but Agent 01 is play-testing
them in Track A1 — **coordinate before editing**, or you will both change the same
file from different reports.

**Depends on:** Agent 01's Task 1 report.
**Blocks:** Agent 05 (the pause decision and the hint wiring).

---

## Task 1 — Carry rejection reasons to the player

`TimeGauntletController.OnUseRejected` fires with a readable reason on every
refused use. **Nothing subscribes.** The player gets silence when the bridge
refuses them — which is exactly the moment the puzzle is trying to teach.

Wire it to `HintTextManager.ShowHint()`. Where that subscription lives is your
call — a small listener component, or inside an existing support script.

Also consider: should `OnInsufficientEnergy` produce a distinct line from a gated
refusal? They are different failures and a player should be able to tell them apart.

**Until Agent 05 builds the Canvas, `HintTextManager` has no label to write to and
nothing will appear.** That is expected. Wire it anyway and verify via
`Debug.Log` that the right strings reach the right place at the right moment.

---

## Task 2 — AudioCueManager

Specified during planning, **never written.** Write
`Assets/02_SupportSystems/AudioCueManager.cs`:

- Subscribe to: `TimeGauntletController.OnGauntletUsed`, `OnUseRejected`,
  `EnergyManager.OnInsufficientEnergy`, `PuzzleCondition.OnSolved`,
  `ZoneUnlock.OnZoneUnlocked`
- One `AudioSource`; one `AudioClip` slot per event
- **Every clip field optional and null-guarded.** Empty slots mean silence, never a crash
- Unsubscribe in `OnDisable` — symmetry matters, MASTER §3 lists the events
- Distinguish rewind from accelerate using the `int direction` argument (-1 / +1)

**Do not source or generate audio files.** Expose the slots and leave them empty.
A silent slice ships; a crashing one does not. Tell the user which slots exist so
they can fill them if they want.

---

## Task 3 — Input backend decision ⛔ ask before acting

Everything currently reads `Keyboard.current` directly:
- `BorrowedTimeInput` — Q, E, F, R
- `PlayerController` — WASD, Shift, Space
- `PlayerCameraRig` — Escape, mouse

`InputSystem_Actions.inputactions` sits in the project **unused**. No generated
action class exists, which is why the direct reads were written in the first place.

**Q4:** generate the action class properly, or keep reading the keyboard directly?
Generating is cleaner and makes rebinding possible later, but it touches an asset
and changes three scripts at once. Recommend with reasoning, **then wait.**

If the answer is "generate": the swap should be contained to each script's input-
reading method. If you find yourself restructuring beyond that, stop and report.

---

## Task 4 — Pause and cursor ⛔ ask before acting

`PlayerCameraRig` unlocks the cursor on Escape and relocks on left click.
`PlayerController` stops horizontal movement while the cursor is free. **But Q and
E still work** — the gauntlet does not check cursor state.

**Q5:** two questions, and they are separable:
1. Should Escape open a pause panel (Resume / Restart / Quit), or only free the cursor?
2. Should Q/E be suppressed while the cursor is free?

Agent 05 builds any panel. **You own whether the input layer suppresses gauntlet
input** — that is a one-line change in the right place or a tangle in the wrong one.
Recommend, then wait.

---

## Acceptance

- [ ] Rejection reasons reach `HintTextManager`, verified by log even before the Canvas exists
- [ ] Insufficient-energy reads differently from a gated refusal, or a reasoned argument for why it should not
- [ ] `AudioCueManager` compiles, subscribes and unsubscribes symmetrically, and is silent-safe with every slot empty
- [ ] Q4 and Q5 answered by the user before any code lands for them
- [ ] No public signature in MASTER §3 changed
- [ ] No edit to `PlayerController` / `PlayerCameraRig` that collides with Agent 01's in-flight fixes

---

## Report

Verdict · what you ran and saw · files changed · findings by severity · your
recommendations on Q4 and Q5 with reasoning · **anything in MASTER §2 that turned
out to be wrong.**
