# CODEX TASK 02 — Implement: Player Controller & Camera

**From:** Claude (Team Lead)
**To:** Codex (Implementation Assistant)
**Project:** BORROWED TIME: Echoes of Tomorrow
**Project root:** `E:\!for WORK\all for one`
**Date issued:** 2026-10-24
**Type:** IMPLEMENTATION (you write code this time, not just review)

---

## ⚠️ READ THIS FIRST — how I want you to behave on this task

The user has asked explicitly: **if anything is unclear, ask. Do not decide for me.**

That instruction outranks speed. Concretely:

- If a design choice has more than one defensible answer and this document does not settle it, **stop and ask** before writing the code that depends on it. Section 8 lists the ones I already know about — there may be others.
- If you need a Unity package that is not already in the project, **stop and ask**. Do not install anything. (`Packages/manifest.json` is the source of truth. Cinemachine in particular — check, do not assume.)
- If a file you need to touch is not in the allowlist in Section 3, **stop and ask**.
- Do not "temporarily" work around a blocker and flag it later. Ask at the point you hit it.
- If you cannot reach the project or an MCP tool, say so plainly and tell me what you need.

A half-finished task with three clear questions attached is a better outcome than a complete task built on three guesses.

---

## 0. Before you start

Read from the project — do not take my word for any of it:
- `MultiAgent_GameDev_Prompt_Pack/MultiAgent_GameDev_Prompt_Pack/MASTER_RULES.md`
- `MultiAgent_GameDev_Prompt_Pack/MultiAgent_GameDev_Prompt_Pack/CODEX_IMPLEMENTATION_REVIEWER.md`
- `README_BORROWED_TIME-1.md` — the approved Game Brief
- `CODEX_TASK_01.md` — the review task; its Section 3 describes the system architecture you are plugging into
- The twelve system scripts listed in `CODEX_TASK_01.md` Section 2, **especially**:
  - `Assets/01_GameSystems/TimeGauntletController.cs`
  - `Assets/01_GameSystems/TimeTarget.cs`
  - `Assets/02_SupportSystems/HintTrigger.cs`

Then report, from what you can actually observe:
1. Can you read **and write** `E:\!for WORK\all for one`? State it explicitly.
2. Can you see a Unity Editor instance, trigger a compile, or enter Play mode? If not, mark all runtime checks NOT RUN.
3. `git status` in the project root — is it a repo, and is the tree clean? I asked the user to run `init_git.ps1` before handing you this. **If git is not initialised, stop and tell me** — I do not want concurrent edits without version control.
4. Does `Packages/manifest.json` contain Cinemachine? Report the exact package list entry or its absence.

---

## 1. Objective

Implement the **player controller and camera** for the vertical slice, so the
existing Time Gauntlet systems become playable. Right now `MainScene` has no
camera at all, so `Camera.main` is null and the gauntlet cannot aim — the whole
puzzle is unreachable.

### Non-goals
- Do not modify the twelve system scripts from Task 01. If one needs a change to accommodate the player, **propose it to me** — Agents 04 and 05 build against that API.
- Do not build HUD or UI (Agent 05).
- Do not create 3D models (Agent 03, in Blender). A capsule is the correct player visual for now.
- No animation, no footstep audio, no inventory, no save system.

---

## 2. What the brief specifies

From the approved Game Brief, the movement requirements are:

| Input | Action |
|---|---|
| WASD | Move |
| Mouse | Look |
| Shift | Sprint |
| Space | Jump |
| Q / E | Rewind / Accelerate (already implemented — do not reimplement) |

Third-person perspective. That is the whole movement spec; the brief does not
specify camera distance, acceleration curves, or jump height. Pick sane starting
values, expose them as `SerializeField`, and say in your report which numbers you
chose so the user can tune them.

---

## 3. Files you may create or modify

