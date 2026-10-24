# Sleeping companions in the meadow menu

Open `Assets/Scenes/BorrowedTimeMainMenu.unity` and enter Play Mode.

- The supplied Protagonist and Robo FBX models are placed together on the existing 3D grass. The human rests with one hand behind the head and the other across the body; both characters breathe gently.
- The human starts with the actual `Blink` blend shape at 100%. Hovering Play, Settings, Exit, or Back eases that blend shape to 0%, opening the eyes. Leaving the button closes them again. Robo retains its `Sleep` expression.
- The pair scales to the visible meadow width so narrower windows keep the buttons clear.
- Materials and model GUIDs were preserved from `C:/Users/Pc/Downloads/Characters/Models` and `Materials`. The FBX files were copied without modification; unrelated XNB files and showcase scripts were not imported.

`SleepingCompanions` contains the runtime behavior. `SleepingCompanionsSetup` installs the posed instances into the existing character anchor. The Tools / Borrowed Time / Install Sleeping Companions menu item can rebuild the pair; it replaces only its own sleeping-companions object.

`Validation.json` records the Play Mode checks. The PNGs show closed eyes, eyes opened by a UI Toolkit pointer event, return to sleep, and a portrait layout. Validation renders the actual imported meshes and current menu to a render texture; it does not use painted stand-ins.

The scene before this change is retained locally in `Backups/SleepingCompanions/BorrowedTimeMainMenu.before.unity`.
