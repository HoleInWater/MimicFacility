# MimicFacility

![Status](https://img.shields.io/badge/Status-In%20Development-orange)
![Engine](https://img.shields.io/badge/Unity_6-6000.0+-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## Overview

**MimicFacility** is a 1–4 player co-op multiplayer horror game built in Unity. Players awaken as experiment subjects trapped inside a brutalist research facility overseen by an omniscient AI called *The Director*. During the first round, players explore freely and talk over voice chat — but the facility is listening. In subsequent rounds, AI-driven creatures called **Mimics** appear, using the players' own recorded voices and phrases to impersonate teammates, sow paranoia, and multiply when triggered by specific spoken words. Trust erodes. The facility tightens. Escape is uncertain.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Engine | Unity 6 (6000.0+) (C#) |
| AI / Director Dialogue | Local LLM via Ollama sidecar (Phi-3 Mini, TinyLlama, Qwen 2.5) |
| Voice Cloning | Chatterbox (Apache 2.0) local sidecar |
| Director TTS | Piper TTS (MIT, CPU-only) |
| Networking | Mirror Networking (MIT) with KCP Transport |
| Voice Capture | Unity Microphone API + custom processing |
| Spatial Audio | Custom 3D audio equation (Mimix) |

## Project Structure

```
MimicFacility/
├── Assets/
│   ├── Scripts/           # C# source code
│   │   ├── Core/          # GameManager, RoundManager, Input, Settings
│   │   ├── AI/            # Director AI, LLM client, weapons, persistence
│   │   │   ├── Director/  # DirectorAI, PromptBuilder
│   │   │   ├── LLM/       # OllamaClient
│   │   │   ├── Weapons/   # PersonalWeaponSystem
│   │   │   ├── Persistence/ # CorruptionTracker, DirectorMemory
│   │   │   ├── Voice/     # VoiceCloneClient, VoiceLearningSystem
│   │   │   ├── Dialogue/  # MimicDialogueManager
│   │   │   └── Controller/ # MimicAIController, MimicStateMachine
│   │   ├── Characters/    # Player and Mimic character classes
│   │   ├── Gameplay/      # AccusationManager, DiagnosticTasks, Tutorial
│   │   ├── Gear/          # Equipment (flashlight, scanner, jammer, etc.)
│   │   ├── Facility/      # Door, Light, SporeVent, Terminal, Environment
│   │   ├── Horror/        # Device horror tricks (meta-horror layer)
│   │   ├── Effects/       # Hallucination, PostProcess
│   │   ├── Audio/         # SpatialAudioProcessor, VoiceChat
│   │   ├── Lore/          # LoreDatabase, LoreEntry
│   │   ├── Networking/    # MimicNetworkManager, LobbyManager
│   │   └── UI/            # HUD, MainMenu, Accusation, Terminal, Settings
│   ├── Prefabs/           # Character, Gear, Facility, UI prefabs
│   ├── Scenes/            # Game scenes (MainMenu, Lobby, Facility)
│   ├── Materials/         # Shaders and materials
│   ├── Audio/             # SFX, Music, Voice assets
│   ├── Data/              # JSON data files (lore entries, etc.)
│   └── Resources/         # Runtime-loaded assets
├── ProjectSettings/       # Unity project settings
├── Packages/              # Unity package manifest
├── GDD.md                 # Game Design Document
├── AI_ARCHITECTURE.md     # AI Systems Architecture
├── DIRECTOR_DESIGN.md     # Director Behavioral Design
├── CREATIVE_MANIFESTO.md  # Creative Philosophy
├── OPEN_DESIGN_ANSWERS.md # Resolved Design Questions
├── PSYCHOLOGY_FRAMEWORK.md # 12 Theories Mapped to Systems
├── CONTRIBUTING.md        # Contribution guidelines
└── LICENSE                # MIT License
```

## Getting Started

1. Install **Unity 6 (6000.0)** or newer via Unity Hub
2. Clone this repository
3. Open the project folder in Unity Hub → "Add project from disk"
4. Wait for Unity to import all assets and compile scripts
5. Open the test scene from `Assets/Scenes/`

### Optional: LLM Integration

For Director AI dialogue via local LLM:

1. Install [Ollama](https://ollama.ai)
2. Pull a model: `ollama pull phi3`
3. Ollama runs at `localhost:11434` — the game connects automatically

### Optional: Voice Cloning

For mimic voice synthesis:

1. Set up [Chatterbox](https://github.com/resemble-ai/chatterbox) locally
2. Run the server on `localhost:8100`

## Dependencies

| Package | Purpose | License |
|---------|---------|---------|
| [Mirror](https://github.com/MirrorNetworking/Mirror) | Multiplayer networking | MIT |
| [TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest) | UI text rendering | Unity |
| [Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest) | New input handling | Unity |
| [Post Processing](https://docs.unity3d.com/Packages/com.unity.postprocessing@latest) | Visual effects | Unity |

## License

This project is licensed under the MIT License — see [LICENSE](LICENSE) for details.
