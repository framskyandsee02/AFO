# AGENT 01 — Gameplay Systems

> **Read `MASTER_BUILD_PROMPT.md` first.** It holds current state (§2), the public
> API you must not break (§3), and the three rules that override everything
> (§0: verify don't assume · ask don't guess · never claim an unrun test passed).
> This file is your slice of §5 (Track A). Where the two disagree, MASTER wins.

**Role:** owner of the Time Gauntlet loop. Your track gates everyone else.
**Project root:** `E:\!for WORK\all for one`

---

## Scope

| You own | You must not touch |
|---|---|
| `Assets/01_GameSystems/*.cs` | `Assets/02_SupportSystems/*` (Agent 02) |
| Balance values inside those scripts | `Assets/04_VFX/*` (Agent 04) |
| The puzzle's logical structure | `Assets/05_UI/*` (Agent 05) |
| `Assets/Editor/BorrowedTimeSceneSetup.cs` | Models, layout (Agent 03) |

**Scene and prefab files belong to whoever holds Unity MCP.** If that is not you,
deliver scripts plus written wiring instructions.

**Blocks:** Agents 02, 04, 05 all wait on your Task 1.
**Depends on:** nothing. You start first.

---

## Task 1 — Play-test what exists ⚠️ DO THIS BEFORE WRITING ANY CODE

**Nothing in this project has ever run.** Compile-clean and scene-wired, never
observed executing. Find out what is actually broken before adding to it.

Run each step in Play mode. Report **PASS / FAIL / NOT RUN** with what you saw.

| # | Step | Expected |
|---|---|---|
| 1 | Enter Play, click to lock cursor | Cursor hides; controls live |
| 2 | WASD / Shift / Space | Moves, sprints, jumps; does not fall through `Ground` |
| 3 | Orbit camera 360° at several pitches | No flip, no clipping through ground or greybox |
| 4 | Aim centre-screen at `Tree_Target` (-4, 0, 6), press Q | Old → Mature; energy 100 → 85 |
| 5 | Aim at `Bridge_Target` (3, 0, 10), press Q ×2 | Broken → Worn → Intact; energy 85 → 45 |
| 6 | — | `puzzle_01_bridge` fires; zone unlocks; fog blends |
| 7 | Reload, aim at bridge **first**, press Q | Refused with the gated hint |
| 8 | Stand **on** the Intact bridge, aim down, press E ×2 | **Report exactly what happens** |

**Step 8 is the one that matters most.** The player is standing on a collider that
gets disabled mid-frame. Ordinary games never hit this; this one hits it by design.
Describe the real behaviour — fall, clip, eject, freeze — do not reason about what
*should* happen.

**Expect several failures.** This is the first run. Finding five real bugs here is
a better outcome than reporting five passes.

Fix what fails. Re-run. Report both passes.

---

## Task 2 — Make the gate legible

`TimeGauntletController.OnUseRejected` already fires with a human-readable reason.
**Nothing subscribes to it.** A player who aims at the bridge first currently gets
silence.

Agent 05 builds the on-screen display; Agent 02 owns the wiring to
`HintTextManager`. **Your job is to confirm the reason strings are actually
useful** — aim at every target in every state and read what comes out. A string
that says "cannot go further back" when the player needs to know *why the bridge
is inert* is a failure of your layer, not theirs.

If a reason is unhelpful, fix the string or the logic that chooses it.

---

## Task 3 — Softlock audit

The player must never reach an unwinnable state.

- Solving costs 55 of a 100 pool (tree 15 + bridge 20×2). **Verify that is still true** — read the inspector, do not trust this document.
- With regen on, can a player strand themselves? There is nothing else to spend on today. Say that explicitly rather than concluding "no softlock possible" — the answer changes the moment Agent 03 adds a second puzzle object.
- Confirm `PlayerController.fallResetY` recovery does **not** reset energy or puzzle state. By design it must not.
- Confirm `PuzzleCondition` with `latchWhenSolved` cannot fire `OnSolved` twice, and that `ZoneUnlock.Start()` re-checking `IsSolved` cannot double-fire alongside the event path.

---

## Acceptance

- [ ] Every step of Task 1 passes, **observed in Play mode**
- [ ] Step 8 behaviour described concretely in the report
- [ ] Every rejection reason read aloud and judged useful
- [ ] Softlock audit complete, with the "no second spend sink yet" caveat stated
- [ ] No console errors across a full playthrough
- [ ] No public signature in MASTER §3 changed

---

## Ask, do not decide

**Q1 — Energy regen.** Passive regen (4/sec after 1.5s) exists purely as softlock
insurance. It may be undercutting the resource tension the brief asks for. The
alternative: remove regen, and guarantee instead that a full pool always affords
every required change. **This is your call to recommend — and the user's to make.**
Recommend with reasoning, then wait.

**Q3 — Gated-hint granularity.** Each `TimeTarget` carries a designer-written
rejection string. Alternative: derive the reason from the prerequisite's actual
state so it cannot go stale when the puzzle changes. Recommend, then wait.

**Decide alone:** numeric tuning, script internals that keep the §3 surface
intact, which greybox primitive stands in for what.

---

## Report

Verdict (COMPLETE / PARTIAL / BLOCKED) · what you ran and saw · files changed ·
findings by severity · questions blocking you · **anything in MASTER §2 that
turned out to be wrong.**

That last item matters: a stale fact in MASTER will mislead the next agent.
