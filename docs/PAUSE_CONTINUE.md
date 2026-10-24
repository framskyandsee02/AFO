# Pause and Continue

Press Escape in a BTPlayer gameplay scene to freeze scaled game time, stop gameplay input, unlock the cursor and open the glass menu. Resume restores the previous time scale, cursor state and existing player pause state. Escape in Settings returns to the pause menu; Escape on the pause menu resumes gameplay. Legacy HUD/pause panels are hidden while this overlay is active.

Quit in the pause menu saves through BTSaveGame and returns to BorrowedTimeMainMenu. It does not exit the application. The main menu's Exit action retains application exit behavior.

Continue on the main menu loads the existing version-1 save, including its scene, checkpoint, energy and puzzle state. It is disabled for missing, malformed, unsupported or incomplete saves. New Play starts a fresh journey without immediately deleting the previous save; subsequent normal gameplay saves replace it.

The runtime pause document uses the existing menu UXML, glass artwork, Settings controls and audio preferences. `Resources/BorrowedTimeMenuUi.asset` holds the UI references so opening TrainingArea, ForgottenCity or FactoryInterior directly in Unity also supports pausing. Tools / Borrowed Time / Install Pause Menu Configuration can recreate that asset.

Validation is dispatched through `Temp/PauseContinue.request.txt` with `validate`. It uses an editor-only save-path override under Temp, so the player's real save is not modified. Reports and captures are in `Art/BorrowedTime/PauseContinue`. The existing menu-layout validator now includes the fourth Continue button.
