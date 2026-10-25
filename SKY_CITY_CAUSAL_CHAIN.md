# Sky City — approved causal chain, 2026-10-25

The user's latest approved design supersedes independent district puzzles and Q/E instructions in older phase documents.

## Player controls and time
- Mouse wheel down ages/decays the selected object and collects its time.
- Mouse wheel up spends stored time to rejuvenate/reconstruct it.
- One meter, capped at 100. Draining still changes objects when the meter is full; surplus is lost.
- No passive regeneration or automatic object reset. Partial states persist between play sessions.
- H gives progressive Thai hints. R deliberately restarts this city chain and its world targets. F enters the separate factory scene after docking and door opening.

## Causal progression
1. Canal: decay the wooden sluice obstruction; repair the pipe and water-wheel blades. Running water and wheel open the clock enclosure.
2. Clock: decay the wooden gear jam and reconstruct the large gear's missing teeth. The clock starts and a telescopic ramp extends to the raised control bay. Upstream water power is required.
3. Bridge controls: drain the reserve cell, repair the docking head while its wooden support is intact, then decay the support to release the telescopic bridge. Both water and transmission power are required.
4. The factory door opens only after the bridge reaches full extension and physically docks. The final mechanical latch preserves factory access if machinery is aged afterward.

The enclosure gate and access ramp latch open once unlocked, preventing loss of upstream power from trapping the player. If the support is removed prematurely, restore it with time before repairing the docking head. Optional city objects provide energy for recovery.

## World interaction and animation
41 optional targets: 10 benches, 3 cargo crates, 12 streetlights and 16 shrubs. Each has a unique saved ID.
Wood and repair fragments deform separately; rust/plant colour and lamp intensity change continuously. Returning time reconstructs the captured original pose. Interaction remains forgiving as wood collapses; decayed wooden solids stop blocking movement.
Water, wheel, gears, clock hands, local audio, gates, access ramp, main bridge and factory door show progression. No city-spanning glowing service routes remain.

## Implementation
- Assets/_BorrowedTime/Scenes/ForgottenCity.unity
- Assets/_BorrowedTime/SkyCity/BTSkyCityGame.cs — input, transfer, targets, save/load, main bridges and factory door
- BTSkyCityPuzzles.cs — causal conditions, access interlocks and machinery/audio
- BTSkyCityAgeVisual.cs — continuous per-part presentation and physical state
- BTSkyCityHints.cs / BTSkyCityVFX.cs — Thai guidance and local transfer effects
- Editor/BTSkyCityChainChecks.cs / BTSkyCityBridgeChecks.cs — runnable checks under BORROWED TIME > Checks

New save namespace: SkyCityChain1. Old SkyCityV2 / SkyCityLogic preferences are preserved.
New scene backup: ForgottenCity_BeforeCausalChain.unity. Older design assets remain for rollback.
Editable Blender sources are included in ArtSource/BorrowedTime/. Imported FBX files are under Assets/Generated/Imported/. Update the authoring scripts export paths for your own checkout before regenerating.

## Verification
- Contract checks: order/gating, both upstream systems, actual wrong-order resource recovery using optional objects, cap/empty/idle/reversal, partial geometry, exact reconstruction and stable aiming.
- Play mode: injected mouse-wheel down/up reached the normal game input handler; clock gate blocked before P1 and allowed traversal after P1; wheel and clock animated; access ramp and factory bridge traversed with CharacterController.
- P3: bridge progressed while factory door stayed at its closed position; at full extension the door rose and commissioning latched.
- Persistence: a real optional target and energy loaded at the saved partial values after stopping/restarting Play; original pose cache survived reload. Two test-created preferences were returned to their original load defaults without deleting keys.
- No standalone build or factory-interior playthrough performed for this change. No packages installed.
