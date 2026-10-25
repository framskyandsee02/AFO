# Sky City handoff — 2026-10-25

Open `Assets/_BorrowedTime/Scenes/ForgottenCity.unity` to test the new city directly.

The city's sequence is water works -> clock transmission -> bridge control bay -> factory entrance. The bridge must finish docking before the factory door opens. Scroll down drains/ages; scroll up gives/restores. H provides Thai hints; R restarts this city; F enters the factory after the door opens.

There are 41 additional time-interactable props: benches, crates, streetlights and shrubs. Intermediate condition persists. Draining at a full meter still ages objects and discards the surplus.

The latest menu, training and factory work already on main is retained. Five existing shared gameplay implementations are retained from main rather than replaced by the older local project copies. The main-menu entry point and Build Settings remain as on main.

The city dependency closure, Unity `.meta` GUIDs, new models/materials and editor checks are included. Editable Blender scenes and authoring scripts are in `ArtSource/BorrowedTime/`; preview captures and the ageing GIF are in `Art/BorrowedTime/SkyCity/`. Change authoring-script export paths for another workstation before running them.

See `SKY_CITY_CAUSAL_CHAIN.md` for conditions, saves, implementation and verification. The source Unity project passed causal-chain/age-transfer and bridge checks, live gate/ramp/bridge traversal, real input-handler transfer and persistence reload checks. The combined checkout was checked for dependency completeness and GUID consistency; no new standalone build was produced for publication.
