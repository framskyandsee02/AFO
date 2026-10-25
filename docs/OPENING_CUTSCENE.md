# Opening film

New Play opens `Cutscene01_Mail`, then `Cutscene02_Interior`, then the existing `TrainingArea` introduction. Continue still loads the saved gameplay scene directly.

The game starts on the main menu and waits for input. Tools / Borrowed Time / Create or Open Main Menu also sets that scene as the Unity Editor Play Mode start scene, so the Editor toolbar Play button opens the menu even when a cutscene scene is being edited. Only the main menu's Play action starts the film. Use Tools / Borrowed Time / Use Main Menu as Play Mode Start to set the same Editor entry point again.

The full sequence shows the delivery van arriving, posting a letter, and leaving; the protagonist comes outside, picks up the letter, and carries it back indoors. Inside, the protagonist reads Mr. X's letter, hears the drawer, discovers the time portal, and jumps through it into the training area.

Thai captions, cinematic framing, character expressions, walking, camera movement, fades, and procedural sound cues accompany the existing scene artwork. Sound cues use the game's Effects volume; the soundtrack from the main menu continues across scenes. Skip or Escape works throughout delivery, letter pickup, and the interior, and goes directly to TrainingArea.

Run Tools / Borrowed Time / Validate Opening Cutscene to check the complete New Play path, all nine story beats, normal gameplay time, and the three Skip paths. Validation uses a temporary save location and restores the previously open scene. Reports and screenshots are saved under `Art/BorrowedTime/Opening`.
