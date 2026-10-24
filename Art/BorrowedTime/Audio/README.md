# Menu soundtrack and button sounds

- `sokoniai wo hitohira.mp3` plays as a looping 2D soundtrack at a base volume of 50%. The existing Music and Master controls apply. A single persistent audio host keeps the song playing when Play opens Start and avoids duplicate music when returning to the menu.
- `oxidvideos-soft-pop-sfx-433612.mp3` plays once on entering Play, Settings, Exit, or Back, and on activating those buttons with the mouse or keyboard. Activating the game title plays the same click sound along with its existing bounce. Hover uses 35% loudness and a slightly brighter pitch; clicks use 65%. Both use the existing Effects and Master controls.
- The soundtrack streams from disk. The 0.192-second pop is preloaded as mono PCM for responsive UI playback.
- Both MP3 files were copied intact from `C:/Users/Pc/Music/`.

The scene stores the clips and audio sources on `Menu Soundtrack and Button Audio`. `MenuAudioFeedback` handles playback and UI event subscriptions; `MenuAudioSetup` can recreate the audio host through Tools / Borrowed Time / Install Soundtrack and Button Sounds.

`Validation.json` records checks of decoded audio output, mouse/keyboard button activation, channel volumes, mute, and continuous single-instance music across scene transitions. Validation restores the user's saved audio preferences.
