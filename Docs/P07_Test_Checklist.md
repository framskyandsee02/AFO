# OOPS! PLANET — P07 Test Checklist (from P01–P06 acceptance criteria)

Build under test: P06 commit `c063589` + P07 changes (ground probe, frame cap, build test runner). Unity 6000.3.25f1, URP 17.3, Windows 11.
Build: `Builds/OopsPlanet_Win64/OopsPlanet.exe` (99.5 MB, 0 errors, 0 warnings).
Evidence: `Assets/OopsPlanet/Evidence/*.json|png` (Editor) and `%USERPROFILE%\AppData\LocalLow\DefaultCompany\AFO\test-reports` (built player).
Run tests in a build: `OopsPlanet.exe -oopsAutoTest diag,screens,route,absorb,power,puzzle,perf` (add `-batchmode -nographics` for headless).

Legend: ✅ verified by automated test · 👀 verified visually (screenshot) · 🙋 manual play · ⏳ not yet verified

## Automated runs (final, after the P07 fix)
| Suite | Editor (Play Mode) | Build headless (240 fps cap) | Build windowed (vSync, 1280×720) |
|---|---|---|---|
| EditMode: combo resolver | ✅ 6/6 | n/a | n/a |
| P01 route (Hub → Factory → Rice → demo end) | ✅ 26/0 | ✅ 26/0 | ✅ 26/0 |
| P02 absorb | ✅ 24/0 | ✅ 24/0 | ✅ 24/0 |
| P03 powers + combos | ✅ 26/0 | ✅ 26/0 | ✅ 26/0 |
| P04 puzzles + restoration | ✅ 29/0 (all 3 solved) | ✅ 29/0 | ✅ 29/0 |
| P05 progress + save (2 sessions) | ✅ 12/0 + 12/0 | not run (needs an Editor Play-session restart) | not run |
| P06 polish (Thai UI, feedback, licences) | ✅ 19/0 | not run (reads project files) | not run |
| Screenshots (Hub / Factory / Rice) | 👀 | — | 👀 Thai HUD renders |
| Player.log exceptions | — | none | none |

## Checklist by area
| Area (P07 list) | Criterion | Status / evidence |
|---|---|---|
| Player movement / camera | moves, jumps 1 m dock, camera follows, no stuck colliders on route | ✅ P01 (Editor + build) |
| Absorption | aim feedback marker, range / line-of-sight / decoy rejection, 3 absorb modes, no stuck state | ✅ P02, 👀 p02-target-feedback-fixed.png |
| Magnet / Pressure / Bounce | pull/push/mass limit/LOS, blast+cooldown, charge bounce + decaying rebounds | ✅ P03 |
| Power switching + combos | max 2, swap, 40 random switches, Magnet Cannon, Pressure Dash, Magnet+Bounce refused | ✅ P03 + EditMode |
| Puzzle completion + reset | 3 puzzles solved by real play, reset pads, out-of-world recovery, solved once | ✅ P04, 🙋 full manual completion by the user (save shows 3/3) |
| Hub unlock + transition | Rice gate locked until Factory restored, Hub stages, announcement | ✅ P05 |
| Save / load | survives Play Mode restart, corrupt / missing / version-safe, player save untouched (hash checked across all P07 runs) | ✅ P05 |
| Thai UI + audio feedback | Noto Sans Thai everywhere, Thai HUD/menus, cue counters for every event | ✅ P06, 👀 build screenshots |
| Asset import | scale / orientation / colliders / materials (no pink) | ✅ Validate, P06 materials check |
| Performance | windowed build: vSync-locked ~6.1 ms (165 Hz display), first-frame Hub hitch max 54.6 ms; Editor ~9–10 ms | ✅ |
| Non-developer playtest | real person, no help | ⏳ form: `Docs/P07_Playtest_Form_TH.md` |

## Diagnosis and fix: grounding at very high frame rates
| Run | fps | isGrounded while standing | Bounce charge | Bounce peak |
|---|---|---|---|---|
| Before fix, headless uncapped | ~14,177 | 0.26% | 0.01 | 0.19 m |
| Before fix, windowed | ~402 | 100% | 1.00 | 5.61 m |
| **After fix**, headless (240 cap) | 240 | 100% | 1.00 | 5.63 m |
| **After fix**, headless cap removed | ~16,152 | 100% | 1.00 | 0.73 m ⚠ |
| **After fix**, windowed (vSync) | ~162–165 | 100% | 1.00 | 5.61 m |

Cause: at extreme frame rates the per-frame ground-stick move is sub-millimetre, so `CharacterController.isGrounded` drops out.
Fix: (1) `BlobPlayer.Grounded` = `isGrounded` OR a short sphere probe under the feet (only while not moving up);
(2) vSync on in the PC Quality level, plus `Application.targetFrameRate = 240` as a fallback when vSync is not active.

## Known issues (P07)
1. ⚠ With the frame cap deliberately removed (~16k fps, test-only), grounding now works but the Bounce launch only reaches 0.73 m
   (expected ~5.6 m). Not reachable in the shipped game (vSync + 240 fps cap); root cause not yet investigated.
2. World signs (TextMesh, default font material) draw through walls, e.g. the turbine-room sign is visible through the closed Factory gate.
   Cosmetic; fix = depth-tested 3D-text material.
3. Non-developer playtest not done yet (needs a real person; form ready).
4. The "Absorption yard" sign slightly overlaps the Hub tree canopy from some angles (cosmetic).
5. Product / company name are Unity defaults (`AFO` / `DefaultCompany`), so saves live in `LocalLow/DefaultCompany/AFO`. Changing them is a
   project-settings decision (it also moves the save folder).
