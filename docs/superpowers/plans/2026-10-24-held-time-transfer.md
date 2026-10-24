# Held Time Transfer Implementation Plan

Goal: One meter starts empty; held E ages objects and collects time, held Q
spends stored time to rejuvenate. Release pauses at the visible intermediate age.
This records the design approved in the chat on 2026-10-24 and supersedes the
old discrete-spend/regen rules in GAME_BRIEF and MASTER_BUILD_PROMPT.

Approach: extend TimeTarget with a continuous age coordinate while retaining
discrete authored states, public endpoint APIs and existing greybox roots.
Interpolate their transforms and colours; do not author models without references.
Energy and progress move together, including at empty/full meter boundaries.
Keep source-state collision during a partial interval, with its interpolated
geometry; hand over to the destination collider only at the endpoint.
Prerequisites and puzzle completion require an exact completed age, not a stale
last-completed index. Keep a short existing cooldown at age endpoints so the
player can release on Mature. Never regenerate time passively.

Tuning: 3 seconds per age interval; costs/yields retain target energy values.
Add a reversible greybox time reserve yielding 75 units, since the original tree
and bridge begin at their oldest endpoints. No packages, final models or audio.

- [ ] Verify old contract fails new requirements; preserve current scene state.
- [ ] Implement meter/continuous age/channel input and exact-state puzzle checks.
- [ ] Wire reserve, update HUD/tutorial and verify compile.
- [ ] Check partial hold/release/reversal, endpoint gating, empty/full limits,
      conservation, start-at-zero solution, fall recovery and scene reload.
- [ ] Update handoff/spec and commit the integrated implementation as requested.

Review focus: no resource duplication from reversing or cap overflow; no
premature bridge unlock at partial ages; no animation transform drift; release,
loss of aim/focus and simultaneous Q/E do not continue transferring; meter zero
does not softlock the opening puzzle. Tests use the installed Unity runtime and
ad-hoc deterministic assertions, without adding a test assembly/package.
