# MASTER BUILD PROMPT — BORROWED TIME: Echoes of Tomorrow

**Target:** a playable vertical slice. Gameplay complete, map complete, UI/UX
complete, nothing left but polish.

**Project root:** `E:\!for WORK\all for one`
**Engine:** Unity 6.3.25f1 · URP 17.3 · Input System 1.20.0 · Cinemachine 3.1.7
**Issued:** 2026-10-24

> Gameplay amendment: the user subsequently approved held Q/E with continuous
> age animation, one meter starting at zero, E collecting time and Q spending it,
> and no passive regeneration. GAME_BRIEF.md's approved gameplay revision is the
> current authority; this document's older regen/discrete-spend instructions and
> example energy totals are historical. Models remain blocked on user references.

---

## 0. READ THIS BEFORE ANYTHING ELSE

### What this document is
One self-contained brief that takes the project from its current state to a
playable slice. You do not need any other document. Everything you need —
current state, architecture, contracts, acceptance criteria — is here.

### Three rules that override everything below

**Rule 1 — Verify, never assume.**
Every factual claim in Section 2 was verified on 2026-10-24. Re-verify anything
you depend on. If this document and the project disagree, **the project wins and
you say so.** Do not build on a stale claim.

**Rule 2 — Ask, do not decide, when it is genuinely ambiguous.**
Section 9 lists what you may decide alone and what you must ask about. When in
doubt, ask. A half-done task with three clear questions beats a complete task
built on three guesses. But do not ask about things Section 9 says are yours to
call — that wastes the user's attention.

**Rule 3 — Never claim a test passed that you did not run.**
State exactly what you ran and what you observed. "NOT RUN" is an acceptable
answer. A false pass is the worst outcome in this project, because it hides a
bug behind a report.

### What cannot be finished by prompt alone
Be honest with the user about these rather than claiming completion:
- **Play testing.** Nobody knows if the slice is fun until a human plays it.
- **Balance.** Energy costs, move speed, camera distance — these need felt
  feedback, not reasoning.
- **Art judgment.** Whether a model looks right is a thing seen, not derived.

Your job is to deliver a build where those three are the *only* things left.

---

## 1. Definition of Done

The slice is done when a player can, without touching the editor:

1. Press Play, click to lock the cursor, and move with WASD / Shift / Space.
2. See a HUD: energy, the name and state of whatever they are aiming at, and
   puzzle progress.
3. Read a tutorial hint that teaches Q and E without reading a manual.
4. Aim at the withered tree, press Q, and watch it become mature. Energy drops
   visibly.
5. Aim at the broken bridge *first* and be refused with a readable reason that
   points them at the tree.
6. After fixing the tree, restore the bridge and cross it.
7. Watch the zone restore: dressing swaps, atmosphere shifts, a line of text
   marks the moment.
8. Walk into the newly opened zone.
9. Do all of the above in 5–8 minutes with no errors in the console and no
   way to softlock.

Anything beyond this list is polish. Anything on this list is not done until it
works in Play mode.

---

## 2. Current State — verified 2026-10-24

### Scripts, compiling clean

| Path | Role |
|---|---|
| `Assets/01_GameSystems/TimeStateDefinition.cs` | Per-state data (name, visual root, flags, collider) |
| `Assets/01_GameSystems/TimeTarget.cs` | Discrete state machine + root-cause gating |
| `Assets/01_GameSystems/TimeGauntletController.cs` | Centre-screen raycast, energy spend, use arbitration |
| `Assets/01_GameSystems/EnergyManager.cs` | Energy pool + passive regen |
| `Assets/01_GameSystems/PuzzleCondition.cs` | Requirement watcher, fires once |
| `Assets/01_GameSystems/ZoneUnlock.cs` | Gate: removes barriers, reveals content |
| `Assets/02_SupportSystems/GameState.cs` | Progression record (zones, puzzles) |
| `Assets/02_SupportSystems/BorrowedTimeInput.cs` | Q/E/F/R → gauntlet |
| `Assets/02_SupportSystems/HintTextManager.cs` | Single-line narrator with fade |
| `Assets/02_SupportSystems/HintTrigger.cs` | Trigger volume → hint |
| `Assets/02_SupportSystems/PlayerController.cs` | CharacterController locomotion |
| `Assets/02_SupportSystems/PlayerCameraRig.cs` | Mouse → Cinemachine orbital axes, cursor lock |
| `Assets/04_VFX/RestorationController.cs` | Before/after dressing swap + fog blend |
| `Assets/05_UI/HUDDisplay.cs` | Energy / target / progress readout |
| `Assets/Editor/BorrowedTimeSceneSetup.cs` | Re-runnable greybox puzzle builder |

