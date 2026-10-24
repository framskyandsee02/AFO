# CODEX TASK 01 — Review: Core Systems Pass 1

**From:** Claude (Team Lead)
**To:** Codex (Implementation Assistant & Independent Reviewer)
**Project:** BORROWED TIME: Echoes of Tomorrow
**Project root:** `E:\!for WORK\all for one`
**Date issued:** 2026-10-24

---

## 0. Before you start

Read these from the project, do not take my word for them:
- `MultiAgent_GameDev_Prompt_Pack/MultiAgent_GameDev_Prompt_Pack/MASTER_RULES.md`
- `MultiAgent_GameDev_Prompt_Pack/MultiAgent_GameDev_Prompt_Pack/CODEX_IMPLEMENTATION_REVIEWER.md`
- `README_BORROWED_TIME-1.md` (approved Game Brief)

Then confirm, from what you can actually observe:
1. Can you read/write `E:\!for WORK\all for one`? Say so explicitly. Do **not** assume we share a filesystem or session.
2. Can you see a Unity Editor instance / run a compile? If not, say so — review statically and mark compile checks as NOT RUN.
3. Is git initialised in the project root? (Expected: **no**, as of this writing. `init_git.ps1` sits unrun in the root.)

If you cannot reach the project, stop and tell me what you need.

---

## 1. Objective

Independent review of the first implementation pass of the **core game systems** —
everything except the player controller.

**Non-goals for this task:**
- Do not implement the player controller, camera, or movement.
- Do not create 3D models (Blender belongs to Agent 03).
- Do not restructure the folder layout or rename the public API.
- Do not build UI prefabs (Agent 05).

---

## 2. Files in scope (review only, do not edit without my go-ahead)

| Path | Role |
|---|---|
| `Assets/01_GameSystems/TimeStateDefinition.cs` | Per-state data |
| `Assets/01_GameSystems/TimeTarget.cs` | Core state machine + root-cause gating |
| `Assets/01_GameSystems/TimeGauntletController.cs` | Aim raycast, energy spend, use arbitration |
| `Assets/01_GameSystems/EnergyManager.cs` | Energy pool + regen |
| `Assets/01_GameSystems/PuzzleCondition.cs` | Requirement watcher |
| `Assets/01_GameSystems/ZoneUnlock.cs` | Gate / zone reveal |
| `Assets/02_SupportSystems/GameState.cs` | Progression record |
| `Assets/02_SupportSystems/BorrowedTimeInput.cs` | Keyboard → gauntlet |
| `Assets/02_SupportSystems/HintTextManager.cs` | Single-line narrator |
| `Assets/02_SupportSystems/HintTrigger.cs` | Trigger volume → hint |
| `Assets/04_VFX/RestorationController.cs` | Before/after payoff + fog blend |
| `Assets/05_UI/HUDDisplay.cs` | Energy / target / progress readout |

**Do not touch:** `Assets/Scenes/MainScene.unity`, anything under `Assets/Prefabs_Shared/`,
`Assets/Settings/`, `Assets/InputSystem_Actions.inputactions`. I own scene and prefab wiring
via Unity MCP; concurrent edits there will silently clobber.

---

## 3. Architecture you are reviewing against

