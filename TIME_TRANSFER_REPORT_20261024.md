# Held time transfer — implementation and verification

## Scope and verdict

Implemented the user's revised single-meter mechanic using the existing greybox.
E held: age/break an object and collect time. Q held: spend stored time to
rejuvenate/repair. Start at 0; no passive regeneration. Release freezes the
intermediate state and reversing resumes there. Default is 3 seconds per age
interval, plus a 0.25-second pause on exact endpoints for controllability.

This is procedural greybox animation (position, rotation, scale and colour), not
final tree/bridge model animation. User references remain required before final
models. No packages or external assets were installed for this revision.

## Implementation

- EnergyManager: removed regeneration; preserved event/getter/debug APIs.
- TimeTarget: continuous AgePosition, IsAtState and OnAgeChanged; cached authored
  transforms and renderer colours; interpolation preserves partial geometry on
  release. Active source-state collider follows that geometry, with endpoint
  handoff to the destination state collider. No query of IsTraversable in movement.
- TimeGauntletController: proportional, bounded transfer. Q cannot restore more
  than current storage affords; E cannot age further than free storage permits.
  Missing energy manager fails closed. Re-read camera ray in the request itself
  to remove aim/input Update-order dependence. Rejections are rate-limited.
- BorrowedTimeInput: isPressed rather than wasPressedThisFrame. Simultaneous Q/E
  cancels, as do cursor unlock and focus loss. No coroutine continues channeling.
- PuzzleCondition uses exact age endpoints and observes age changes. A last
  completed index does not incorrectly unlock a partially changed prerequisite.
- HUD refreshes fractional age/state text and the one stored-time meter.
- AudioCueManager retains silent-safe slots and avoids a duplicate generic cue
  when insufficient-energy feedback is used.
- Scene: reversible greybox TimeReserve_Target yields 75 units; opening hints
  and controls teach held E/Q. This source is necessary because the original
  tree/bridge begin at their oldest endpoints and cannot supply initial time.
- Far-side barrier spans the bank, so an almost-restored bridge cannot bypass
  the exact-endpoint puzzle gate by walking around a narrow barrier.
- GAME_BRIEF and MASTER_BUILD_PROMPT identify the new approved mechanics;
  historical discrete-spend/regen descriptions are explicitly superseded.

## Observed runtime checks

Tests call the actual private channel step deterministically in Unity Play mode;
they are not represented as an uninterrupted human mouse/keyboard playthrough.

| Check | Observed result |
|---|---|
| Start | CurrentEnergy = 0 |
| Partial extraction | 0.75 seconds equivalent: reserve age 0.25, energy 18.75, visible height 1.575 (between original 2 and spent 0.3) |
| Release | Across separate tool calls with no channel request: age 0.25 and energy 18.75 unchanged |
| Reverse partial | 0.375 seconds Q equivalent: age 0.125, energy 9.375 |
| Complete extraction | Reserve reaches Spent with 75 total stored time |
| Gated bridge | Refused before tree reaches Mature; no spend/age change |
| Partial prerequisite | Tree halfway from Old to Mature does not unlock bridge |
| Opening solution | Collect 75, Q tree to Mature -> 60, Q bridge to Worn -> 40, Q bridge to Intact -> 20; puzzle solved |
| Full storage | At 100, E does not change the object's age |
| Near capacity | With room for 5, E moves reserve exactly 5/75 of an interval and meter reaches 100 |
| Empty storage | Q does not change age or make meter negative |
| Reversal repetition | 100 partial E/Q reversals returned to age 0, energy 0 and original height 2; no measured resource/transform drift |
| Mid-animation capture | Age 0.5, exact energy 37.5 (HUD rounds to 38), height 1.15 |
| Paused capture | Subsequent capture retained age 0.5 and energy 37.5 |

Evidence: `Captures/TimeTransfer_before.png` and
`Captures/TimeTransfer_partial.png`. QA used a temporary runtime camera for the
reserve close-up; stopped Play restores the saved player camera configuration.

## Remaining verification / limits

Final review found and corrected an endpoint rounding defect: partial progress
no longer snaps beyond its paid delta. Endpoint gates now require an exact
endpoint; resource validation/payment precedes age-change notifications.
Fresh Play retest: opening solution retained 20 energy; 100 reversals returned
to age 0 / energy 0. Boundary regression (age 0.00002, energy 0.001, rate 75)
stopped at age 0.00000666666529 with energy 0 and endpoint false, rather than
granting an unpaid endpoint. Insufficient floating-point budget fails closed.

- A full human held-key playthrough, camera tracking as objects shrink, and final
  subjective animation/art feedback remain necessary.
- Existing instantaneous SetState/Rewind/Accelerate endpoint APIs are retained
  for debug/setup compatibility; normal Q/E gameplay does not use them.
- Partial progress is in-memory only; there is no save/load feature in scope.
- Greybox deformation is suitable for these shared cube meshes. Final authored
  meshes may need blend shapes, skeletal or piece animations after references
  and a model-specific animation plan are agreed.
- Full-meter extraction stops rather than wasting additional time. This keeps
  reversible objects from becoming an unrecoverable resource-loss sink.

The earlier BUILD_REPORT documents the integrated baseline and its manual
playtest limitations; energy/regen expectations in that report are historical.
