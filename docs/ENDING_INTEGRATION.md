# Ending replacement

Factory completion now loads `Assets/_BorrowedTime/Scenes/EndingCutscene.unity`, the 76-second 3D ending from commit `e88b1d6c6`. Both factory ending variants use this film, and the selected result remains in BTJourney and the completed factory save. The existing recovery montage is bypassed when the new scene is available.

The completed factory is saved before the scene changes. The new film uses its supplied `komorebi no nakade` music and rebuilt SFX. Music and Effects settings still apply, and the persistent menu soundtrack pauses until the player returns to the main menu.

Escape, Skip, or F8 moves to the final credits. After playback, Replay restarts the film and Main Menu returns to the existing menu. Opening Play and Continue retain their existing routes.

Tools / Borrowed Time / Validate Ending Integration checks the real completion transition for both variants, the completed save, the full audio timeline, Replay, Skip, F8, and returning to the menu. It uses a temporary save file and restores the previously open scene. Evidence is stored in `Art/BorrowedTime/EndingIntegration`.
