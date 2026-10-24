# Vrzo main-menu integration

Integrated on 2026-10-25 (Asia/Bangkok).

The main menu from `Vrzo` commit `a5fe102d4` is integrated with the existing game on `main` (`afed14b8f`). The selected menu assets were transferred without applying the legacy gameplay deletions on Vrzo.

## Entry point

Open `Assets/Scenes/BorrowedTimeMainMenu.unity`. Play opens `Assets/_BorrowedTime/Scenes/TrainingArea.unity`. Build Settings also retain ForgottenCity, FactoryInterior, FunPrototype and MainScene. FunPrototype is included because the existing introduction offers a button that loads it.

## Shared characters

The menu reuses `Assets/_BorrowedTime/Characters/Models/Protagonist.fbx`, `Robo.fbx` and their existing materials. The FBX blobs are identical to those on Vrzo. Reusing these assets preserves 27 existing GUIDs and avoids duplicate model/material imports. The sleeping-companions installer points at the shared character folder.

## Menu behavior

- The human and Robo lie together on the meadow. Hovering a menu button opens the human's eyes; leaving it closes them.
- `sokoniai wo hitohira.mp3` loops and continues into TrainingArea.
- The soft pop plays on button hover/click and title activation. Music, Effects, Master and Mute retain independent routing.
- Menu validation uses the configured gameplay destination instead of the old Start placeholder.

The committed Build Settings conflict markers were resolved. The orphaned OopsPlanet authoring tool is conditional on `OOPS_PLANET_RUNTIME`; its missing runtime classes do not block the Borrowed Time editor assembly.

## Verification

- Runtime and editor assemblies compiled in Unity 6000.3.25f1.
- Audio/navigation validation: 16 checks passed, including loading TrainingArea, continuous music across scene changes, title-click audio and reconnecting button feedback on return.
- Character validation: 17 checks passed, including ground placement, actual eyelid blend shapes, pointer transitions, materials and portrait framing.
- Build scene paths and GUIDs match existing scene assets.
- Closed-eye and open-eye render captures were visually checked against the menu from Vrzo.

Reports and render captures are under `Art/BorrowedTime/Audio` and `Art/BorrowedTime/SleepingCompanions`.
