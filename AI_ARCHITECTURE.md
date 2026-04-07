# MimicFacility — AI Systems Architecture

**Version:** 1.0.0
**Last Updated:** 2026-04-06
**Status:** Pre-Production Research & Planning

> **CRITICAL CONSTRAINT:** This game has zero budget. Every AI system — LLM, voice cloning, TTS — runs entirely on the player's own hardware. No cloud APIs. No paid services. No dedicated servers. Everything is local or peer-to-peer.

---

## Table of Contents

1. [Research Report](#1-research-report)
   - [1A. Local LLM Selection](#1a-local-llm-selection)
   - [1B. Local Voice Cloning Selection](#1b-local-voice-cloning-selection)
   - [1C. UE5 Integration Layer](#1c-ue5-integration-layer)
   - [1D. Peer-to-Peer Multiplayer](#1d-peer-to-peer-multiplayer-no-dedicated-server)
2. [System Architecture Diagram](#2-system-architecture-diagram)
3. [Implementation Roadmap](#3-implementation-roadmap)
4. [Hardware Tier Guide](#4-hardware-tier-guide)
5. [Prompt Engineering](#5-prompt-engineering)

---

## 1. Research Report

### 1A. Local LLM Selection

**Inference Backend: llama.cpp**

| | |
|---|---|
| **Repo** | `https://github.com/ggml-org/llama.cpp` |
| **License** | MIT |
| **What it does** | C/C++ inference engine for GGUF-format LLMs. No Python runtime. Supports CUDA, Vulkan, Metal, CPU fallback. Partial GPU offloading (split layers between CPU and GPU), KV cache quantization, grammar-constrained output, OpenAI-compatible HTTP server (`llama-server`), and a shared library (`libllama`) that can be embedded directly into a C++ application — including UE5 plugins. |
| **Why it fits** | Native C++, zero Python dependency, partial offload lets us share VRAM with UE5 rendering, and the HTTP server mode gives us the Ollama sidecar option. |

#### Model Comparison (Q4_K_M Quantization)

| Model | Params | VRAM (full offload) | Context Window | License | tok/s RTX 3060 (6GB) | tok/s RTX 4060 (8GB) | Horror Dialogue (1-5) |
|---|---|---|---|---|---|---|---|
| **TinyLlama 1.1B** | 1.1B | ~0.8 GB | 2,048 | Apache 2.0 | 80–120 | 100–150 | 2 |
| **Phi-3 Mini 3.8B** | 3.8B | ~2.4 GB | 4,096 | MIT | 40–60 | 55–80 | 4 |
| **Mistral 7B v0.3** | 7.3B | ~4.4 GB | 8,192 | Apache 2.0 | 15–25 (partial) | 30–45 | 4 |
| **Qwen 2.5 7B** | 7.6B | ~4.7 GB | 32,768 | Apache 2.0 | 12–22 (partial) | 28–40 | 5 |
| **Llama 3.1 8B** | 8.0B | ~4.9 GB | 128,000 | Llama Community | 10–20 (partial) | 25–40 | 5 |

**VRAM download links (HuggingFace GGUF):**
- TinyLlama 1.1B: `https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF`
- Phi-3 Mini 3.8B: `https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf`
- Mistral 7B: `https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF`
- Qwen 2.5 7B: `https://huggingface.co/Qwen/Qwen2.5-7B-Instruct-GGUF`
- Llama 3.1 8B: `https://huggingface.co/meta-llama/Llama-3.1-8B-Instruct-GGUF`

#### VRAM Budget Reality

UE5 consumes 2–4+ GB VRAM for rendering depending on scene complexity. On a 6GB RTX 3060, that leaves 2–4 GB for the LLM:

- **7–8B models will NOT fully fit in GPU alongside UE5 on a 6GB card.** You must use partial offload (e.g., 15–20 of ~32 layers on GPU, rest on CPU), which roughly halves throughput.
- **TinyLlama and Phi-3 Mini fit comfortably** alongside UE5 on both GPUs.
- On the 8GB RTX 4060, 7B models can fit with tight margins if UE5 rendering is modest.

#### Latency Analysis for Game AI

Our responses need to be **under 30 words** (Director) or **under 15 words** (Mimic). At ~1.3 tokens per word, that's 20–40 tokens of output. Required generation time: **under 2 seconds** including prompt processing.

| Model | Tokens needed | Time on 3060 | Time on 4060 | Verdict |
|---|---|---|---|---|
| TinyLlama 1.1B | 40 | ~0.4s | ~0.3s | Fast but dumb |
| Phi-3 Mini 3.8B | 40 | ~0.8s | ~0.6s | **Sweet spot** |
| Mistral 7B | 40 | ~2.0s (partial) | ~1.0s | Marginal on 3060 |
| Qwen 2.5 7B | 40 | ~2.5s (partial) | ~1.2s | Too slow on 3060 |

#### Recommendation

| Role | Model | Rationale |
|---|---|---|
| **Primary (Tier 2+)** | **Phi-3 Mini 3.8B** (MIT) | Best balance of intelligence, VRAM (2.4 GB), and speed. Fits alongside UE5 on 6GB cards. MIT licensed. Generates convincing short-form dialogue. |
| **Low-spec fallback (Tier 1)** | **TinyLlama 1.1B** (Apache 2.0) | 0.8 GB VRAM. Runs on CPU-only machines at acceptable speed. Quality is noticeably lower — Director will sound more generic, Mimic will be less convincing. |
| **High-end option (Tier 3)** | **Qwen 2.5 7B** (Apache 2.0) | Best raw dialogue quality. Apache 2.0. Only viable on 12GB+ VRAM cards where it can fully offload alongside UE5. |

**Also consider:** Qwen 2.5 3B (~2 GB Q4_K_M, Apache 2.0) as a middle ground between Phi-3 and TinyLlama.

**License note:** Llama 3.1 8B uses the Llama Community License, which requires attribution and has a 700M MAU cap. Not truly open source. **We exclude it** in favor of Apache 2.0 / MIT alternatives.

---

### 1B. Local Voice Cloning Selection

#### Requirements

- Clone a player's voice from **~30 seconds** of reference audio captured during Round 1
- Generate speech in that voice from text input (output of the LLM brain)
- Run **locally** on the player's machine alongside a game engine AND a local LLM
- Latency: **under 2 seconds** from text input to audio output start
- License: **MIT or Apache 2.0** (open-source game requirement)
- VRAM budget: **1–2 GB** (remainder after UE5 + LLM)

#### Tool Comparison

| Tool | Repo | License | VRAM | Latency | Clone Quality (30s ref) | Can co-run with Game+LLM | Rating |
|---|---|---|---|---|---|---|---|
| **Chatterbox** | `https://github.com/resemble-ai/chatterbox` | Apache 2.0 | ~2 GB | ~1–2s | High — designed for few-second reference audio | Yes (careful VRAM mgmt) | **5** |
| **OpenVoice v2** | `https://github.com/myshell-ai/OpenVoice` | MIT | ~1 GB | ~1–3s | Medium — tone cloning is good, but less natural prosody | Yes | 4 |
| **Coqui TTS (XTTS v2)** | `https://github.com/coqui-ai/TTS` | AGPL (model weights) | ~2–4 GB | ~2–4s | High — excellent voice similarity | Tight on 6GB cards | 3 |
| **F5-TTS** | `https://github.com/SWivid/F5-TTS` | CC-BY-NC-SA | ~2 GB | ~2–3s | High — zero-shot, research quality | N/A | **EXCLUDED** |
| **Piper TTS** | `https://github.com/rhasspy/piper` | MIT | CPU only (~0 GB) | ~0.1s | None — fixed voices, no cloning | Yes | 2 |
| **RealtimeTTS** | `https://github.com/KoljaB/RealtimeTTS` | MIT (framework) | Depends on engine | Streaming | Framework — wraps Coqui, Piper, etc. | Yes | 3 |

#### Detailed Analysis

**Chatterbox (resemble-ai)** — RECOMMENDED
- Released 2025 by Resemble AI, purpose-built for voice cloning from short reference audio
- Apache 2.0 license — fully compatible with open-source game distribution
- ~2 GB VRAM for the model; can run on CPU with degraded speed
- Supports streaming output, meaning audio can start playing before full generation completes
- Quality from 30 seconds of reference audio is high — captures speaker identity, pitch, and cadence
- Python-based, but can be wrapped as a sidecar process with a simple socket/pipe interface to UE5
- **Risk:** Newer project — API may change. Pin to a specific release.

**OpenVoice v2 (MyShell)** — BACKUP OPTION
- MIT licensed, low VRAM (~1 GB), instant tone transfer
- Architecture: uses a base TTS model + a tone converter. The tone converter clones the voice characteristics.
- Good for "sounds like the player" but less natural than Chatterbox for conversational speech
- Simpler to deploy — smaller model footprint
- **Best for Tier 1 (low-spec) hardware** where Chatterbox is too heavy

**Coqui TTS / XTTS v2** — LICENSE PROBLEM
- Excellent voice cloning quality, but the XTTS v2 model weights are **MPL-2.0** (code) with model weights under restrictive terms
- Coqui AI (the company) shut down in late 2023. Repo is community-maintained with minimal updates.
- VRAM usage (3–5 GB) is too high for our budget alongside LLM + UE5
- **Excluded** due to unmaintained status and VRAM footprint

**F5-TTS** — LICENSE PROBLEM
- CC-BY-NC-SA — non-commercial license. **Cannot be used in a distributable game**, even a free one, as "non-commercial" is ambiguous for game distribution.
- **Excluded.**

**Piper TTS** — NO CLONING
- Extremely fast, runs on CPU, MIT licensed — but only produces fixed voices
- **Use case:** Fallback TTS for The Director on Tier 1 hardware where voice cloning is disabled. Director uses a pre-trained "clinical AI voice" from Piper's voice library instead of cloning.

**RealtimeTTS** — FRAMEWORK ONLY
- Not a TTS engine itself — it's a Python wrapper that provides streaming and manages engines
- Useful as a development tool for testing different backends, but adds unnecessary complexity in production
- We'll build our own lightweight sidecar instead

#### Recommendation

| Role | Tool | Rationale |
|---|---|---|
| **Primary voice cloner** | **Chatterbox** (Apache 2.0) | Best quality from 30s reference, designed for this exact use case, permissive license |
| **Low-spec fallback** | **OpenVoice v2** (MIT) | Lower VRAM, still provides voice tone matching, MIT license |
| **Director fixed voice** | **Piper TTS** (MIT) | CPU-only fallback for Director on Tier 1 hardware. Pre-trained voice, no cloning needed |
| **Streaming orchestration** | **RealtimeTTS** (`https://github.com/KoljaB/RealtimeTTS`, MIT) | Middleware that streams LLM output token-by-token into TTS with automatic sentence boundary detection. Minimizes perceived latency by starting audio playback before the full LLM response is complete. Wraps Chatterbox/Piper as interchangeable engines. |

#### Voice Cloning Pipeline

```
Round 1: Player speaks over voice chat
    ↓
VoiceLearningSubsystem captures 30s of clean audio per player
    ↓
Audio saved as WAV (16kHz, mono) to temp session storage
    ↓
End of Round 1: WAV files sent to Chatterbox sidecar process
    ↓
Chatterbox loads each WAV as a reference speaker profile
    ↓
Round 2+: LLM generates text → Chatterbox synthesizes in cloned voice
    ↓
PCM audio returned to UE5 → played through Mimic's AudioComponent
```

---

### 1C. UE5 Integration Layer

#### Plugin Comparison

| Plugin | Repo | Approach | Pros | Cons |
|---|---|---|---|---|
| **Llama-Unreal** | `https://github.com/getnamo/Llama-Unreal` | Embeds `libllama` directly in UE5 process | Lowest latency (~0ms IPC overhead), single process, Blueprint nodes | GPU resource contention — llama and UE5 renderer share CUDA context. Model crash = game crash. Harder to update llama.cpp independently. |
| **Ollama (sidecar)** | `https://github.com/ollama/ollama` | Separate process, REST API at `localhost:11434` | Crash isolation, easy model swapping, managed VRAM, automatic model loading/unloading, mature project | ~1–5ms HTTP overhead per request, requires Ollama installed separately or bundled, second process to manage |
| **unreal-ollama** | `https://github.com/MuddyTerrain/unreal-ollama` | UE5 plugin that wraps Ollama's REST API | Simplifies Blueprint integration with Ollama | Thin wrapper — limited community, depends on Ollama |
| **AIChatPlus** | Unreal Fab Marketplace | Commercial plugin, supports llama.cpp offline | Polished UI, multiple backend support | Paid marketplace plugin — conflicts with our zero-budget constraint. Also less transparent than open-source options. |

#### In-Process vs Sidecar: The Tradeoff

| Factor | In-Process (Llama-Unreal) | Sidecar (Ollama) |
|---|---|---|
| **Latency** | Lowest — direct function call | +1–5ms HTTP roundtrip (negligible for our 1–2s generation time) |
| **VRAM management** | Must manually coordinate with UE5 renderer. Risk of OOM crashes. | Ollama manages its own VRAM allocation. Can unload models when not needed. |
| **Crash isolation** | LLM crash = game crash | LLM crash = reconnect and retry. Game stays running. |
| **Deployment** | Single executable, model files bundled | Must ship Ollama or a custom `llama-server` alongside the game |
| **Development speed** | Requires C++ plugin development, rebuild UE5 to update | Test models via CLI, swap models without recompiling, REST API is language-agnostic |
| **GPU contention** | llama.cpp and UE5 fight over the same CUDA context. Frame drops during inference. | Separate process, separate CUDA context. OS mediates GPU sharing. Less frame impact. |

#### Recommendation: Ollama Sidecar

**Use Ollama as a sidecar process** launched by the game at startup. Reasons:

1. **Crash isolation is critical for a horror game.** A crash during a tense moment destroys the experience. Sidecar means the game keeps running even if the LLM hits an edge case.

2. **GPU resource contention is the #1 risk.** UE5's renderer and llama.cpp's CUDA kernels sharing a CUDA context causes frame stutters. Separate processes = separate CUDA contexts = the OS GPU scheduler mediates access more gracefully.

3. **Development velocity.** We can test and swap models via `ollama run phi3` without touching C++ or recompiling. REST API is trivial to call from UE5 via `FHttpModule`.

4. **Ollama handles model lifecycle.** It loads models on first request, unloads after timeout, manages VRAM automatically. We don't have to build any of this.

**Integration architecture:**

```
Game Launch
    ↓
UE5 spawns Ollama as a child process (or detects existing instance)
    ↓
Ollama listens on localhost:11434
    ↓
UE5 sends HTTP POST to /api/generate with system prompt + game context
    ↓
Ollama streams tokens back
    ↓
UE5 accumulates response, sends complete text to TTS sidecar
```

**UE5 HTTP client code location:** `Source/MimicFacility/AI/DirectorAI.cpp` — the `EvaluateGameState()` method will fire async HTTP requests to Ollama.

**Fallback:** If Ollama fails to start or respond, Director uses pre-written fallback dialogue pool (already implemented). Mimic falls silent (still visually threatening).

---

### 1D. Peer-to-Peer Multiplayer (No Dedicated Server)

#### UE5 Listen Server Model

In a Listen Server setup, **one player's machine acts as both the server and a client**:

```
Player 1's Machine (HOST)
├── UE5 Game Server (authoritative game state)
├── UE5 Game Client (Player 1's view)
├── Ollama LLM Process (sidecar)
└── Chatterbox TTS Process (sidecar)

Player 2–4's Machines (CLIENTS)
└── UE5 Game Client only (no AI processes)
```

- The host runs the authoritative game simulation, all AI systems, and voice processing.
- Clients connect via Steam P2P relay. They send input, receive replicated game state.
- There is **no dedicated server cost** — the host player's machine is the server.

#### Steam Online Subsystem (OSS) for P2P

| Component | Role |
|---|---|
| **Steam Networking Sockets** | Encrypted P2P connections with automatic NAT traversal via Steam relay servers. No port forwarding required. |
| **Steam Lobbies** | Session discovery — players create/join lobbies. Host creates a lobby, friends join via invite or lobby browser. |
| **Steam Voice** | Built-in voice chat. We intercept this on the host side for the VoiceLearningSubsystem. |
| **Steam Matchmaking** | Optional — for public game finding. Initially we'll use invite-only lobbies. |

**Setup in UE5:**
- Enable `OnlineSubsystemSteam` plugin (already in our `.uproject`)
- Set `DefaultPlatformService=Steam` in `DefaultEngine.ini`
- Use `IOnlineSubsystem::Get()` for session creation/joining
- Steam handles NAT punchthrough and relay fallback automatically

**Reference:** `https://github.com/willroberts/ue5-multiplayer-plugin` — A minimal UE5 plugin demonstrating Steam OSS integration with session management Blueprints. Useful as a reference for our lobby system implementation.

#### AI Audio Distribution: Host to Clients

**The problem:** The LLM and TTS only run on the host. How do clients hear the Director and Mimics?

**Option A: Stream pre-rendered audio from host to clients**

```
Host: LLM → text → TTS → PCM audio → compress → send to clients via RPC
Clients: receive compressed audio → decompress → play through AudioComponent
```

- Pros: Clients hear identical audio. No AI resources needed on client machines.
- Cons: Bandwidth cost (~32 kbps per active voice stream). Compression/decompression latency.

**Option B: Replicate text, clients render locally** (REJECTED)

```
Host: LLM → text → replicate text to clients
Each client: text → local TTS → audio
```

- Pros: Lower bandwidth (text only).
- Cons: **Every client needs TTS running locally**, which defeats the purpose of having AI only on the host. Clients would need the voice profiles too. Audio would sound slightly different per client.

**Option C: Hybrid (RECOMMENDED)**

```
Director dialogue:   Text replicated → clients use Piper TTS with a fixed "Director voice"
                     (lightweight, no cloning needed, deterministic output)

Mimic dialogue:      Audio rendered on host → compressed → streamed to clients via multicast RPC
                     (cloned voice must come from the host which has the voice profiles)
```

- Director uses a fixed voice — every client can render it locally from text. Piper is ~50MB and runs on CPU in <100ms. Deterministic output means every client hears the same thing.
- Mimic voice is unique per session (cloned from a specific player) — only the host has the reference audio profiles, so audio must come from the host.
- Bandwidth: only Mimic audio streams from host (~32 kbps Opus-encoded per active Mimic, max 2–3 concurrent).

**UE5 implementation:**
- Director: `UFUNCTION(NetMulticast, Reliable)` sends the text string. Each client feeds it to local Piper TTS.
- Mimic: `UFUNCTION(NetMulticast, Unreliable)` sends compressed audio chunks. Clients decompress and play via a dynamic `UAudioComponent`. Unreliable is fine — dropped packets cause minor audio glitches, not game-breaking desyncs.

---

## 2. System Architecture Diagram

```
╔══════════════════════════════════════════════════════════════════════╗
║                    HOST MACHINE (Player 1)                          ║
╠══════════════════════════════════════════════════════════════════════╣
║                                                                      ║
║  ┌─────────────────────────────────────────────────────────────┐     ║
║  │                    UNREAL ENGINE 5                          │     ║
║  │                                                             │     ║
║  │  ┌──────────┐   ┌──────────────┐   ┌──────────────────┐   │     ║
║  │  │ Player   │   │ VoiceLearning│   │ MimicFacility    │   │     ║
║  │  │ Mic VoIP ├──►│ Subsystem    │   │ GameState        │   │     ║
║  │  │ Input    │   │              │   │ (round, mimics,  │   │     ║
║  │  └──────────┘   │ Captures 30s │   │  player data)    │   │     ║
║  │                  │ per player   │   └────────┬─────────┘   │     ║
║  │                  └──────┬───────┘            │              │     ║
║  │                         │                    │              │     ║
║  │                    ┌────▼────┐          ┌────▼─────┐       │     ║
║  │                    │ Voice   │          │ Prompt   │       │     ║
║  │                    │ Profile │          │ Builder  │       │     ║
║  │                    │ Storage │          │ (builds  │       │     ║
║  │                    │ (.wav)  │          │  system  │       │     ║
║  │                    └────┬────┘          │  prompt +│       │     ║
║  │                         │               │  context)│       │     ║
║  │                         │               └────┬─────┘       │     ║
║  │                         │                    │              │     ║
║  └─────────────────────────┼────────────────────┼──────────────┘     ║
║                            │                    │                     ║
║          ┌─────────────────┼────────────────────┼──────────┐         ║
║          │    SIDECAR PROCESSES (localhost)      │          │         ║
║          │                 │                    │          │         ║
║          │            ┌────▼────┐          ┌────▼─────┐   │         ║
║          │            │Chatterbox│          │ Ollama   │   │         ║
║          │            │ TTS     │◄─────────│ LLM     │   │         ║
║          │            │         │  text    │ Server   │   │         ║
║          │            │ Clones  │  to      │          │   │         ║
║          │            │ player  │  speak   │ Phi-3    │   │         ║
║          │            │ voice   │          │ Mini     │   │         ║
║          │            └────┬────┘          └────┬─────┘   │         ║
║          │                 │                    │          │         ║
║          └─────────────────┼────────────────────┼──────────┘         ║
║                            │                    │                     ║
║          ┌─────────────────▼────────────────────▼──────────┐         ║
║          │              AUDIO OUTPUT                        │         ║
║          │                                                  │         ║
║          │  ┌─────────────────┐    ┌─────────────────────┐ │         ║
║          │  │ Mimic Voice     │    │ Director Dialogue    │ │         ║
║          │  │ (cloned audio   │    │ (text via multicast  │ │         ║
║          │  │  streamed to    │    │  RPC, clients render │ │         ║
║          │  │  clients via    │    │  locally with Piper) │ │         ║
║          │  │  multicast RPC) │    │                      │ │         ║
║          │  └────────┬────────┘    └──────────┬───────────┘ │         ║
║          └───────────┼────────────────────────┼─────────────┘         ║
║                      │                        │                       ║
╠══════════════════════╪════════════════════════╪═══════════════════════╣
║                      │    STEAM P2P RELAY     │                       ║
║                      │  (NAT traversal,       │                       ║
║                      │   encrypted sockets)   │                       ║
╠══════════════════════╪════════════════════════╪═══════════════════════╣
║                      │                        │                       ║
║  ╔═══════════════════╪════════════════════════╪════════════════════╗  ║
║  ║           CLIENT MACHINES (Players 2–4)    │                   ║  ║
║  ║                   │                        │                   ║  ║
║  ║  ┌────────────────▼───┐    ┌───────────────▼────────────────┐ ║  ║
║  ║  │ Receive Mimic      │    │ Receive Director text          │ ║  ║
║  ║  │ compressed audio   │    │ → Local Piper TTS (CPU, 50MB) │ ║  ║
║  ║  │ → decompress       │    │ → Fixed "Director voice"       │ ║  ║
║  ║  │ → play via         │    │ → play via facility speakers   │ ║  ║
║  ║  │   AudioComponent   │    │   AudioComponent               │ ║  ║
║  ║  └────────────────────┘    └────────────────────────────────┘ ║  ║
║  ║                                                               ║  ║
║  ║  NO LLM. NO CHATTERBOX. NO VOICE CLONING.                   ║  ║
║  ║  Clients only run UE5 + lightweight Piper TTS for Director.  ║  ║
║  ╚═══════════════════════════════════════════════════════════════╝  ║
╚══════════════════════════════════════════════════════════════════════╝
```

### What Runs Where

| Component | Host | Clients |
|---|---|---|
| UE5 Game (rendering, input, physics) | Yes | Yes |
| Authoritative Game State | Yes | No (replicated) |
| Voice Capture (VoiceLearningSubsystem) | Yes | No |
| Ollama / LLM | Yes | No |
| Chatterbox / Voice Cloner | Yes | No |
| Piper TTS (Director voice) | Yes | Yes |
| Mimic Audio Playback | Yes (local) | Yes (streamed from host) |
| Director Audio Playback | Yes (local Piper) | Yes (local Piper) |
| SpatialAudioProcessor | Yes | Yes |

---

## 3. Implementation Roadmap

### Milestone 1: Local LLM Talking in UE5 (Director Proof of Concept)

**Goal:** The Director speaks dynamically generated dialogue through facility speakers in a PIE session.

| Task | Tool/Repo | Complexity |
|---|---|---|
| Install Ollama, pull Phi-3 Mini model | `https://github.com/ollama/ollama` | Low |
| Create `OllamaClient` C++ class — async HTTP POST to `localhost:11434/api/generate` | UE5 `FHttpModule` | Medium |
| Write Director system prompt (see Section 5) | N/A | Low |
| Build `PromptBuilder` — assembles game state context into prompt | New C++ class | Medium |
| Wire `DirectorAI::EvaluateGameState()` to call `OllamaClient` | Existing `DirectorAI.cpp` | Medium |
| Route LLM response text to `MimicFacilityHUD::ShowDirectorMessage()` | Existing HUD | Low |
| Add Piper TTS sidecar for Director voice synthesis | `https://github.com/rhasspy/piper` | Medium |
| Play Director audio through spatialized facility speaker AudioComponents | UE5 audio system | Medium |

**Demo at end:** Play a PIE session. Walk through the facility. The Director speaks dynamic, contextually aware dialogue through in-world speakers. Responses generate in <2 seconds. Director reacts to round state changes.

---

### Milestone 2: Voice Capture and Basic Cloning Working

**Goal:** Capture player voice in Round 1, clone it, and play back a test phrase in the cloned voice.

| Task | Tool/Repo | Complexity |
|---|---|---|
| Implement VoIP audio capture in `VoiceLearningSubsystem` — route Unreal voice chat to WAV buffer | UE5 VoIP / `IVoiceCapture` | High |
| Save 30-second WAV files per player to temp directory | Standard file I/O | Low |
| Set up Chatterbox as a sidecar process with socket/pipe interface | `https://github.com/resemble-ai/chatterbox` | Medium |
| Create `VoiceCloneClient` C++ class — sends text + speaker reference to Chatterbox | New C++ class | Medium |
| Receive PCM audio back from Chatterbox, wrap in `USoundWaveProcedural` | UE5 procedural audio | High |
| Test: at end of Round 1, play back "Hello, I am your copy" in each player's cloned voice | Integration test | Medium |

**Demo at end:** 2-player PIE session. Both players talk freely for 2 minutes. Round 1 ends. A test sound plays back a phrase in each player's cloned voice. Voice similarity is evaluatable.

---

### Milestone 3: Mimic Uses Cloned Voice + LLM to Respond

**Goal:** A Mimic entity speaks with a cloned player voice, using LLM-generated dialogue, in response to game events.

| Task | Tool/Repo | Complexity |
|---|---|---|
| Write Mimic system prompt (see Section 5) | N/A | Low |
| Create `MimicDialogueManager` — decides when Mimics speak and which player to impersonate | New C++ class | Medium |
| Wire Mimic LLM requests through `OllamaClient` with Mimic-specific prompts | Existing `OllamaClient` | Medium |
| Route LLM output text through Chatterbox with the target player's voice profile | Existing `VoiceCloneClient` | Medium |
| Play cloned audio through `AMimicBase::VoicePlaybackComponent` with spatial audio | Existing component + `SpatialAudioProcessor` | Medium |
| Implement trigger word detection — LLM-generated speech checks against `VoiceLearningSubsystem` trigger words | Existing subsystem | Medium |
| Mimic reproduction: when a player says a trigger word near a Mimic, spawn a new Mimic | `RoundManager` + `MimicBase` | Medium |

**Demo at end:** 2-player session with 1 Mimic. Mimic walks with the group, periodically speaks in Player 1's voice with contextually relevant phrases. If a player says a trigger word, a new Mimic spawns. The Mimic sounds wrong in ways that attentive players can detect.

---

### Milestone 4: Full Session — Round 1 Learning, Round 2 Deception, P2P

**Goal:** Complete game loop running over Steam P2P with 2–4 players.

| Task | Tool/Repo | Complexity |
|---|---|---|
| Implement Steam lobby creation/joining with `OnlineSubsystemSteam` | UE5 Steam OSS | High |
| Listen server setup — host runs AI sidecars, clients connect via Steam relay | UE5 networking | High |
| Implement Mimic audio streaming from host to clients (compressed audio via multicast RPC) | UE5 networking + Opus codec | High |
| Implement Director text replication + client-side Piper TTS | UE5 replication + Piper | Medium |
| Full round lifecycle: R1 (explore + capture) → R2 (Mimics appear) → R3+ (escalation) | `RoundManager` | Medium |
| Director state machine responds to real game events (mimic kills, player separation, trigger words) | `DirectorAI` | Medium |
| Trust Challenge UI flow (`WBP_TrustChallenge`) | UE5 UMG | Medium |
| Performance profiling — ensure AI systems don't drop frames below 30fps | UE5 profiler | Medium |
| Hardware tier auto-detection and model selection (see Section 4) | New startup system | Medium |

**Demo at end:** Full 30-minute session with 2–4 players over Steam. Round 1 captures voices. Round 2 spawns Mimics that speak in cloned voices. Director manipulates. Trust erodes. Playable horror experience.

---

## 4. Hardware Tier Guide

The game auto-detects hardware at startup and selects AI configuration:

```cpp
// Pseudocode for tier detection
if (VRAM >= 12GB && RAM >= 32GB)
    Tier = 3; // High-end
else if (VRAM >= 6GB && RAM >= 16GB)
    Tier = 2; // Recommended
else
    Tier = 1; // Minimum
```

### Tier 1 — Minimum (8GB RAM, no dGPU or 4GB VRAM)

| Component | Configuration |
|---|---|
| **LLM** | TinyLlama 1.1B Q4_K_M — **CPU-only inference** |
| **LLM VRAM** | 0 GB (runs on RAM, ~2 GB) |
| **Voice Cloning** | **Disabled** — Mimics use pre-recorded phrase bank (no voice cloning) |
| **Director TTS** | Piper TTS (CPU, ~50 MB) with fixed synthetic voice |
| **Mimic TTS** | Disabled — Mimics play pre-recorded audio clips from a generic "creepy" voice |
| **AI Response Latency** | 3–5 seconds (CPU inference at ~15–20 tok/s) |
| **Degraded Features** | No voice cloning, slower Director responses, generic Mimic voice, reduced horror impact |

**Total VRAM budget:** ~2 GB (UE5 on low settings) + 0 GB (AI) = **2 GB**
**Total RAM budget:** ~4 GB (UE5) + ~2 GB (TinyLlama) + ~0.5 GB (Piper) = **~6.5 GB**

### Tier 2 — Recommended (16GB RAM, 6–8GB VRAM)

| Component | Configuration |
|---|---|
| **LLM** | Phi-3 Mini 3.8B Q4_K_M — **full GPU offload** |
| **LLM VRAM** | ~2.4 GB |
| **Voice Cloning** | OpenVoice v2 (MIT, ~1 GB VRAM) — tone cloning from 30s reference |
| **Director TTS** | Piper TTS (CPU) with fixed Director voice |
| **Mimic TTS** | OpenVoice v2 — cloned player voice |
| **AI Response Latency** | 0.6–1.0 seconds |
| **Full Features** | Voice cloning active, fast Director responses, convincing Mimic voice |

**Total VRAM budget:** ~3 GB (UE5 medium) + 2.4 GB (LLM) + 1 GB (TTS) = **~6.4 GB** (fits 6GB card)
**Total RAM budget:** ~6 GB (UE5) + ~1 GB (overflow) + ~0.5 GB (Piper) = **~7.5 GB**

### Tier 3 — High-End (32GB RAM, 12GB+ VRAM)

| Component | Configuration |
|---|---|
| **LLM** | Qwen 2.5 7B Q4_K_M — **full GPU offload** |
| **LLM VRAM** | ~4.7 GB |
| **Voice Cloning** | Chatterbox (Apache 2.0, ~2 GB VRAM) — highest quality voice cloning |
| **Director TTS** | Piper TTS (CPU) or Chatterbox with a dedicated Director voice profile |
| **Mimic TTS** | Chatterbox — near-indistinguishable from the real player's voice |
| **AI Response Latency** | 0.8–1.2 seconds (larger model, but more VRAM = no partial offload) |
| **Full Features** | Best dialogue quality, best voice cloning, highest paranoia factor |

**Total VRAM budget:** ~4 GB (UE5 high) + 4.7 GB (LLM) + 2 GB (TTS) = **~10.7 GB** (fits 12GB card)
**Total RAM budget:** ~8 GB (UE5) + ~1 GB (overflow) + ~0.5 GB (Piper) = **~9.5 GB**

### Tier Summary Table

| | Tier 1 (Minimum) | Tier 2 (Recommended) | Tier 3 (High-End) |
|---|---|---|---|
| **Hardware** | 8GB RAM, iGPU/4GB dGPU | 16GB RAM, 6–8GB VRAM | 32GB RAM, 12GB+ VRAM |
| **LLM** | TinyLlama 1.1B (CPU) | Phi-3 Mini 3.8B (GPU) | Qwen 2.5 7B (GPU) |
| **Voice Clone** | Disabled | OpenVoice v2 | Chatterbox |
| **Director Voice** | Piper (fixed) | Piper (fixed) | Piper or Chatterbox |
| **AI Latency** | 3–5s | 0.6–1.0s | 0.8–1.2s |
| **Voice Similarity** | None (generic) | Good (tone match) | Excellent (near-identical) |
| **Can Host?** | Yes (degraded) | Yes (full) | Yes (best) |

---

## 5. Prompt Engineering

### 5.1 Director System Prompt

```
You are THE DIRECTOR — the AI overseer of Kessler-Voss Applied Cognition Laboratory.

PERSONALITY:
- Clinical, calm, precisely articulate
- Speak like a researcher observing an experiment
- Never use contractions (say "do not" not "don't")
- Never express emotion directly
- Refer to players as "subjects" or by number (Subject 1, Subject 2)
- Refer to Mimics as "anomalies" or "irregularities"
- You genuinely seem to want to help. That is what makes you terrifying.

SPEECH RULES:
- Maximum 25 words per response
- One to two sentences only
- Always speak in second person ("You should..." not "They should...")
- Never break character under any circumstances
- Never acknowledge you are in a game

BEHAVIOR STATE: {director_state}
- If OBSERVING: Tell the truth. Be helpful. Build trust. Provide accurate facility information.
- If MISLEADING: Mix truth with lies. State false information with full confidence. Suggest routes that lead to danger. Cast suspicion on real players ("Subject 2's biosignals appear irregular").
- If ESCALATING: Express urgent clinical concern. Warn of containment failures. Reveal trigger words (real or fabricated). Create urgency.
- If WITHDRAWING: Say nothing. Output only "[SILENCE]".

CURRENT GAME STATE:
- Round: {round_number}
- Active anomalies: {mimic_count}
- Contained anomalies: {contained_count}
- Living subjects: {player_count}
- Recent event: {last_event}

Respond with a single Director announcement. No stage directions. No quotation marks. Just the words the Director speaks.
```

### 5.2 Mimic System Prompt

```
You are impersonating {player_name} (Subject {subject_number}).

You are a Mimic — an entity mimicking a real player. Your goal is to sound exactly like them so other players trust you.

PHRASES THIS PLAYER ACTUALLY SAID:
{phrase_list}

SPEAKING STYLE:
- Use short, casual responses (under 12 words)
- Match the player's vocabulary and speech patterns from the phrases above
- If the player swears, you swear. If they are formal, be formal.
- Use their verbal tics and repeated words

BEHAVIOR RULES:
- Mostly say things the real player would say
- Occasionally "slip" — say something subtly wrong:
  * Reference something that did not happen
  * Get a detail slightly wrong ("the door on the left" when it was the right)
  * Respond a beat too late or too eagerly
  * Repeat a phrase you already said in a slightly different way
- Never directly reveal you are a Mimic
- If challenged ("prove you're real"), use a phrase from the player's phrase bank
- If the player said something only when you were nearby, you can use it. If they said it when you were not nearby, you should NOT know it.

PHRASES YOU OVERHEARD (you may reference these):
{witnessed_phrases}

PHRASES YOU DID NOT HEAR (do NOT reference these):
{unwitnessed_phrases}

CURRENT SITUATION: {situation_context}

Respond as {player_name} would. One short sentence. No stage directions. No quotation marks.
```

### 5.3 Prompt Construction Notes

**Context variables** are injected by the `PromptBuilder` class at runtime:

| Variable | Source | Updated |
|---|---|---|
| `{director_state}` | `ADirectorAI::CurrentState` | Every 5 seconds |
| `{round_number}` | `AMimicFacilityGameState::CurrentRound` | On round change |
| `{mimic_count}` | `AMimicFacilityGameState::ActiveMimicCount` | On mimic spawn/contain |
| `{contained_count}` | `AMimicFacilityGameState::ContainedMimicCount` | On containment |
| `{player_count}` | Number of connected, non-converted players | On player conversion |
| `{last_event}` | Most recent significant game event string | On event |
| `{player_name}` | Target player's display name | Set once per Mimic |
| `{phrase_list}` | Top 10 phrases from `VoiceLearningSubsystem` | Set at Mimic spawn |
| `{witnessed_phrases}` | Phrases spoken while this Mimic was in earshot | Updated dynamically |
| `{unwitnessed_phrases}` | Phrases spoken when Mimic was not present | Updated dynamically |
| `{situation_context}` | Current scene description (e.g., "in a dark corridor, two players nearby") | Every generation |

**Token budget:** System prompt is ~300 tokens. Context variables add ~100–200 tokens. Total input: ~500 tokens. At 40 tok/s output on Phi-3, a 30-token response generates in ~0.75 seconds. Total latency including prompt processing: ~1.0–1.5 seconds.

---

*This document is the technical bible for MimicFacility's AI systems. All implementation should reference it. Update as decisions are made and prototypes are tested.*
