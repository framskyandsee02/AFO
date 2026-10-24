# BORROWED TIME — World Design Document

**Status:** DRAFT for approval · 2026-10-24
**Supersedes:** the single-zone slice scope in `GAME_BRIEF.md`
**Project root:** `E:\!for WORK\all for one`

---

## 1. The One-Line Pitch

A valley is dying from the top down. You carry a gauntlet that moves time inside
single objects — and the only way forward is to bring things back to life, because
what you revive becomes the path you walk on.

---

## 2. Core Design Principle

**Restoration *is* traversal.** Not decoration, not a reward screen — the literal
mechanism of movement.

Every zone has one keystone act of restoration, and completing it physically opens
the way onward:

| Zone | Keystone act | How it opens the path |
|---|---|---|
| Forest | Revive the great tree | Its roots rise and become a walkable bridge upward |
| City | Restore the water wheel | It drives the mechanism that raises the canal lift |
| Factory | Plant the Seed of Wisdom | Yggdrasil grows — the ending |

This is why the player restores things. Not because a quest told them to.

---

## 3. World Structure

### Geography — water carries the problem downhill

```
                              ╔═══════════════╗
                              ║   FACTORY     ║  highest · the source
                              ║  red brick    ║
                              ║  smokestacks  ║
                              ╚═══════╤═══════╝
                                      │ effluent + broken rail line
                                      ▼
                              ╔═══════════════╗
                              ║     CITY      ║  middle · the victim
                              ║  cream stone  ║
                              ║  canals       ║
                              ╚═══════╤═══════╝
                                      │ waterfall + old timber bridge
                                      ▼
                              ╔═══════════════╗
                              ║    FOREST     ║  lowest · the symptom
                              ║  moss · wood  ║
                              ║  great tree   ║
                              ╚═══════════════╝
                                   ▲ PLAYER STARTS HERE
```

**Why start at the bottom:** the player meets the *symptom* first and must climb
toward the *cause*. The geography teaches "fix the root cause" without a single
line of instruction — you are literally walking upstream.

### Scale — 5 minutes end to end

Player walk speed is 4 m/s, sprint 6.5 m/s.

| Measure | Target |
|---|---|
| Main axis, end to end | ~800 m |
| Per zone, along the main axis | ~250–280 m |
| Straight walk, bottom to top | ~3 min |
| With exploration and elevation | ~5 min |
| Full playthrough | 30–45 min |

**Vertical separation matters more than horizontal.** Roughly 40 m of climb from
forest floor to factory yard. Height makes the valley readable at a glance and
makes "upstream" visible rather than stated.

### Seamless — no loading, no transition cutscenes

One scene. The player walks from forest to city to factory without interruption.

- Single `MainScene` with three zone roots
- LOD + occlusion culling handle the cost; **no streaming for a world this size**
- Zones separated by elevation and geometry (ravine walls, tunnel, canyon), which
  gives occlusion culling natural barriers to work with
- If profiling later shows a problem, additive scene loading is the fallback —
  but do not build for it preemptively

---

## 4. The Energy Economy

### The rule that prevents softlocks

Two protections, both required:

1. **Repaired paths stay repaired.** Once the tree roots rise, they never
   retract. Once the lift works, it keeps working. Backtracking is always free.
2. **Every zone holds at least one renewable time source** — an object that can be
   aged for time and rejuvenated again, indefinitely.

Together these mean the player can always recover. They may have to walk back,
but they can never be stranded.

### Time sources per zone

| Zone | Source | Yield | Fiction |
|---|---|---|---|
| Forest | Fallen deadwood (several) | 25 each | Dead trunks still hold the years they lived |
| City | Crumbling stone arches | 30 each | Centuries pressed into worked stone |
| Factory | Rusted machinery, rail cars | 40 each | Industry's stolen decades |

Yields rise with zone depth so late-game costs stay affordable without backtracking
becoming mandatory.

### Costs

| Act | Cost | Notes |
|---|---|---|
| Minor restoration (a plank, a vine, a pane) | 10–20 | Optional, cosmetic, or a shortcut |
| **Revive the great tree** | **60** | Forest keystone |
| **Restore the water wheel** | **80** | City keystone |
| Clear factory obstacles | 30–50 each | Three or four of them |
| **Grow Yggdrasil** | **Everything the player holds** | The ending — see §7.3 |

### Storage cap

Current cap is 100. **This needs to rise** — the city keystone alone costs 80, and
the finale should feel like spending a fortune.

**Recommendation: 150.** Enough that the finale feels substantial, tight enough
that the player must still make choices. Open to tuning after playtest.

---

## 5. Zone Design

### 5.1 Forest — the symptom

**Visual language:** moss, wet timber, grey-green canopy, shafts of light.
Reference image 1 drives the mood — but this is a *sick* forest: the great tree is
bare, water runs cloudy, flowers are closed.

**Layout**
- **Opening clearing** — spawn. The great tree is visible immediately, dead and
  enormous. First thing the player sees. First question they ask.