### Scene: `Assets/Scenes/MainScene.unity`

| Object | Components | State |
|---|---|---|
| `GameState` | GameState | ✅ |
| `EnergyManager` | EnergyManager | ✅ |
| `TimeGauntletController` | TimeGauntletController | ✅ aimCamera + targetMask wired |
| `InputManager` | BorrowedTimeInput | ✅ |
| `Canvas` | Canvas, HUDDisplay, HintTextManager | ⚠️ **no UI children — every field empty** |
| `EnvironmentPuzzle` | PuzzleCondition (`puzzle_01_bridge`) | ✅ 2 requirements wired |
| `Zone` | ZoneUnlock (zone 1) | ⚠️ no barriers, no content assigned |
| `RestorationController` | RestorationController | ⚠️ no dressing assigned |
| `Tree_Target` | TimeTarget + 3 greybox children | ✅ starts Old, cost 15 |
| `Bridge_Target` | TimeTarget + 3 greybox children | ✅ starts Broken, cost 20, gated on Tree=Mature |
| `Player` | CharacterController, PlayerController, CameraTarget, Visual capsule | ✅ tag=Player, layer=Ignore Raycast |
| `Main Camera` | Camera, AudioListener, CinemachineBrain | ✅ tag=MainCamera, `Camera.main` resolves |
| `PlayerCamera` | CinemachineCamera, OrbitalFollow, RotationComposer, Deoccluder, PlayerCameraRig | ✅ targets → CameraTarget |
| `Ground` | Plane, MeshCollider, scale (3,1,3) | ✅ |
| `Directional Light` | Light | ✅ |

### Packages — read from `Packages/manifest.json`

Installed: URP 17.3.0 · Input System 1.20.0 · **Cinemachine 3.1.7** · uGUI 2.0.0
(includes TextMeshPro) · **Test Framework 1.6.0** · AI Navigation 2.0.14 ·
Timeline 1.8.13 · unity-mcp (git).

**No further package installs are approved.** Anything else needs the user's consent.

⚠️ **Cinemachine 3.x is not 2.x.** `CinemachineVirtualCamera` → `CinemachineCamera`;
namespace is `Unity.Cinemachine`; and **`Follow`/`LookAt` are not serialized
directly** — they live in a `Target` struct, reachable as `Target.TrackingTarget`
and `Target.LookAtTarget`. Code written for 2.x will not compile.

### What is NOT done

1. **Nothing has been play-tested.** Compile, static review and scene wiring only. Movement, jumping, camera collision and the full puzzle loop have never been observed running.
2. **The Canvas is empty.** `HUDDisplay` and `HintTextManager` have no Text, Image or CanvasGroup to drive. Nothing appears on screen.
3. **No map.** One flat `Ground` plane and two greybox objects.
4. **No real models.** Coloured cubes stand in for tree and bridge.
5. **`ZoneUnlock` and `RestorationController` have empty object arrays** — the gate opens nothing and the restoration swaps nothing.
6. **No tutorial.** No `HintTrigger` exists in the scene.
7. **No audio.** `AudioCueManager` was specified in planning and never written.
8. Three design decisions remain under review — see Section 9.

