# BORROWED TIME — Greybox simulation handoff (2026-10-24)

## Verdict: PARTIAL

The integrated greybox simulation is saved in `Assets/Scenes/MainScene.unity`.
Gameplay systems, map, HUD/tutorial, silent audio routing and restoration feedback
are implemented. Full manual acceptance is NOT claimed: see the remaining checks.
No final models were authored. The user requested simulation first; modelling
still requires their references and descriptions.

## Decisions authorized in this chat

- Codex owns the scene for this pass.
- Keep energy regeneration (4/sec after the existing spend delay).
- Capture and restore fog on disable; retain authored prerequisite hints.
- Keep the previously approved direct keyboard input and cursor-only Escape.
- Do not add a pause panel, camera hold/cutscene, audio downloads or packages.
- Use greybox geometry while waiting for model references.

## What changed

### Gameplay / support

- Repaired four stale script GUIDs in MainScene: GameState,
  TimeGauntletController, BorrowedTimeInput and HUDDisplay. The scene referenced
  obsolete GUIDs despite the corresponding C# classes compiling. Existing
  serialized component values were retained.
- HintTextManager now subscribes to gauntlet rejection messages, displays them
  for four seconds and logs the actual reason. It handles singleton initialization
  order and unsubscribes on disable.
- Added `Assets/02_SupportSystems/AudioCueManager.cs` with optional rewind,
  accelerate, rejection, insufficient-energy, puzzle-solved and zone-unlocked
  clips. All slots remain empty by design. Insufficient energy avoids playing
  the generic rejection sound a second time for the same refused use.

### Map / scene

- Added an explicitly named `BT_Slice_Greybox` hierarchy with two banks and a
  six-metre ravine at the existing bridge coordinates. Kept tree/bridge roots,
  costs, state order and prerequisite unchanged.
- Disabled the old continuous Ground plane; retained it for reference.
- Added visible greybox boundary blocks, a removable far-side barrier, unlocked
  content and distinct polluted/restored dressing parents; assigned all relevant
  ZoneUnlock and RestorationController references.
- Ravine bottom is visual only. Its collider is disabled, allowing existing
  fall recovery to work instead of trapping the player below the bridge.
- Intact/Worn bridge colliders span the gap. Broken leaves only a short near-side
  remnant, so it does not provide a crossing.
- Camera tuning saved: FOV 60, orbital radius 5.5. No authored models created.

### UI / VFX

- Added an anchored overlay HUD: filled energy bar and numeric value, target
  name/state, coloured crosshair, puzzle progress, hint panel and controls line.
- Added spawn/tree/completion HintTrigger volumes with kinematic trigger bodies.
- HUDDisplay now handles startup ordering and updates a target's state/locked
  presentation even when the player continues aiming at the same object.
- Imported TMP Essential Resources already bundled with the installed uGUI
  package. No external download, dependency installation or manifest edit was
  performed in this pass. These provide the font/settings missing from Assets.
- Added short cool inward rewind, warm outward accelerate, per-target flashes
  and a green zone restoration effect in `Assets/04_VFX/Prefabs/`, plus a particle
  material. Effects use a 0.22-second lifetime for the existing 0.25-second use
  cooldown. They are prototype feedback, not final art.
- RestorationController captures/restores fog and the assigned light's original
  properties, blends cool-to-warm light colour, and marks the zone restored on
  completion. No public contract signature was changed.
- Added guarded one-time editor setup helper:
  `Assets/Editor/BorrowedTimeSliceSetup.cs`. It refuses to duplicate an already
  built slice. It is not a general level regeneration tool.

## Tests and observed results

