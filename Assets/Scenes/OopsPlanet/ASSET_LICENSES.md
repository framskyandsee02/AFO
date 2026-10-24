# OOPS! PLANET — Asset sources & licences

Everything that ships with the prototype, and where it comes from. Nothing with an unknown licence is used.

| Asset | Location | Source | Licence |
|---|---|---|---|
| Noto Sans Thai (variable TTF) | `Assets/OopsPlanet/Fonts/NotoSansThai-Variable.ttf` | Google Fonts repo `ofl/notosansthai` (https://github.com/google/fonts), downloaded 2026-10-23, 218,652 bytes | SIL Open Font License 1.1 — full text in `Assets/OopsPlanet/Fonts/OFL.txt` (may be bundled and redistributed with the game; may not be sold on its own) |
| 3D models (BLOB, props, puzzle pieces, Hub tree, factory/rice dressing) | `ArtSource/Blender/*.blend` → `Assets/OopsPlanet/Models/*.fbx` | Made for this project in Blender (scripted via Blender MCP) | Project-owned |
| Materials | `Assets/OopsPlanet/Materials` | Created by the scene builder (URP Lit / Particles Unlit) | Project-owned |
| All sound (BLOB chirps, power SFX, puzzle jingles, zone ambience) | generated at runtime by `Scripts/Audio/ProcAudio.cs` | Procedural synthesis in code; no audio files | Project-owned |
| VFX | `Scripts/Feedback/VfxKit.cs` + `Materials/VFX particle.mat` | Code-driven particle bursts | Project-owned |
| URP settings, default volume profile, Input System actions | `Assets/Settings`, `Assets/InputSystem_Actions.inputactions` | Unity project template | Unity Companion License (Unity-provided) |
| `Assets/TutorialInfo` | Unity template readme | Unity project template | Unity Companion License (not used in the game) |
| Legacy Wobble Works greybox (retired) | `Assets/WobbleWorks/Models`, `Prefabs`, `Materials`, `Scenes` | Made earlier for this project | Project-owned (not in the build) |

Checked for P06: no third-party models, textures or audio; the only external file is the OFL font above.
