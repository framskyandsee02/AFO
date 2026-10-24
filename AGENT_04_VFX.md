# AGENT 04 — VFX & The Restoration Moment

> **Read `MASTER_BUILD_PROMPT.md` first.** Current state is §2, the public API is
> §3, and §0 holds the three rules that override everything. Where this file and
> MASTER disagree, MASTER wins.

**Role:** make time-manipulation feel like something, and make the restoration land
as a payoff rather than a state change.
**Project root:** `E:\!for WORK\all for one`

---

## Scope

| You own | You must not touch |
|---|---|
| `Assets/04_VFX/RestorationController.cs` | `Assets/01_GameSystems/*` (Agent 01) |
| Particle systems and their prefabs | `Assets/02_SupportSystems/*` (Agent 02) |
| Lighting setup, before and after | Canvas and UI (Agent 05) |
| Fog / atmosphere handling | Layout and models (Agent 03) |

**Depends on:** Agent 01 Task 1 (a working loop) and Agent 03 Task 1 (geometry to light).
**Blocks:** nobody. You are the last layer — which means you are also the one most
likely to get squeezed. Task 1 is the one that cannot be cut.

---

## Task 1 ⛔ — Resolve the fog problem · ASK FIRST, DO NOT BUILD AROUND IT

`RestorationController` writes global `RenderSettings.fogColor` and `fogDensity`.

**This is a real defect, and I wrote it.** Two consequences:
1. It mutates project-wide render state, not scene-local state.
2. **It persists into the editor after Play mode stops.** The scene stays
   restored-looking until someone changes it back by hand. A developer who plays
   once then reopens the scene sees the "after" state and may not know why.

Three options:

| Option | Trade-off |
|---|---|
| Capture on enable, restore on disable | Smallest change; still global during play |
| Move to a URP Volume override | Correct for URP; more setup; needs a Volume + profile in the scene |
| Accept it | Zero work; the editor confusion stays |

**Recommend one with reasoning and wait for the answer.** Do not build particle
work on top of the current behaviour and leave this for later — the fix may change
where the atmosphere transition lives, which changes what your VFX has to blend with.

---

## Task 2 — Gauntlet feedback

`TimeGauntletController` exposes `rewindVFX` and `accelerateVFX` slots, and
`TimeTarget` exposes `changeVFX`. **All three are empty.** Using the gauntlet
currently produces no visual response at all.

Build particle prefabs into `Assets/04_VFX/`:

- **Rewind** — something that reads as time pulling backward. Converging, inward, cool.
- **Accelerate** — the opposite. Expanding, outward, warm.
- **Per-target change** — a brief flash at the object itself, confirming *this* is what changed.

Rewind and accelerate must be **distinguishable at a glance**, not just different.
A player should learn which direction they pressed from the effect alone.

Keep them short — these fire on a 0.25s cooldown, so anything over about a second
will overlap itself.

**Note:** `TimeGauntletController.PlayUseVFX` moves the effect to the target's
position before playing. Author the prefabs expecting to be relocated, not parented.

---

## Task 3 — The restoration moment

This is the emotional payoff of the entire slice, and right now it is one line of
text plus a fog blend.

Available hooks (MASTER §3): `PuzzleCondition.OnSolved`,
`ZoneUnlock.OnZoneUnlocked`, `RestorationController` has its own
`restorationVFX` slot — also empty.

Build:
- A restoration effect at the zone, not at the player
- Coordination with the atmosphere blend, which runs 2.5s by default
- Timing that lets the player *watch* — the dressing swap is instant today, which
  undercuts it

### ⛔ Ask before taking camera control
A brief held beat — camera pauses, hint fades in slower, atmosphere finishes
blending before control returns — would strengthen this considerably.

**But the brief explicitly excludes cutscenes.** A held moment is not a cutscene,
and that distinction is the user's to draw, not yours. **Propose it, describe
exactly what you would hold and for how long, and wait.** Agent 05 owns Task C3
on the same moment — coordinate rather than both reaching for the camera.

---

## Task 4 — Lighting, before and after

The payoff is carried largely by light, not particles.

- **Before:** overcast, desaturated, heavy fog
- **After:** warmer, clearer, fog thinner

Depends on Agent 03's geometry. Coordinate: they own the dressing arrays, you own
how those dressings are lit.

If you change the Directional Light's values at runtime, the same persistence
problem as Task 1 applies — **say so in your report** rather than discovering it
twice.

---

## Acceptance

- [ ] Fog decision made by the user, and implemented as decided
- [ ] Rewind and accelerate are distinguishable at a glance
- [ ] Every target change produces a visible confirmation at the object
- [ ] Effects do not overlap themselves at the 0.25s use cooldown
- [ ] The restoration moment registers as a payoff, not a state flip
- [ ] No runtime global state left mutated after Play mode stops — or the exceptions are named explicitly in the report
- [ ] **Observed in Play mode**, not just authored

---

## Ask, do not decide

**Ask:** the fog approach (Task 1) · any camera control during restoration (Task 3) ·
any package install · changing anything in MASTER §3.

**Decide alone:** particle shapes, colours, emission counts, curves, lighting
values, effect durations within the cooldown constraint.

---

## Report

Verdict · what you ran and saw in Play mode · files created, with paths · your fog
recommendation with reasoning · **what global state your work mutates, if any** ·
findings by severity · **anything in MASTER §2 that turned out to be wrong.**