**Create:**
| Path | Purpose |
|---|---|
| `Assets/02_SupportSystems/PlayerController.cs` | Movement |
| `Assets/02_SupportSystems/PlayerCameraRig.cs` | Glue for the Cinemachine rig — mouse input → orbital axes, cursor lock. The rig itself is Cinemachine components I wire in the scene; this script only drives it. |
| `Assets/02_SupportSystems/PlayerInput_BT.cs` | Optional: split input out of the controller if you prefer. Name it exactly this to avoid colliding with Unity's own `PlayerInput`. |

**Modify:**
- Nothing in the existing codebase without asking first.

**Do not touch:**
- `Assets/Scenes/MainScene.unity` and anything under `Assets/Prefabs_Shared/` — I own scene and prefab wiring through Unity MCP. Concurrent edits there clobber silently.
- `Assets/Editor/BorrowedTimeSceneSetup.cs`
- `Assets/InputSystem_Actions.inputactions`
- `Assets/Settings/`
- `Packages/manifest.json`

**Scene wiring is mine.** Deliver the scripts plus a short list of exactly what I
should add to the scene (GameObject names, components, inspector values, tags,
layers). I will wire it and report back what happened.

---

## 4. Interface contract — what the existing systems expect of the player

These are hard requirements. Breaking any of them silently breaks a system that
already works.

1. **`Camera.main` must resolve to the player's camera.**
   `TimeGauntletController` aims a ray from `aimCamera.ViewportPointToRay(0.5, 0.5)`,
   falling back to `Camera.main` when its field is empty. The camera therefore needs
   the `MainCamera` tag. Aim is from the **camera**, not the character — in third
   person those differ, and that is intended.

2. **The player GameObject must be tagged `Player`.**
   `HintTrigger.OnTriggerEnter` filters on `other.CompareTag(playerTag)` with
   `playerTag` defaulting to `"Player"`. Confirm the tag exists in the project; if not,
   tell me and I will add it (tag changes are a project-settings edit).

3. **Traversal is driven by colliders, not by code you write.**
   `TimeTarget` enables exactly one state's `stateCollider` at a time. The bridge's
   `Intact` and `Worn` states are `isTraversable`; `Broken` is not. So the player
   should simply collide with whatever is enabled. **Do not query `TimeTarget.IsTraversable`
   from movement code** — if the colliders are wired right, physics already handles it.
   If you find a case where that is not enough, tell me rather than reaching into the API.

4. **The gauntlet already owns Q and E.**
   `BorrowedTimeInput` reads `Keyboard.current.qKey` / `.eKey` directly. Your input
   code must not consume or conflict with those.

5. **Time scale.** Nothing in the current systems modifies `Time.timeScale` — the
   gauntlet changes discrete object states, it does not dilate time. Use `Time.deltaTime`
   normally for movement. (Cooldowns and hint timers use `unscaledTime` deliberately;
   that is not a precedent for movement.)

---

## 5. Acceptance criteria

A reviewer should be able to confirm each of these:

- [ ] Scripts compile with no errors and no new warnings.
- [ ] WASD moves the character relative to **camera facing**, not world axes.
- [ ] Mouse look orbits the camera around the character; pitch is clamped so it cannot flip over.
- [ ] Shift sprints; the speed difference is obvious without being comical.
- [ ] Space jumps only when grounded. No double-jump, no mid-air steering abuse.
- [ ] The character does not fall through `Ground` (a scaled Plane with a MeshCollider).
- [ ] Camera does not clip through `Ground` or through the greybox blocks. If you solve this with a collision probe, say so; if you leave it unsolved, say that instead of leaving me to find out.
- [ ] Aiming the camera's centre at `Tree_Target` and pressing Q changes its state and spends energy.
- [ ] Aiming at `Bridge_Target` while the tree is still `Old` prints/returns the gated hint instead of changing state.
- [ ] Every `SerializeField` that could be left empty is null-guarded.
- [ ] Cursor is locked and hidden during play, and released on `Escape`. (If you think `Escape` belongs to Agent 05's pause menu instead, ask.)

---

## 6. Test plan

**Run what you can; mark honestly what you cannot.** Do not report a pass you did not observe.

**Static:**
1. Compile check.
2. Re-read your own code against Section 4's five contract points, one by one.

**Play mode (only if you can actually enter it):**
1. Walk the full extent of `Ground` — no fall-through, no stuck corners.
2. Jump repeatedly on flat ground, on the bridge, and at the Ground edge.
3. Rotate the camera a full 360° at several pitches; check for flipping and clipping.
4. Stand at `Tree_Target` (-4, 0, 6), aim centre-screen at it, press Q → state should go Old → Mature, energy 100 → 85.
5. Then aim at `Bridge_Target` (3, 0, 10), press Q twice → Broken → Worn → Intact, energy 85 → 45.
6. That should solve `puzzle_01_bridge` and fire the zone unlock and restoration. Report what you observed, including whether the fog blend looked right.
7. Reload the scene and aim at the bridge **first**, before touching the tree → it must refuse with the gated hint.

**EditMode tests:** do not write any yet. There is no test assembly and I have not
authorised installing one. If you think one is warranted, propose it in your report.

---

## 7. Report format

Per `CODEX_IMPLEMENTATION_REVIEWER.md`:

1. Verdict: COMPLETE / PARTIAL / BLOCKED
2. Scope covered
3. Files created or changed, with paths
4. **Scene wiring instructions for me** — GameObjects, components, inspector values, tags, layers
5. Findings ranked by severity
6. Tests run and exact results
7. Tests not run, and why
8. Recommended fixes
9. **Questions blocking you** — anything from Section 8 or that you hit along the way
10. Decisions needing user approval

---

## 8. Questions I already know you will face — ask me, do not pick

I have deliberately not decided these. Answer with your recommendation **and the
reasoning**, then wait for my go-ahead on the ones marked ⛔.

**Q1 ✅ ANSWERED — use Cinemachine.**
Settled. The user approved the install; I installed **Cinemachine 3.1.7** on
2026-10-24. Use it. Do not hand-roll an orbit rig.

⚠️ **Cinemachine 3.x is not 2.x.** The API was renamed between major versions:
`CinemachineVirtualCamera` → `CinemachineCamera`, `CinemachineFreeLook` →
`CinemachineCamera` plus an orbital follow component, and the namespace is now
`Unity.Cinemachine`. Code written against 2.x will not compile. Read the installed
package's own documentation; do not write from memory of 2.x tutorials.

`CinemachineBrain` goes on the camera carrying the `MainCamera` tag — that is the
camera `TimeGauntletController` raycasts from (Section 4, point 1). Tell me the
exact Cinemachine components and inspector values you want; I wire them in the scene.

**Q2 ⛔ CharacterController or Rigidbody?**
`CharacterController` is simpler, gives reliable `isGrounded`, and ignores physics
forces. `Rigidbody` integrates with the physics world, which may matter later if
objects the player stands on change state underneath them — a real scenario in this
game, since the bridge mutates while you are on it. **Consider that case specifically
and tell me what happens in each approach.** That consideration is the actual reason
I am asking.

**Q3 Input source.**
The gauntlet reads `Keyboard.current` directly because the generated action class
from `InputSystem_Actions.inputactions` does not exist. Should the player match that
for consistency, or is this the moment to generate the action class properly? I own
that asset file — do not edit it, but tell me what you would want from it.

**Q4 What happens when the ground changes state under the player?**
If the player is standing on the bridge and it goes `Intact → Broken`, the collider
they are standing on is disabled that frame. Do they fall, clip, or get ejected?
Whatever your answer to Q2, describe the actual behaviour. If handling this needs a
hook in `TimeTarget`, propose the signature — do not add it.

**Q5 Does `Escape` belong to you or to Agent 05?**
Cursor unlock has to live somewhere. I have not assigned the pause menu yet.

---

## 9. Constraints

- **Do not** install packages.
- **Do not** edit scene or prefab files.
- **Do not** change the public API of existing systems without my approval.
- **Do not** claim a test passed unless you ran it and read the output.
- **Do not** create 3D models in Unity in place of Blender work.
- **Do** commit nothing yourself — tell me what changed and I handle integration.