- **Deadwood hollow** — the tutorial time source. Several fallen trunks. Here the
  player learns hold-E to extract.
- **Cloudy stream** — runs from the city waterfall. Follow it upstream and it leads
  to the way out. The water is the signpost.
- **The great tree** — 25 m tall, roots splayed across a 15 m ravine. Dead.
- **Ravine** — the barrier. Too wide to jump. The roots are the only crossing.

**Keystone — The Great Tree**
Cost 60. The player extracts from deadwood until they can afford it, then holds Q
on the tree. It takes several seconds of channeling — long enough to feel like an
act, not a button press.

Bark darkens and thickens, the canopy fills, and **the roots lift from the ravine
floor and knit into a bridge.** The way up opens.

**Optional restorations**
- Closed flowers → bloom (cosmetic, 10 each)
- The rotted footbridge in reference image 1 → a shortcut back to spawn (15)

---

### 5.2 City — the victim

**Visual language:** cream sandstone, terracotta, canal water, climbing ivy.
Reference image 2 is the target state. **At arrival the city is grey** — the
palette drains to near-monochrome, the canals run with factory effluent, ivy is
brown, the clock tower has stopped.

Stone does not die the way wood does. The city reads as *abandoned mid-life*:
chairs still at tables, shutters half open, nobody home.

**Layout**
- **Lower canal** — entry from the forest side. The effluent is visible and ugly.
- **Stepped terraces** — the climb. Stone stairs, landings, washing lines.
- **Plaza and clock tower** — the centre. The stopped clock is the city's wound.
- **Water wheel** — the keystone, at the canal head. Seized, split, rotted.
- **Canal lift** — the way out. A stone platform lift driven by the wheel. Dead
  without it.

**Keystone — The Water Wheel**
Cost 80. The most expensive restoration before the finale, and it requires
extracting from several stone arches first — which means genuinely exploring the
city rather than walking through.

Restoring the wheel turns the shaft, and the shaft drives the lift. **The clock
tower restarts at the same moment** — a second visual beat the player did not pay
for, which makes the city feel alive rather than merely functional.

**Optional restorations**
- Fountain statue in the plaza (20)
- Ivy on the walls, brown → green (15 each)
- Market awnings (10 each)

---

### 5.3 Factory — the cause

**Visual language:** red brick, rusted steel, smoke, weeds forcing through
concrete. Reference image 3 is close to how the factory actually looks —
**this zone does not get prettier until the very end.** The weeds in that image are
the only life here, and they matter: nature is already trying.

**Layout**
- **Rail approach** — entry from the city lift. Broken track, rusted cars.
- **Pipe yard** — the obstacle course. Three or four blockages, each solved by
  aging or rejuvenating something:
  - A collapsed pipe → rejuvenate to walk through it
  - A sealed steel door → age it to rust until it fails
  - A gap → rejuvenate the conveyor to bridge it
- **The outfall** — the heart. The widest effluent pipe, pouring into the water
  that poisons everything below. This is where the seed goes.

**Keystone — Plant the Seed**
Planting itself is free. **Growing it costs everything the player holds.**

---

## 6. Narrative

### The voice of the tree

No NPCs, no dialogue trees, no voice acting. The tree speaks **once**, in on-screen
text with a wind-through-leaves sound beneath it. It is the only voice in the game,
which is what makes it land.

It speaks when it is revived, and it says roughly this — final wording to be
tuned, kept short:

> *"You gave me back my years. Take this seed. It does not belong in soil that is
> already living — carry it to where the water is poisoned, and plant it at the
> wound."*

That is the entire plot. One instruction, from one source, delivered once. The
player then has to find the wound themselves.

### Why the seed comes from the tree

The first thing you save gives you the means to save everything. The forest is not
a level you clear and leave behind — it is where the ending begins. The player
holds that seed through the whole city and factory, and it quietly means something
the entire way.

---

## 7. Cutscenes

**Three total. None at zone transitions** — the player walks between zones
uninterrupted, as specified. Cutscenes fire **only when a keystone restoration
completes**, as the payoff.

Each holds the camera briefly, then returns control. No skipping needed — keep
them short enough that nobody wants to.

### 7.1 Forest — "The Tree Remembers"
**~12 seconds**
- Camera pulls back from the player to frame the whole tree
- Bark thickens, canopy fills from trunk outward
- Light shifts from grey to gold as the canopy catches it
- **Birds enter the frame** — a flock rising past the canopy
- Butterflies and drifting pollen fill the lower air
- Roots lift from the ravine and knit into a bridge
- The tree's line appears; wind-in-leaves sound
- Control returns on the new bridge

### 7.2 City — "The Clock Starts"
**~12 seconds**
- Camera rises from the wheel to overlook the plaza
- The wheel turns; water runs clear downstream
- **Colour returns in a wave outward from the wheel** — terracotta, cream,
  green ivy, washing on lines