| Check | Result / exact observation |
|---|---|
| Script compilation | Passed; final console query returned zero errors/warnings |
| Missing-script scan | Initially four missing components; final scan zero |
| MainCamera | Resolves to the gameplay camera |
| Camera ray + gauntlet API | Bridge-first refused; Tree Old -> Mature cost 15; Bridge Broken -> Worn -> Intact cost 40; energy 100 -> 85 -> 65 -> 45 |
| Keyboard input reading | Injected Q through the installed Input System and invoked BorrowedTimeInput.ReadInput against controlled targets; same gate/state/energy results |
| Gated hint on HUD | Exact bridge/tree explanation reached the hint label and console |
| Puzzle and zone | IsSolved=True and IsUnlocked=True |
| Restoration | Restored dressing active, fog density 0.012 and GameState.IsZoneRestored(1)=True after background simulation advanced |
| HUD | Energy text showed 45 / 100 and progress 2 / 2 |
| Jump logic | Injected Space plus controlled PlayerController.Update: first vertical velocity 6.871677, second in-air press 6.476993; no second jump boost |
| Timed/native jump attempts | Inconclusive: sampled height=0. Window/input automation and editor focus did not provide a reliable normal-frame key press. Not reported as a manual jump pass |
| Walk input | Timed injected W moved 4.014269 metres; final Y=0.03999982 and grounded=True. Not a calibrated speed benchmark |
| Bridge underfoot | Intact -> Worn -> Broken through gauntlet API; sampled minimum Y=-20.11356, returned to (0,0.04,-3), energy=69.54563 after regen, puzzle still solved |
| Straight-W crossing attempt | Fell and recovered; off-axis camera-relative movement did not stay on the bridge. Not a manual traversal pass |
| Bridge collider traversal | Controlled CharacterController.Move along bridge centre reached (3,0.04,17.50), minimum Y=0.03999984 and grounded=True |
| Audio slots empty | Integrated simulation produced no audio-related exception with all slots empty |
| Fog cleanup | Captured before play: enabled=False, colour=(0.5,0.5,0.5,1), density=0.01; identical after Stop |
| 16:9 HUD | Observed in Game View screenshot at 926x521 |
| 16:10 HUD | Rendered at 1600x1000 using a temporary camera-space Canvas; original overlay configuration restored; image visually inspected |
| Final editor state | Play=False, MainScene dirty=False, missing scripts=0, runInBackground=False |

Some early harness attempts failed because preview scenes use a separate physics
world, editor focus stopped normal game frames, or scripts recompiled during
Play. These were not treated as gameplay passes. A fresh Play session was used
for final system checks. Background simulation was enabled temporarily for
automation and restored to its original false value before the final Stop.

## Remaining manual acceptance / findings

- **P1:** Manually walk the entire map, sprint, jump on ground/bridge/edges,
  steer across the bridge, and orbit 360 degrees near walls at pitch limits.
  Automated method/input checks do not establish the complete human playthrough.
- **P1:** Mouse aiming and movement through the full puzzle still need one
  uninterrupted human run; no claim that a stranger completes it in 5–8 minutes.
- **P2:** UI readability is verified visually at two aspect ratios, but first-time
  comprehension and feel are unverified. All strings are currently English.
- **P2:** VFX are wired and exercised by the state changes; their direction and
  quality need human visual feedback. No camera hold/cutscene was added.
- Models and audio clips intentionally remain placeholders/empty slots.
- There are still only two TimeTargets/spend sinks. The required solution costs
  55 energy; regen prevents a resource-only softlock. Reassess this if more sinks
  are added. Fall recovery keeps the current puzzle/energy state.

## Files / evidence

Modified source:
- Assets/02_SupportSystems/HintTextManager.cs
- Assets/04_VFX/RestorationController.cs
- Assets/05_UI/HUDDisplay.cs
- Assets/Scenes/MainScene.unity

New source/resources:
- Assets/02_SupportSystems/AudioCueManager.cs (+ meta)
- Assets/Editor/BorrowedTimeSliceSetup.cs (+ meta)
- Assets/03_EnvironmentArt/Greybox/ materials (+ meta)
- Assets/04_VFX/BT_Particle.mat and Prefabs/ (+ meta)
- Assets/TextMesh Pro/ bundled essential resources (+ meta)
- Captures/BT_greybox_16x9.png
- Captures/BT_restored_16x9.png
- Captures/BT_restored_16x10.png

A pre-edit copy of MainScene was preserved outside the project at:
`E:/!for WORK/GPT/BT_MainScene_before_build_20261024.unity`.

## Git / existing changes

The initial working tree already contained scene/package/task edits, the new
Player scripts and a deleted Player prefab. Those were preserved. This pass did
not install Cinemachine or create that prefab deletion.

No commit or staging was completed. Automatic approval review rejected the
attempted broad baseline checkpoint because it included prior scene/prefab,
package and document work and conflicted with Task 02's earlier no-commit rule.
No workaround was attempted. Explicit user approval is needed before committing.
`git diff --check` reports Unity-generated blank-field trailing spaces in scene
YAML; those were not cosmetically rewritten behind the Editor's back.

## Facts in the master that differed from the project

- Four MainScene script references were broken despite the document's
  compile-clean/wired description.
- TMP was installed as code, but its font/settings resources were absent.
- The master called fog project-wide; RenderSettings is the active scene's render
  state. Explicit cleanup was nevertheless implemented and verified.
- Git was not clean: significant handoff changes were still uncommitted.

No new model references are required to test this greybox. Ask the user for
references/descriptions before replacing any placeholder with authored geometry.
