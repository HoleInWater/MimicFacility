# MimicFacility

![Status](https://img.shields.io/badge/Status-In%20Development-orange)
![Engine](https://img.shields.io/badge/Unreal%20Engine-5-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## Overview

**MimicFacility** is a 1–4 player co-op multiplayer horror game built in Unreal Engine 5. Players awaken as experiment subjects trapped inside a brutalist research facility overseen by an omniscient AI called *The Director*. During the first round, players explore freely and talk over voice chat — but the facility is listening. In subsequent rounds, AI-driven creatures called **Mimics** appear, using the players' own recorded voices and phrases to impersonate teammates, sow paranoia, and multiply when triggered by specific spoken words. Trust erodes. The facility tightens. Escape is uncertain.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Engine | Unreal Engine 5 (C++ & Blueprints) |
| Voice Mimic System | Claude AI API for voice analysis & Director dialogue generation |
| Networking | Unreal dedicated server / listen server hybrid |
| Voice Capture | Unreal VoIP pipeline + custom processing subsystem |
| Build | Unreal Build Tool (UBT) |

## Project Structure

```
MimicFacility/
├── Config/              # Engine, game, and input configuration
├── Content/             # Blueprints, maps, audio, FX (Unreal assets)
│   ├── Blueprints/      # Character, AI, GameMode, UI blueprints
│   ├── Maps/            # Level maps
│   ├── Audio/           # Director and Mimic audio assets
│   └── FX/              # Visual effects
├── Source/              # C++ source code
│   └── MimicFacility/
│       ├── Characters/  # Player and Mimic character classes
│       ├── AI/          # Director AI, Mimic AI controller, voice learning
│       ├── GameModes/   # Game mode and round management
│       ├── Networking/  # Game state and player state replication
│       ├── Gear/        # Player equipment (flashlight, scanner, etc.)
│       └── UI/          # HUD and widget classes
├── Docs/                # Design docs, art direction, open questions
├── GDD.md               # Game Design Document
├── CONTRIBUTING.md       # Contribution guidelines
└── LICENSE               # MIT License
```

## Getting Started

1. Install Unreal Engine 5.4+ via the Epic Games Launcher
2. Clone this repository
3. Right-click `MimicFacility.uproject` → "Generate Visual Studio project files"
4. Open the `.sln` and build (Development Editor, Win64)
5. Open the project in Unreal Editor

## License

This project is licensed under the MIT License — see [LICENSE](LICENSE) for details.
