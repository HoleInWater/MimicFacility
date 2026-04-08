# MimicFacility — Setup & Testing Guide

## Prerequisites

- **Unity 6** (6000.0 or newer) installed via Unity Hub
- **Git** installed
- **Ollama** (optional, for AI Director dialogue) — https://ollama.ai

---

## Step 1: Clone & Open

```bash
git clone https://github.com/HoleInWater/MimicFacility.git
```

1. Open **Unity Hub**
2. Click **Add** → **Add project from disk**
3. Select the cloned `MimicFacility` folder
4. Unity will import all assets and compile scripts (first time takes a few minutes)

**If you get compile errors after opening:**
- Close Unity
- Delete the `Library/` folder inside the project
- Reopen — Unity will regenerate everything

---

## Step 2: Verify Mirror is Installed

Mirror Networking should already be in `Assets/Mirror/`. If it's missing:

1. Download Mirror from the Unity Asset Store: https://assetstore.unity.com/packages/tools/network/mirror-129321
2. Import it into the project

---

## Step 3: Create a Test Scene

### Option A: Automatic (Recommended)

1. In Unity, go to **File → New Scene → Basic (Built-in)**
2. Save it as `Assets/Scenes/TestScene.unity`
3. Delete the default **Directional Light** (the map generator creates its own lights)
4. Create an **empty GameObject**, name it `Bootstrap`
5. Add the **TestSceneBootstrap** component to it (search for it in Add Component)
6. Press **Play**

The bootstrap will automatically:
- Generate a 6-room facility with corridors, doors, lights, spore vents, and terminals
- Spawn you as a player with camera, flashlight, and movement
- Spawn the Director AI with LLM client, corruption tracker, and all subsystems
- Spawn 2 mimics, 1 stalker, 1 fraud, and 1 phantom
- Place gear pickups around the map
- Set up fog and ambient lighting
- Show a debug overlay with game state

### Option B: Manual

1. Create a new scene
2. Add a **Plane** (scale 10,10,10) as floor
3. Add some **Cube** walls
4. Bake the NavMesh: **Window → AI → Navigation → Bake**
5. Create empty GameObjects and add these components:
   - `GameManager`
   - `SettingsManager`
   - `FallbackInputManager`
   - `RoundManager`
   - `NetworkedGameState`
6. Create a player GameObject with:
   - `CharacterController` (height 1.8, radius 0.3)
   - Child Camera at (0, 1.6, 0)
   - `AudioSource`
   - `AudioListener` (on the camera)
   - `PlayerCharacter`
   - `MimicPlayerState`
   - Tag it as "Player"

---

## Step 4: Controls

| Key | Action |
|-----|--------|
| WASD | Move |
| Mouse | Look around |
| Space | Jump |
| Shift | Sprint (only when being chased — 6th sense) |
| E | Interact (doors, terminals, gear pickup) |
| Left Click | Use equipped gear |
| F | Toggle flashlight |
| V (hold) | Push-to-talk |
| Escape | Pause |

---

## Step 5: Testing Individual Systems

### Director AI (without Ollama)
The Director works without Ollama installed — it falls back to handcoded dialogue lines per phase. To test:
1. Find the `DirectorAI` object in the hierarchy
2. In the Inspector, observe `CurrentPhase`
3. The Director evaluates game state every 10 seconds and transitions phases

### Director AI (with Ollama)
1. Install Ollama: https://ollama.ai
2. Open a terminal and run:
   ```bash
   ollama pull phi3
   ```
3. Ollama auto-starts on `localhost:11434`
4. Play the scene — the Director will generate real LLM dialogue

### Corruption System
1. Find `CorruptionTracker` in the hierarchy
2. In the Inspector, see `CorruptionIndex` (0-100)
3. To test manually, add this to any script:
   ```csharp
   FindObjectOfType<CorruptionTracker>().ProcessEvent(ECorruptionEvent.PlayerMockedDirector);
   ```
4. Watch the index climb and phase transitions fire at 25/50/75

### Entities
- **Red capsules** = Mimics (patrol on NavMesh, impersonate voices)
- **Black capsules** = Stalkers (follow you, freeze when you look)
- **Yellow capsules** = Frauds (copy your body, wave, then chase)
- **Blue capsules** = Phantoms (invisible, project fake sounds)

### Gear
- **White cubes** = Flashlights
- **Cyan cubes** = Audio Scanners (scan entities, check waveform integrity)
- **Red cubes** = Containment Devices (single use, captures mimics or stuns players)
- **Magenta cubes** = Signal Jammers (blocks voice in radius)

### Facility Controls
- **Doors**: Walk up and press E. Some are locked (brown colored)
- **Lights**: Controlled by Director/Warden. Watch for flicker events
- **Spore Vents**: Green particle effects. Standing in them increases spore exposure
- **Terminals**: Black rectangles on walls with green glow. Press E to access lore

### Proximity Voice Chat
- Hold V to talk
- Voice volume decreases with distance (full at 5m, silent at 30m)
- Walls reduce volume to 30%
- Echoes generate based on room size

---

## Step 6: Testing Multiplayer

1. **Build the project**: File → Build Settings → Build
2. Run one instance as **Host**: the built .exe
3. Run another instance in the **Unity Editor** as Client
4. In the built instance, use Mirror's NetworkManagerHUD to **Host**
5. In the editor, enter the IP and **Connect**
6. Both players should appear in the facility

---

## Step 7: Bootstrap Settings

The `TestSceneBootstrap` component has these toggles in the Inspector:

| Setting | Default | What it does |
|---------|---------|-------------|
| Generate Map | true | Builds the procedural facility |
| Spawn Player | true | Creates your player character |
| Spawn Director | true | Creates the Director AI + all subsystems |
| Spawn Entities | true | Places mimics, stalkers, frauds, phantoms |
| Spawn Gear | true | Places gear pickups in rooms |
| Setup UI | true | Creates the UI canvas |
| Setup Audio | true | Creates spatial audio processor, sets fog |
| Room Count | 6 | Number of rooms in the facility |
| Mimic Count | 2 | Number of voice-copying mimics |
| Stalker Count | 1 | Number of freeze-when-looked-at stalkers |
| Fraud Count | 1 | Number of body-copying frauds |
| Phantom Count | 1 | Number of sound-projecting phantoms |
| Show Debug Info | true | Green overlay showing game state |

---

## Troubleshooting

**"Assembly-CSharp-Editor" error:**
- Delete the `Library/` folder, reopen Unity

**Entities don't move:**
- NavMesh needs to be baked. The map generator does this automatically, but if you built a manual scene: Window → AI → Navigation → Bake

**No sound:**
- Check AudioListener exists (should be on the player camera)
- Check AudioSource.spatialBlend is set to 1.0 for 3D sounds

**Mirror errors about SyncVar:**
- Make sure Mirror is in `Assets/Mirror/`, not installed via package manager

**Input not working:**
- Make sure `FallbackInputManager` is in the scene (Bootstrap adds it automatically)
- Check Cursor.lockState is Locked (should happen on play)

**Director says nothing:**
- Without Ollama: fallback lines display in console. Check `DirectorAI.GetFallbackLines()`
- With Ollama: make sure `ollama serve` is running and `phi3` model is pulled
