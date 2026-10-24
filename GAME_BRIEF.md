# BORROWED TIME: Echoes of Tomorrow — Game Brief

**Status:** APPROVED 2026-10-24 · Phase 1 closed
**Engine:** Unity 6.3.25f1 · URP 17.3 · Input System 1.20.0
**Target:** Windows PC
**Project root:** `E:\!for WORK\all for one`

## Approved gameplay revision — 2026-10-24

The user's newer instructions supersede the old discrete-spend/regen sections
below: one stored-time meter starts at **0**. **Hold E** to gradually age or break
an object and collect its time; **hold Q** to spend that time and gradually
rejuvenate/repair an object. Release pauses at the intermediate visible age;
reversing continues from that exact age. No passive regeneration.

Prototype tuning is 3 seconds per authored age interval with a short 0.25-second
endpoint pause. Transfer amount is proportional to age moved. Empty/full storage
limits the actual age movement, so time is never created or destroyed by caps.
Gating/puzzles require exact authored endpoints, not the previous completed index.

The scene has an explicitly labelled, reversible greybox time reserve yielding
75 units. This supplies the opening energy because the existing tree and bridge
start old/broken. Collect 75, restore tree for 15 and bridge for 40, leaving 20.
Greybox animation interpolates geometry transforms/colour and source-state
collision continuously, handing off to the next collider at the endpoint.
It is a mechanical animation prototype, not final tree/bridge artwork. Final
models still require user references and descriptions. No new package approved.

The sections below describe the original baseline where they conflict with this
revision. See `TIME_TRANSFER_REPORT_20261024.md` for implementation/verification.

> This file is the **single source of truth** for scope. It supersedes any
> earlier brief document. Where this file and a task package disagree, this
> file wins — and tell Claude, because the task package needs fixing.

---

## 1. Core Concept

A young conservationist wields a **Time Gauntlet** that rewinds or accelerates
the age of individual objects. Using it, they solve environmental puzzles and
restore a polluted world one zone at a time.

The gauntlet does **not** dilate time globally. It moves a single object between
discrete states of its own history.

---

## 2. MVP Target — Vertical Slice (5–8 minutes)

### Must-have
- Third-person movement and camera: WASD, Mouse look, Shift sprint, Space jump
- Time Gauntlet: **Rewind (Q)** and **Accelerate (E)** change object state
- Two object archetypes:
  - **Tree** — Young → Mature → Old
  - **Bridge** — Intact → Worn → Broken
- One environmental puzzle whose solution requires **fixing the root cause first**
- One zone unlock gated on that puzzle
- Visible before/after restoration payoff
- Tutorial and hint text only — no voice, no NPCs, no cutscenes
- Playable start to finish with no critical bugs

### Explicitly out of scope for MVP
- Large open world (semi-open is the long-term goal, not the slice)
- Time Transfer, Time Scan as mechanics
- NPCs, dialogue, voice acting, cutscenes
- Advanced physics simulation
- Multiple zones beyond the one unlock
- Save/load system
- Animation beyond what movement needs

---

## 3. Input Map (locked)

| Input | Action | Status |
|---|---|---|
| WASD | Move | Not yet implemented — Codex Task 02 |
| Mouse | Look | Not yet implemented — Codex Task 02 |
| Shift | Sprint | Not yet implemented — Codex Task 02 |
| Space | Jump | Not yet implemented — Codex Task 02 |
| **Q** | **Rewind** | ✅ Implemented |
| **E** | **Accelerate** | ✅ Implemented |
| F | Scan (optional) | Partial — shows a state readout |
| R | Undo (optional) | Explicit no-op, out of MVP scope |
| Mouse Left | Select | Not used — aim is centre-screen raycast |

---

## 4. The Puzzle as Built

Implemented in `MainScene` as greybox. This is the concrete instance of
"fix the root cause first".

```
Tree_Target  at (-4, 0, 6)   starts Old       cost 15/change
   │
   │ prerequisite gate: Tree must be Mature (index 1)
   ▼
Bridge_Target at (3, 0, 10)  starts Broken    cost 20/change
```

**Solution path:** Tree Old → Mature (Q, 15 energy), then Bridge Broken → Worn →
Intact (Q×2, 40 energy). Total 55 of a 100 pool.

**Gated hint if the player tries the bridge first:**
"The bridge's timber came from that tree. Restore the tree first."

**On solve** (`puzzle_01_bridge`): `ZoneUnlock` opens zone 1 and
`RestorationController` swaps the dressing and blends the fog.

---

## 5. Architecture as Implemented

```
BorrowedTimeInput  ──Q/E──▶  TimeGauntletController
                                   │ raycast from Camera.main, centre-screen
                                   ▼
                              TimeTarget  ──OnStateChanged──▶  PuzzleCondition
                                   ▲                                 │ OnSolved
                          prerequisite gate                          ├──▶ ZoneUnlock
                                                                     └──▶ RestorationController
                             EnergyManager ◀── TrySpend
                                   │ OnEnergyChanged
                                   ▼
                              HUDDisplay ◀── OnTargetChanged, OnProgressChanged
```