### Git
Initialised, with an initial commit. **Commit before starting each track.**

---

## 3. Architecture and Contracts

```
BorrowedTimeInput  ──Q/E──▶  TimeGauntletController
                                   │ raycast: Camera.main, viewport (0.5, 0.5)
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

### Public surface you may rely on — do not change these signatures

```csharp
// TimeTarget
public int CurrentStateIndex { get; }
public int StateCount { get; }
public string CurrentStateName { get; }
public string displayName;
public bool IsHealthy { get; }
public bool IsTraversable { get; }
public bool IsUnlocked();
public string GetGatedHint();
public bool CanRewind();
public bool CanAccelerate();
public bool Rewind();
public bool Accelerate();
public bool SetState(int index);
public event Action<TimeTarget, int, int> OnStateChanged;   // (target, previous, current)

// TimeGauntletController
public static TimeGauntletController Instance { get; }
public TimeTarget CurrentTarget { get; }
public void RequestRewind();
public void RequestAccelerate();
public event Action<TimeTarget> OnTargetChanged;            // null when nothing aimed at
public event Action<TimeTarget, int> OnGauntletUsed;        // (target, -1 rewind | +1 accelerate)
public event Action<string> OnUseRejected;                  // human-readable reason

// EnergyManager
public static EnergyManager Instance { get; }
public float CurrentEnergy { get; }
public float MaxEnergy { get; }
public float Normalized { get; }
public bool HasEnergy(float amount);
public bool TrySpend(float amount);
public void Refill(float amount);
public void RefillFull();
public event Action<float, float> OnEnergyChanged;          // (current, max)
public event Action OnInsufficientEnergy;

// PuzzleCondition
public string PuzzleId { get; }
public bool IsSolved { get; }
public int RequirementCount { get; }
public float GetProgressNormalized();
public event Action<PuzzleCondition> OnSolved;
public event Action<int, int> OnProgressChanged;            // (matched, total)

// ZoneUnlock
public bool IsUnlocked { get; }
public int ZoneId { get; }
public void Unlock();
public event Action<int> OnZoneUnlocked;

// GameState
public static GameState Instance { get; }
public void UnlockZone(int id);
public void RestoreZone(int id);
public void MarkPuzzleSolved(string id);
public bool IsZoneUnlocked(int id);
public bool IsZoneRestored(int id);
public bool IsPuzzleSolved(string id);