- The clock tower hands move; a bell sounds
- The lift platform descends into frame, ready
- Control returns

### 7.3 Factory — "Yggdrasil"
**~25 seconds — the finale**
- The player plants the seed at the outfall
- Camera stays low and close as the first shoot breaks concrete
- **The player's stored time drains to zero on the HUD** — visibly, deliberately
- The tree accelerates: trunk thickening, bark splitting brick, roots cracking
  the outfall shut
- Camera tilts and rises, and keeps rising — the tree outgrows the frame
- Smokestacks fall. The effluent stops.
- Final shot pulls back across the whole valley: **Yggdrasil visible from the city
  and from the forest**, canopy above the mountains
- Fade

**Yggdrasil must be visible from every zone after this.** A far silhouette costs
almost nothing and makes the valley feel like one place the player changed.

---

## 8. Art Direction — "stylized, middle ground"

Not flat low-poly, not photoreal. Readable shapes with enough surface detail to
feel like material.

| Element | Treatment |
|---|---|
| Geometry | Clean silhouettes, moderate poly count. Shape reads before detail. |
| Textures | Simple albedo + light roughness variation. Moss, rust, wet stone. No PBR scan work. |
| Colour | **Each zone owns a palette.** Forest grey-green→gold · City grey→cream-terracotta · Factory rust-red, staying rust-red |
| Lighting | Directional light per zone mood. Forest: low shafts. City: open sky. Factory: overcast with smoke haze |
| Before/after | Carried by **colour saturation and light**, not by swapping models. Cheapest convincing transformation available |

### What gets built how

| Asset | Method | Why |
|---|---|---|
| Great tree, Yggdrasil | Blender | Hero assets — must be exactly right |
| Water wheel, clock tower, lift | Blender | Specific mechanisms, must read as functional |
| Rock, cliff, terrain | Blender + Unity terrain | Bulk geometry |
| Buildings | Blender modular kit | Build once, arrange many |
| Pipes, rail cars, smokestacks | Blender modular kit | Same |
| **Birds, butterflies, fireflies, pollen** | **Unity particles** | No rigging, no animation, full effect. This is how "life returns" gets delivered |
| Smoke, water spray, dust | Unity particles | Standard |
| Grass, small plants | Unity detail meshes | Standard |

**On downloaded assets:** Claude cannot fetch files — the workspace network is
allowlisted to package registries and GitHub only, and downloads need the user's
per-item approval regardless. If a specific asset would genuinely help, Claude
researches candidates, checks licensing, and hands the user a shortlist to
download. Default is build-in-Blender, because it is unblocked.

---

## 9. Build Plan

Each phase ends playable. Nothing is built that cannot be walked through.

### Phase 1 — Greybox the whole valley ← START HERE
Three zones at final scale, grey boxes only. Correct elevation, correct distances,
correct connection points. No art.

**Done when:** the player walks forest → city → factory end to end, all three
keystones work as placeholder interactions, and the 5-minute traversal target is
measured rather than estimated.

This phase answers the only question that matters early: **does the valley feel
right to walk through?** Everything after is refinement.

### Phase 2 — Forest, finished
One zone fully realised: models, materials, lighting, particles, the tree cutscene,
the seed handoff. Establishes the quality bar the other two match.

### Phase 3 — City, finished
Modular building kit, canal water, the wheel and lift mechanism, colour-return
cutscene.

### Phase 4 — Factory and finale
Pipe-yard obstacles, the outfall, Yggdrasil, the ending cutscene, the far
silhouette visible from every zone.

### Phase 5 — Pass over the whole thing
Balance the economy against real play. Audio. Thai localisation. Performance.
Playtest with someone who has never seen it.

---

## 10. Open Questions

1. **Storage cap at 150?** Needed for the city keystone at 80 plus a finale worth
   feeling. Tunable after playtest.
2. **Does the factory ever get beautiful?** Current intent: no — it stays rust and
   brick, and Yggdrasil growing *through* it is the contrast. The alternative is a
   full green takeover, which is more spectacular but loses the point that some
   damage does not undo.
3. **Thai or English?** Everything is English right now. The tree's single line is
   the most important text in the game.
4. **Factory obstacles — three or four?** Four gives the zone more weight; three
   keeps the finale closer. Decide after the greybox walk.

---

## 11. What Carries Over

The slice already built is not thrown away:

| Existing | Becomes |
|---|---|
| `Tree_Target` (3 states) | The great tree, with more states and a bigger model |
| `Bridge_Target` | A forest footbridge — an optional shortcut |
| `TimeReserve_Target` | The deadwood time source, duplicated across the forest |
| `PuzzleCondition` + `ZoneUnlock` | Per-zone keystone gating, unchanged |
| `RestorationController` | Per-zone colour and atmosphere transitions |
| Hold-E / hold-Q channeling | Unchanged — this is the core and it works |
| HUD, hints, audio routing | Unchanged, extended |

**The engine is right. The world is what is missing.**