### File ownership
| Folder | Owner | Contents |
|---|---|---|
| `Assets/01_GameSystems/` | Claude | TimeTarget, TimeGauntletController, EnergyManager, PuzzleCondition, ZoneUnlock, TimeStateDefinition |
| `Assets/02_SupportSystems/` | Claude + Codex | GameState, BorrowedTimeInput, HintTextManager, HintTrigger · **Player scripts go here (Codex)** |
| `Assets/03_EnvironmentArt/` | Claude via Blender MCP | Models (empty — greybox stands in) |
| `Assets/04_VFX/` | Claude | RestorationController |
| `Assets/05_UI/` | Claude | HUDDisplay |
| `Assets/Editor/` | Claude | BorrowedTimeSceneSetup (greybox builder, re-runnable) |
| `Assets/Scenes/MainScene.unity` | **Claude only** | Scene wiring |
| `Assets/Prefabs_Shared/` | **Claude only** | Prefabs |

**Rule:** Claude owns all scene and prefab edits. Codex delivers scripts plus
wiring instructions. Concurrent edits to scene files clobber silently.

---

## 6. Design Decisions Already Taken

Recorded so nobody re-litigates them by accident. Each is open to challenge
**with reasoning** — none is sacred.

| Decision | Chosen | Rationale | Open? |
|---|---|---|---|
| State model | Discrete, index-ordered. 0 = earliest. | Legible, testable, cheap. Tree and Bridge share machinery. | Settled |
| Root-cause gating | `TimeTarget.prerequisite` + required state index | Makes "fix the cause first" readable without dialogue | Settled |
| Energy | Discrete cost per change | Easier to balance than continuous drain | Settled |
| Energy regen | Slow passive regen, 4/sec after 1.5s delay | Anti-softlock insurance | **Under review (Codex Task 01)** |
| Fog on restore | Writes global `RenderSettings` | Simplest path to a visible payoff | **Under review — persists into editor after play** |
| Gated hint | Author-written string per target | Designer control over wording | **Under review** |
| Time scale | Never modified | The gauntlet ages objects, it does not slow the world | Settled |
| Input backend | `Keyboard.current` read directly | No generated action class exists yet | Open — see Codex Task 02 Q3 |

---

## 7. Current State (2026-10-24)

**Done:**
- 12 system scripts, compiling clean
- `MainScene` with 13 objects: systems, Ground, Directional Light, Tree_Target, Bridge_Target
- Greybox puzzle wired and solvable in principle
- Git initialised

**Not done:**
- **No player, no camera** — `Camera.main` is null, so the gauntlet cannot aim and the puzzle is unreachable in play mode
- Canvas has no UI children, so `HUDDisplay` has nothing to drive
- No Blender models; greybox cubes stand in
- No test *assembly definition* — but see Section 9: the Test Framework package **is** installed

**Blocking:** the player controller. Everything else is reachable once it exists.

---

## 9. Package Inventory (verified from `Packages/manifest.json`, 2026-10-24)

Read from the file, not assumed. Relevant entries:

| Package | Version | Note |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.3.0 | URP confirmed |
| `com.unity.inputsystem` | 1.20.0 | New Input System confirmed |
| `com.unity.ugui` | 2.0.0 | Includes TextMeshPro in Unity 6 |
| **`com.unity.test-framework`** | **1.6.0** | **Installed.** An asmdef is all that's missing to write tests |
| `com.unity.ai.navigation` | 2.0.14 | Present, unused |
| `com.unity.timeline` | 1.8.13 | Present, unused |
| `com.coplaydev.unity-mcp` | git#main | The MCP bridge |

| **`com.unity.cinemachine`** | **3.1.7** | **Installed 2026-10-24** with the user's explicit approval, for the third-person camera |

⚠️ **Cinemachine 3.x is not 2.x.** The API was renamed: `CinemachineVirtualCamera`
→ `CinemachineCamera`, `CinemachineFreeLook` → `CinemachineCamera` + an orbital
follow component, and the `Cinemachine` namespace moved to `Unity.Cinemachine`.
Code or tutorials written for 2.x will not compile. Check the installed package's
own documentation rather than recalling 2.x patterns.

No further packages are approved. Anything else still needs the user's consent.

---

## 8. Known Assumptions

- Unity MCP v10.3 is reachable and working — verified
- Blender 5.2 FBX export works — not yet exercised
- Energy and state balance need playtesting; current numbers are first guesses
- The puzzle must read unmistakably as "fix the cause first" — if playtesting
  shows players brute-force the bridge instead, the gating needs louder feedback,
  not a different structure