```
BorrowedTimeInput  ──Q/E──▶  TimeGauntletController
                                   │ raycast from camera
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

**Design intent, so you can judge whether the code serves it:**
- States are **discrete and index-ordered**. Index 0 = earliest in time. Rewind decrements, Accelerate increments. Tree (Young/Mature/Old) and Bridge (Intact/Worn/Broken) reuse the same machinery with different `isHealthy` / `isTraversable` flags.
- **Root-cause gating is the puzzle.** `TimeTarget.prerequisite` + `prerequisiteRequiredState` make a target inert until its cause is fixed. This is what makes "fix the source first" legible without dialogue.
- Energy is a **discrete cost per state change**, with slow passive regen specifically so a player cannot hard-lock a puzzle by spending the pool. This was my call, not the brief's — flag it if you disagree.
- Nothing in these systems knows about the player. The gauntlet aims from `Camera.main` by default.

---

## 4. What I want reviewed, in priority order

### P0 — Correctness and crash safety
- Null handling on every `SerializeField` that a designer could leave empty. I believe every one is guarded; prove me wrong.
- Event subscribe/unsubscribe symmetry (`OnEnable`/`OnDisable`). Any leak or double-subscribe?
- `TimeGauntletController.RequestUse()`: the energy is spent *before* `Rewind()`/`Accelerate()` is called, with a refund if the change is refused. Is there any path where energy is spent and no refund occurs?
- `PuzzleCondition.Evaluate()` runs on every target's `OnStateChanged`. With `latchWhenSolved = true`, is there a path where `OnSolved` fires twice?
- `ZoneUnlock.Start()` and `RestorationController.Start()` both re-check `IsSolved` for load-order safety. Can that double-fire with the event path?
- Singleton `Awake()` guards: all four (`EnergyManager`, `TimeGauntletController`, `GameState`, `HintTextManager`) destroy the duplicate GameObject, not just the component. Is destroying the whole GameObject correct here, or should it be `Destroy(this)`?

### P1 — Design risk
- `RestorationController` writes to global `RenderSettings` (fog colour/density). This mutates project-wide render state and **persists into the editor after play mode stops**. Flag whether this should be captured/restored, or moved to a Volume override. I consider this the single most likely thing to annoy us later.
- Is `Time.unscaledTime` the right clock for cooldowns and hint timers, given the brief has no time-dilation of actual gameplay? I used unscaled deliberately; sanity-check it.
- `TimeTarget.ApplyStateVisuals()` toggles `GameObject.SetActive` per state. For 3 states that is fine; say so if you see a scaling problem.

### P2 — API and integration
- Will Agent 05's HUD and Agent 04's VFX have everything they need from the public surface? Any event or getter obviously missing?
- `BorrowedTimeInput` reads `Keyboard.current` directly instead of using the generated action class from `InputSystem_Actions.inputactions`. That is a deliberate stopgap so nothing depends on a generated file that does not exist yet. Confirm the swap later will be contained to `ReadInput()`.
- Naming, comment accuracy, anything that reads as untrue about what the code does.

### P3 — Tests
Propose (do not yet write) a minimal EditMode test plan for:
- `TimeTarget` boundary behaviour at index 0 and last index
- Prerequisite gating blocking a change
- `EnergyManager.TrySpend` refusal path
- `PuzzleCondition` firing exactly once

Tell me whether the project has a test assembly set up. I have not checked and have not installed one — **do not install packages.**

---

## 5. Known gaps (do not report these as findings)

- No player controller, camera rig, or movement. Out of scope by instruction.
- `F` (Scan) shows a state readout; `R` (Undo) is an explicit no-op. Both optional in the brief.
- No save system. Out of MVP scope.
- `MainScene` components still need re-attaching after the file rename — I am handling that.
- No Blender models yet; `TimeStateDefinition.visualRoot` slots are empty.
- No test assembly.

---

## 6. Constraints

- **Do not** install Unity packages.
- **Do not** edit scene or prefab files.
- **Do not** change public API signatures without flagging to me first — Agents 04 and 05 are building against them.
- **Do not** claim a test or compile passed unless you ran it and read the result.
- If you want to change anything, propose the patch and the reasoning; I integrate.

---

## 7. Report format (per `CODEX_IMPLEMENTATION_REVIEWER.md`)

1. Verdict: APPROVE / APPROVE WITH NOTES / BLOCK
2. Scope reviewed
3. Files changed (expected: none)
4. Findings ranked by severity, with file/line references
5. Tests run and exact results
6. Tests not run / limitations
7. Recommended fixes
8. Any decision requiring user approval

---

## 8. Open questions I actually want your opinion on

1. **Fog mutation** — capture-and-restore, or move to a URP Volume? Your call carries weight; I have not committed either way.
2. **Energy regen** — I added passive regen purely as softlock insurance. Does that undercut the resource tension the brief wants, and should the anti-softlock instead be "the puzzle's required changes are always affordable from a full pool"?
3. **Gating feedback** — right now a gated target reports a designer-authored string. Is a string per target the right granularity, or should gating reasons be derived from the prerequisite's actual state?