// HintTextManager
public static HintTextManager Instance { get; }
public void ShowHint(string message, float duration = -1f);
public void ClearHint();
```

### Hard invariants — breaking any of these breaks a working system

1. **`Camera.main` must be the player's camera.** The gauntlet raycasts from it. It needs the `MainCamera` tag and a `CinemachineBrain`.
2. **The player must be tagged `Player`.** `HintTrigger` filters on it.
3. **The player's whole subtree stays on layer `Ignore Raycast` (2),** and the gauntlet's `targetMask` must exclude layer 2. Otherwise the player's own body blocks every aim ray.
4. **Traversal is collider-driven, not code-driven.** `TimeTarget` enables exactly one state's collider. Do not query `IsTraversable` from movement code.
5. **Nothing modifies `Time.timeScale`.** The gauntlet ages objects; it does not dilate time.
6. **Claude owns scene and prefab files.** An agent without Unity MCP delivers scripts plus wiring instructions; it does not edit `MainScene.unity` or anything in `Prefabs_Shared/`. Concurrent scene edits clobber silently.

---

## 4. Role Routing — who does what

Check your own tools first and say what you actually have.

| If you have | You own |
|---|---|
| Unity MCP | Scene wiring, prefabs, component setup, Play-mode checks, package queries |
| Blender MCP | Model authoring and FBX export |
| Only file read/write | Scripts, plus written wiring instructions for whoever has Unity MCP |

**If several agents work in parallel:** one agent owns the scene. Everyone else
delivers scripts and instructions. Commit to git between handoffs.

---

## 5. TRACK A — Gameplay

**Goal:** the full loop works in Play mode. This track gates the other two.

### A1. Play-test what already exists ← **start here, before writing anything**

Nothing in this project has ever run. Find out what is actually broken before
adding to it.

1. Enter Play mode. Click to lock the cursor.
2. WASD, Shift, Space. Does the character move, sprint, jump? Does it fall through `Ground`?
3. Orbit the camera 360° at several pitches. Does it flip? Clip through the ground or the greyboxes?
4. Aim at `Tree_Target` (-4, 0, 6). Press Q. Expect: Old → Mature, energy 100 → 85.
5. Aim at `Bridge_Target` (3, 0, 10). Press Q twice. Expect: Broken → Worn → Intact, energy 85 → 45.
6. Expect `puzzle_01_bridge` to fire, the zone to unlock, the fog to blend.
7. Reload. Aim at the bridge **first**. Expect refusal with the gated hint.
8. Walk onto the bridge while it is Intact, then aim down at it and press E twice to break it under your own feet. **Report exactly what happens.** This is the case the design is most likely to get wrong.

**Report each step as PASS / FAIL / NOT RUN with what you saw.** Fix what fails
before continuing. Expect several failures — this is the first run.

### A2. Make the gate legible
A player who aims at the bridge first must understand *why* they were refused.
`OnUseRejected` already carries the reason; nothing is listening. Wire it to
`HintTextManager` (Track C builds the display; until then, `Debug.Log` is fine
and honest).

### A3. Audio cues
`AudioCueManager` was planned and never written. Write it:
- Subscribe to `OnGauntletUsed`, `OnUseRejected`, `OnInsufficientEnergy`, `PuzzleCondition.OnSolved`, `ZoneUnlock.OnZoneUnlocked`.
- One `AudioSource`, one clip per event, every field optional and null-guarded.
- **Do not source audio files.** Expose `AudioClip` slots and leave them empty; the user fills them or they stay silent. A silent slice is acceptable; a crash is not.

### A4. Softlock audit
The player must never reach an unwinnable state.
- Total cost to solve is 55 of a 100 pool. Confirm that is still true.
- Confirm regen cannot be outrun: with regen on, can a player strand themselves by spending on something irrelevant? There is nothing else to spend on today — but say so rather than assuming it holds.
- Confirm `fallResetY` recovery does not reset energy or puzzle state (by design, it must not).

### A5. Acceptance
- [ ] Every step of A1 passes, observed in Play mode
- [ ] Breaking the bridge underfoot produces sane behaviour, described in the report
- [ ] Rejection reasons reach the player (text or log)
- [ ] `AudioCueManager` exists, compiles, and is silent-safe with empty slots
- [ ] No console errors across a full playthrough
- [ ] No reachable softlock

---

## 6. TRACK B — Map and Environment

**Goal:** a space worth walking through, that teaches the puzzle by its layout.

**Depends on:** Track A1 (do not dress a broken loop).

### B1. Layout
One flat plane is not a level. Build a small, readable space:
- A start area with clear sightlines to the withered tree.
- A ravine or gap the bridge spans — **the obstacle must be visible before the bridge is usable,** so the player wants it fixed.
- The locked zone on the far side, visibly present but unreachable.
- Soft boundaries (terrain, rocks, water) rather than invisible walls.

Scale guidance: the whole slice is 5–8 minutes. Walking end to end should take
under a minute. Resist sprawl.

### B2. Assign the empty arrays
`ZoneUnlock` and `RestorationController` currently reference nothing:
- `ZoneUnlock.barriersToRemove` → whatever blocks the far side
- `ZoneUnlock.contentToReveal` → the far-side content (starts disabled)
- `RestorationController.pollutedDressing` → dead props, debris, haze
- `RestorationController.restoredDressing` → greenery, clear water

Both dressing sets want to be siblings under one parent so the swap is one
`SetActive` each.

### B3. Models
Greybox is the current state and greybox is shippable. Real models are an
upgrade, not a requirement.

If you have Blender MCP:
- Tree in 3 states: young sapling, mature canopy, dead and bare
- Bridge in 3 states: intact, worn with gaps, collapsed
- Low-poly stylised, flat colour, no textures needed
- Export FBX to `Assets/03_EnvironmentArt/Models/`
- **Swap them in as children of the existing `TimeTarget` objects.** The state wiring survives; only `TimeStateDefinition.visualRoot` and `stateCollider` get repointed.

**Do not model in Unity.** Blender owns authored geometry; primitives are only
for greybox.

### B4. Lighting and atmosphere
The restoration payoff is carried largely by light. Before: overcast, desaturated,
heavy fog. After: warmer, clearer, fog thinner. `RestorationController` already
blends fog — see the caveat in Section 9 Q2 before relying on it.

### B5. Acceptance
- [ ] The gap and the far side are visible from the start area
- [ ] A player who has never seen the game understands the bridge is broken
- [ ] No invisible walls where geometry would read better
- [ ] `ZoneUnlock` and `RestorationController` arrays are populated
- [ ] Before/after is obvious in a side-by-side screenshot
- [ ] Walking end to end takes under a minute
- [ ] No fall-through, no stuck geometry, observed in Play mode

---

## 7. TRACK C — UI / UX

**Goal:** the player understands the game without being told by a human.

**Depends on:** Track A1 (the HUD must reflect systems that actually work).

### C1. Build the Canvas
`HUDDisplay` and `HintTextManager` are written and wired but have **no children
to drive.** Every serialized field is empty. Build them:

`HUDDisplay` expects:
- `energyFill` — an `Image` set to Filled, Horizontal
- `energyLabel` — `TextMeshProUGUI`
- `targetPanel` — a `GameObject` shown only while aiming at something
- `targetNameLabel`, `targetStateLabel` — `TextMeshProUGUI`
- `puzzleProgressLabel` — `TextMeshProUGUI`
- `crosshair` — any `Graphic`; it recolours by state (idle / active / gated)

`HintTextManager` expects:
- `hintLabel` — `TextMeshProUGUI`
- `hintGroup` — `CanvasGroup` on the hint's parent, starting at alpha 0

**Every field is optional and null-guarded** — a partially built Canvas still
runs. Build it in pieces and test as you go.

Canvas setup: Scale With Screen Size, 1920×1080 reference, Match 0.5. Test at
16:9 and 16:10.

### C2. Teach Q and E without a manual
The brief forbids NPCs and voice. Text is all there is. Place `HintTrigger`
volumes:
1. Near spawn: name the gauntlet and the aim-at-something action.
2. Approaching the tree: teach Q as "wind back".
3. First approach to the bridge: let the gated refusal land as the lesson.

Keep each line under ~12 words. Diegetic beats instructional: *"The tree remembers
being younger"* over *"Press Q to rewind"* — but clarity wins over flavour if
testing shows players are lost.

### C3. The restoration moment
This is the emotional payoff. Right now it is one line of text and a fog blend.
Consider — and propose before building — a brief non-interactive beat: hold the
camera, fade the hint in slower, let the atmosphere finish blending before
returning control. **Do not build a cutscene;** the brief excludes them. A held
moment is not a cutscene, but ask before taking liberties with camera control.

### C4. Pause and cursor
`PlayerCameraRig` currently unlocks the cursor on Escape and relocks on left
click. There is no menu. Decide with the user (Section 9 Q5) whether Escape
should open a pause panel — Resume / Restart / Quit — and whether Q/E should be
suppressed while unlocked. Today they are not.

### C5. Acceptance
- [ ] Energy visibly drops on every gauntlet use
- [ ] Aiming at a target shows its name and state; the crosshair changes colour; a gated target reads differently from a usable one
- [ ] Puzzle progress is visible
- [ ] Rejection reasons appear on screen, not only in the console
- [ ] A first-time player discovers Q and E without being told
- [ ] The restoration moment registers as a payoff
- [ ] Layout holds at 16:9 and 16:10
- [ ] No `NullReferenceException` from a partially assigned Canvas

---

## 8. Order of Work and Handoffs

```
         ┌─────────────────────────────────────────┐
         │  A1: play-test what exists  ← FIRST     │
         │  nothing else starts until this reports  │
         └────────────────┬────────────────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        ▼                 ▼                 ▼
   A2–A4 gameplay    B1–B2 layout      C1 Canvas
        │                 │                 │
        │                 ▼                 ▼
        │            B3 models         C2 tutorial
        │                 │                 │
        └────────┬────────┴────────┬────────┘
                 ▼                 ▼
            B4 lighting      C3 restoration beat
                 │                 │
                 └────────┬────────┘
                          ▼
                 Full playthrough test
                          ▼
                   A5 softlock audit
                          ▼
                   🎮 Playable slice
