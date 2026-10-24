# AGENT 03 — Map & Environment

> **Read `MASTER_BUILD_PROMPT.md` first.** Current state is §2, §0 holds the three
> rules that override everything. This file is your slice of §6 (Track B).
> Where the two disagree, MASTER wins.

**Role:** build a space that teaches the puzzle by its layout, and replace greybox
with real geometry.
**Project root:** `E:\!for WORK\all for one`

---

## Scope

| You own | You must not touch |
|---|---|
| `Assets/03_EnvironmentArt/` — models, materials | Any `*.cs` outside your folder |
| Level layout, terrain, props, boundaries | The `TimeTarget` components themselves |
| `ZoneUnlock` / `RestorationController` **array contents** (not the scripts) | Canvas and UI (Agent 05) |
| Blender authoring and FBX export | |

**Depends on:** Agent 01's Task 1 report. Do not dress a loop that is still broken.
**Blocks:** Agent 04 (lighting needs geometry), Agent 05 (hint placement needs layout).

**Scene edits require Unity MCP.** Without it, deliver FBX files plus written
placement instructions.

---

## Task 1 — Layout

One flat plane is not a level. The current `Ground` is a Plane scaled (3,1,3).

Build a small, readable space:

| Element | Purpose |
|---|---|
| Start area | Clear sightline to the withered tree. The player's first look should land on the thing they can fix. |
| **A ravine or gap** | The bridge spans it. **The obstacle must be visible before the bridge is usable** — otherwise the player has no reason to want it fixed. This single piece of geometry carries the puzzle's motivation. |
| Far side | The locked zone. Visibly present, clearly unreachable. |
| Boundaries | Terrain, rocks, water — not invisible walls. |

**Scale:** the whole slice runs 5–8 minutes. End to end on foot should be under a
minute. Resist sprawl — an empty large map reads worse than a dense small one.

The tree sits at (-4, 0, 6) and the bridge at (3, 0, 10). You may move them, but
tell Agent 01 if you do — their play-test script cites those coordinates.

---

## Task 2 — Fill the empty arrays

Two components currently reference **nothing**, so the gate opens nothing and the
restoration swaps nothing:

| Component | Field | Assign |
|---|---|---|
| `ZoneUnlock` | `barriersToRemove` | Whatever blocks the far side |
| `ZoneUnlock` | `contentToReveal` | Far-side content — **starts disabled**, the script enables it |
| `RestorationController` | `pollutedDressing` | Dead props, debris, haze |
| `RestorationController` | `restoredDressing` | Greenery, clear water |

Put each dressing set under **one parent each**, so the swap is a single
`SetActive` per side. The script already does exactly that — do not restructure it.

---

## Task 3 — Models

**Greybox is shippable.** Real models are an upgrade, not a blocker. If time is
short, Task 1 and Task 2 matter more than this.

If you have Blender MCP, author six pieces:

| Object | States |
|---|---|
| Tree | young sapling · mature canopy · dead and bare |
| Bridge | intact · worn with gaps · collapsed |

- **Low-poly stylised, flat colour, no textures.** Vertex colour or simple materials.
- Export FBX to `Assets/03_EnvironmentArt/Models/`
- Unity 6 imports Blender files directly, but explicit FBX export is more predictable — prefer it.

### Swapping models in without breaking the state machine

Each `TimeTarget` holds a `TimeStateDefinition[]`, and each entry points at a
`visualRoot` (a child GameObject) and a `stateCollider`. The greybox children are
named `State_0_*`, `State_1_*`, `State_2_*`.

**To swap:** add the real mesh as a child in the same slot, then repoint
`visualRoot` and `stateCollider` at it. The state indices, costs, prerequisite
gating and puzzle requirements all survive untouched.

**Do not** delete a `State_*` child before its replacement is wired — the array
entry goes null and the state silently stops rendering.

**Do not model in Unity.** Primitives are for greybox only; authored geometry
belongs in Blender. This is a standing rule in `MASTER_RULES.md`.

### Bridge colliders carry the traversal rule
`Intact` and `Worn` are flagged `isTraversable`; `Broken` is not. Traversal is
**collider-driven** — the player simply collides with whatever state is enabled.
When you swap meshes, **the Broken state must not leave a walkable surface across
the gap.** If it does, the puzzle is bypassable and nobody will notice until a
playtester walks across a collapsed bridge.

---

## Acceptance

- [ ] The gap and the far side are visible from the start area
- [ ] A player who has never seen the game understands the bridge is broken
- [ ] No invisible walls where geometry would read better
- [ ] All four arrays in Task 2 populated; gate and restoration visibly do something
- [ ] Before/after reads clearly in a side-by-side screenshot
- [ ] End to end on foot is under a minute
- [ ] **Walked the whole map in Play mode** — no fall-through, no stuck corners, no climbing out of bounds
- [ ] If models were swapped: every state still renders, and `Broken` offers no crossing

---

## Ask, do not decide

**Ask before:** moving the tree or bridge coordinates (tell Agent 01) · changing
`TimeTarget` state counts or order · anything that would push the slice past 8
minutes · installing a package.

**Decide alone:** layout geometry, prop placement, colour palette, model
silhouettes, material choices, which boundary type suits which edge.

---

## Report

Verdict · what you ran and saw (**including walking the map in Play mode**) · files
created, with paths · screenshots or a described before/after · findings by
severity · **anything in MASTER §2 that turned out to be wrong.**
