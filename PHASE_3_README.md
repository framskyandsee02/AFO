# 🎮 BORROWED TIME: Phase 3 Implementation Guide

**Status**: ✅ Project Template Ready | ⏳ Code Templates Ready

---

## 📁 Folder Structure (Agents 1-5)

```
Assets/
├── 01_GameSystems/          (Agent 01)
│   ├── TimeGauntletController.cs
│   ├── TimeTarget.cs
│   ├── PuzzleCondition.cs
│   ├── ZoneUnlock.cs
│   └── EnergyManager.cs
├── 02_SupportSystems/       (Agent 02)
│   ├── BorrowedTimeInput.cs
│   ├── GameState.cs
│   ├── AudioCueManager.cs
│   └── RestorationController.cs (refs Agent 04)
├── 03_EnvironmentArt/       (Agent 03)
│   ├── Models/
│   ├── Materials/
│   └── [Blender exports here]
├── 04_VFX/                  (Agent 04)
│   ├── RestorationController.cs
│   ├── Particles/
│   └── Prefabs/
├── 05_UI/                   (Agent 05)
│   ├── HUDDisplay.cs
│   ├── Canvas.prefab
│   ├── MenuUI/
│   └── Prefabs/
├── Scripts_Shared/          (Utilities)
│   └── [Shared code]
└── Prefabs_Shared/          (Common prefabs)
    └── [Shared prefabs]
```

---

## 🔧 Code Templates Provided

### Agent 01: Gameplay Systems
- **TimeGauntletController.cs** - Core time state machine (Rewind/Accelerate/Normal)

### Agent 02: Support Systems
- **BorrowedTimeInput.cs** - Input handling (Q=Rewind, E=Accelerate, F=Normal, R=Interact)
- **GameState.cs** - Global state (zones, energy, progression)

### Agent 03: Environment Art
- Placeholder for Blender models → Assets/03_EnvironmentArt/Models/

### Agent 04: Cutscene/VFX
- **RestorationController.cs** - Zone restoration + particle effects

### Agent 05: UX/UI
- **HUDDisplay.cs** - Energy bar, time state, zone progress display

---

## ✅ Next Steps

1. **Copy scripts to Assets/**
   ```
   Copy all .cs files to their respective Agent folders
   ```

2. **Create Scene Template**
   - Create Assets/Scenes/MainScene.unity
   - Add TimeGauntletController prefab
   - Add GameState singleton
   - Add Canvas with HUDDisplay

3. **Initialize Git** (if not done)
   ```
   powershell.exe -ExecutionPolicy Bypass -File .\init_git.ps1
   ```

4. **Agent Assignments** (Ready for parallel work)
   - Agent 01: Expand TimeGauntletController with puzzle logic
   - Agent 02: Wire input → time control, add AudioCueManager
   - Agent 03: Create Blender models (Tree, Bridge, Environment)
   - Agent 04: Create particle systems, zone restoration VFX
   - Agent 05: Build menus, settings UI, HUD details

---

## 🎯 MVP Scope
- **Time**: 3 days parallel work
- **Content**: 1 zone (vertical slice)
- **Assets**: Low-poly stylized models
- **Features**: Time mechanics, 1 puzzle, zone unlock gate

---

**ทีม claude.ai Ready! 🚀**