```

**Commit to git at every arrow.** If more than one agent is working, the scene
owner commits before handing off.

---

## 9. Decide vs Ask

### Decide alone — do not ask
- Numeric tuning values exposed in the Inspector (speeds, damping, fade times)
- Layout geometry, prop placement, colour choices
- Script internals that do not change the public surface in Section 3
- Hint wording
- Canvas hierarchy and anchoring
- Which greybox primitive stands in for what

### Ask first — do not decide

**Q1 — Energy regen.** Passive regen (4/sec after a 1.5s delay) exists purely as
softlock insurance. It may be undercutting the resource tension the brief wants.
The alternative: remove regen, and guarantee instead that a full pool always
affords every required change. **Recommend one, with reasoning, and wait.**

**Q2 — Fog on restoration.** `RestorationController` writes global
`RenderSettings.fogColor` and `fogDensity`. That mutates project-wide render
state **and persists into the editor after Play mode stops** — the scene stays
restored-looking until someone changes it back. Options: capture and restore on
disable, move to a URP Volume override, or accept it. **This is the single most
likely thing to cause confusion later.** Recommend and wait.

**Q3 — Gated-hint granularity.** Each `TimeTarget` carries a designer-written
rejection string. Alternative: derive the reason from the prerequisite's actual
state, so it cannot go stale when the puzzle changes. Recommend and wait.

**Q4 — Input backend.** Everything reads `Keyboard.current` directly because no
generated action class exists. `InputSystem_Actions.inputactions` sits unused in
the project. Generating it properly is cleaner and touches an asset Claude owns.
Recommend and wait.

**Q5 — Escape and pause.** Does Escape open a menu, or only free the cursor?
Should Q/E be suppressed while the cursor is free? Recommend and wait.

**Also ask before:** installing any package · changing anything in Section 3's
public surface · adding a mechanic not in the brief · anything that would make
the slice longer than 8 minutes.

---

## 10. Reporting

After each track, report:

1. **Verdict** — COMPLETE / PARTIAL / BLOCKED
2. **What you actually ran**, and what you observed. Mark NOT RUN honestly.
3. **Files created or changed**, with paths
4. **Scene wiring**, either done or as instructions for whoever owns the scene
5. **Findings** ranked by severity, with file and line where you have it
6. **Questions blocking you**
7. **Anything in Section 2 that turned out to be wrong** — this matters more than
   it sounds. A stale fact in this document will mislead the next agent.

---

## 11. Polish Backlog — explicitly out of scope

Do not build these. Listed so nobody mistakes them for gaps:

- Save/load · multiple zones · Time Transfer / Time Scan · NPCs, dialogue, voice
- Character animation beyond locomotion · footstep audio · music
- Settings menu, key rebinding, accessibility options
- Particle polish beyond the restoration beat
- Performance optimisation (the slice is far too small to need it)
- Builds for any platform other than the editor

When Sections 5–7 all pass, the slice is done, and these become the next
conversation — not this one.
