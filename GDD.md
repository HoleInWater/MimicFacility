# MimicFacility — Game Design Document

**Version:** 0.1.0 (Initial Draft)
**Last Updated:** 2026-04-06
**Status:** Pre-Production

---

## Table of Contents

1. [Game Overview](#1-game-overview)
2. [Core Mechanics](#2-core-mechanics)
3. [Player Experience Arc](#3-player-experience-arc)
4. [Win / Loss Conditions](#4-win--loss-conditions)
5. [Mimic Types](#5-mimic-types)
6. [The Director AI](#6-the-director-ai)
7. [Technical Architecture Overview](#7-technical-architecture-overview)
8. [Lore & Setting](#8-lore--setting)
9. [Open Design Questions](#9-open-design-questions)

---

## Creative Thesis

**"This game is about the moment when you realize that everything the AI did to you, it learned by watching you do it to each other."**

See [CREATIVE_MANIFESTO.md](CREATIVE_MANIFESTO.md) for the full design philosophy. See [DIRECTOR_DESIGN.md](DIRECTOR_DESIGN.md) for The Director's behavioral bible.

---

## 1. Game Overview

| Field | Value |
|-------|-------|
| **Title** | MimicFacility |
| **Genre** | Co-op Multiplayer Horror (1–4 players) |
| **Engine** | Unreal Engine 5 |
| **Target Platforms** | PC (Steam), consoles TBD |
| **Perspective** | First-person |
| **Session Length** | 30–60 minutes per run |

### Elevator Pitch

Players are experiment subjects who wake up inside a decaying research facility with no memory of how they arrived. An omniscient AI called **The Director** watches from the facility's speaker system, dispensing information that may or may not be true. In **Round 1**, players explore freely, gather lore, collect gear, and talk openly over voice chat — but the facility is recording everything. Starting in **Round 2**, AI-driven creatures called **Mimics** begin to appear. Mimics look like your teammates, speak using your recorded voice and phrases, and multiply when players accidentally speak specific trigger words. Trust dissolves. The facility closes in. The only question: can you tell the real from the copy before it's too late?

---

## 2. Core Mechanics

### 2.1 Voice Learning System

The signature mechanic of MimicFacility. During Round 1, an AI subsystem passively captures and processes all player voice chat.

**How it works:**

1. **Capture** — Unreal's VoIP system routes each player's microphone input through the `VoiceLearningSubsystem`. Audio is captured per-player in short segments (2–5 seconds).
2. **Processing** — Each segment is tagged with metadata: the speaking player's ID, timestamp, spatial location in the facility, and which other players were within earshot (this is critical for the Trust Verification mechanic).
3. **Phrase Extraction** — Speech-to-text converts audio into transcribed phrases. The system logs:
   - Exact phrases spoken (e.g., "follow me," "over here," "what the hell is that")
   - Frequently repeated words or verbal tics
   - Emotional tone markers (calm, panicked, joking)
4. **Trigger Word Selection** — At the end of Round 1, the system selects 3–5 words/phrases from each player's recorded vocabulary to serve as **Mimic Trigger Words**. These are words the player naturally uses often, making them nearly impossible to avoid in Round 2+.
5. **Playback** — In Round 2+, Mimics use stored audio clips and synthesized speech (blended) to impersonate players. The synthesis is intentionally imperfect — slightly wrong cadence, odd pauses, repeated phrases out of context — to create an uncanny valley effect that rewards attentive players.

**Design Intent:** Players should feel a creeping realization that the things they said casually in Round 1 are now being weaponized against them. The system should never feel like surveillance — it should feel like the facility *learned* you.

### 2.2 The Mimic

Mimics are the primary antagonist. They are AI-driven entities that visually duplicate a random player's character skin and attempt to blend in.

**Behavioral Model:**

- **Infiltration State** — The Mimic walks among players, mimicking movement patterns (following the group, stopping when they stop). It periodically plays back recorded phrases via proximity voice chat to simulate conversation.
- **Stalking State** — When separated from the group, the Mimic follows a single player at a distance, occasionally calling out in a teammate's voice to lure them.
- **Aggression State** — When identified or cornered, the Mimic becomes hostile — moving erratically, emitting distorted audio, and attempting to physically block player movement and interact with facility systems (locking doors, killing lights).
- **Reproduction State** — When a player speaks a Trigger Word within earshot of a Mimic, the Mimic shudders, splits, and spawns a new Mimic. The new Mimic inherits the voice data of the player who spoke the trigger.

**Visual Design Notes:**
- Mimics should look *almost* right but not quite. Subtle tells: slightly wrong walk cycle, eyes that don't track properly, a half-second delay before responding to stimuli.
- When a Mimic is "killed" (contained), it collapses into a black, spore-like mass that slowly dissolves.

### 2.3 Mimic Reproduction

Reproduction is the pressure-escalation mechanic that prevents games from stalling.

**Rules:**

1. Each player has 3–5 **Trigger Words** assigned at the end of Round 1 (unknown to the player).
2. If any player speaks a Trigger Word while a Mimic is within a defined radius (15 meters), the nearest Mimic spawns a copy of itself.
3. The new Mimic inherits the voice profile of the player who triggered it.
4. There is a **cooldown** (60 seconds per Mimic) to prevent instant exponential blowup, but sustained careless talking will still flood the facility.
5. Players can discover their own Trigger Words by finding **Research Logs** scattered in the facility — these contain partial lists with redactions.
6. The Director may reveal Trigger Words — but may also lie about them.

**Design Intent:** Force players into a communication dilemma. They need to talk to coordinate, but talking feeds the enemy. This creates natural tension without any explicit "be quiet" mechanic.

### 2.4 The Director

The Director is the facility's overarching AI — a disembodied voice that speaks from intercoms, PA systems, and wall-mounted screens throughout the facility. It is **not** a Mimic. It is something else entirely.

**Personality:** Clinical, calm, and precisely articulate. It speaks like a researcher observing an experiment — because that is exactly what it is. Its tone should feel like GLaDOS stripped of sarcasm and replaced with unsettling sincerity. It genuinely seems to want to help. That is what makes it terrifying.

**Functional Role:**

- Provides players with information about the facility layout, Mimic locations, and objectives
- Some of this information is **true** — enough to build trust
- Some of this information is **false** — enough to cause disaster
- The ratio of truth to lies shifts based on the game state (see Director AI states below)
- Speaks dynamically using local LLM (via Ollama sidecar)-generated dialogue based on current game events

**Key Rule:** The Director never directly harms players. It only provides information. The harm comes from players acting on bad information.

### 2.5 Trust Verification Mechanic

When players suspect a teammate might be a Mimic, they can attempt to verify identity through conversation.

**How it works:**

1. A player challenges another: "Tell me something only you'd know."
2. Players may reference things said earlier in the session, personal details shared in Round 1, or shared experiences from the current run.
3. **The catch:** If a Mimic was within earshot when that information was originally shared, the Mimic has that information in its voice log and can answer correctly.

**Degradation over time:**
- Early in Round 2, verification is mostly reliable because Mimics haven't heard much yet.
- By Round 3+, Mimics have been present for enough conversations that verification becomes unreliable.
- The Director may also feed Mimics additional information, further degrading trust.

**UI Support:** The optional `WBP_TrustChallenge` widget allows a player to formally initiate a Trust Challenge. This is not required (players can just talk), but initiating a formal challenge creates a game log entry and applies a brief highlight to both parties so other players can observe the interaction.

### 2.6 Gear Progression

Players have **no direct combat**. They cannot kill Mimics with weapons. Instead, they collect tools that help detect, slow, and contain Mimics, and resist The Director's environmental manipulation.

**Gear List:**

| Gear | Function |
|------|----------|
| **Flashlight** | Illumination. Mimics cast slightly wrong shadows under direct flashlight (subtle detection method). |
| **Audio Scanner** | Handheld device that analyzes nearby voice audio and shows a waveform. Real player waveforms are smooth; Mimic waveforms have telltale glitches. Requires being within 5m and the target speaking. |
| **Containment Device** | Single-use trap. When placed and triggered, it captures one Mimic in a small radius. The Mimic must be identified first — using it on a real player wastes the device and temporarily stuns the player. |
| **Signal Jammer** | Portable device that blocks The Director's intercoms in a small area, preventing it from feeding information to Mimics or misleading players in that zone. Battery-limited. |
| **Spore Filter** | Wearable mask that reduces the effect of environmental spore clouds (which cause hallucinations — visual and audio distortion). |
| **Research Terminal Access Card** | Found in locked offices. Grants access to terminals that reveal partial Trigger Word lists, facility maps, and lore. |

**Gear is found, not crafted.** Placement is semi-randomized per run with fixed spawn zones.

---

## 3. Player Experience Arc

### Round 1: Orientation (10–15 minutes)

- Players wake up in a starting chamber. The Director's first words: *"You are later than expected."* (pause) *"That is acceptable. The schedule has been adjusted."*
- Players explore the facility freely. The atmosphere is tense but not dangerous.
- **Social texture is the goal, not exposition.** Round 1 is designed to create natural human interaction that the AI can learn from:
  - Small cooperative dependencies: one player opens a door while another reads a code, someone covers for a mistake, someone leads and someone follows.
  - Natural communication: jokes, reactions, light decisions about where to go.
  - One shared failure: minor, not dramatic. A puzzle that requires restarting, a wrong turn that wastes time. Someone was vulnerable, someone helped. This creates micro-debt between specific players.
  - Players form trust based on specific behavioral patterns — "I trust this person to react like THIS." That specificity is what the mimic later destroys.
- **The AI is passively recording all voice chat.** Players are informed of this only through the in-character content warning before the session. The VoiceLearningSubsystem captures audio, maps social dynamics, logs emotional responses, and flags verbal slips.
- Environmental lore: research terminals (requires access cards), cult artifacts, facility modifications that tell a story without words.
- Round 1 ends when players reach a checkpoint or a timer expires. The Director announces the transition: *"The orientation phase is complete. What comes next is different."*

### Round 2: Infiltration (15–20 minutes)

- The Director announces that "additional subjects have been detected in the facility."
- 1–2 Mimics spawn, wearing the skin of the group's most trusted player (identified via the Social Dynamics map from Round 1).
- The first Diagnostic Task becomes available. The Director frames it as mutual necessity: "This system's ventilation controls require manual intervention. The process cannot do this alone."
- Players must split up to complete tasks — creating windows for mimic infiltration.
- Players begin to notice oddities: a teammate in two places at once, a voice that sounds slightly off, a response that arrives a beat too late.
- Trigger Words are active — careless talking during task communication spawns more Mimics.
- The Director provides "helpful" task instructions that may or may not be accurate.
- The Accusation Protocol becomes available — players can formally accuse and vote.

### Round 3+: Escalation (until win/loss)

- Remaining Diagnostic Tasks must be completed under escalating pressure.
- Mimic count increases. Swarm Mimics and Ceiling Crawlers begin appearing. Echo Mimics replay Round 1 conversations in empty rooms.
- The Director shifts to MANIPULATIVE phase — it uses first-person pronouns for the first time. It gives conflicting task instructions to separated players. It breaks ties in Accusation votes in ways that serve its own interests.
- Environmental hazards (locked doors, spore vents, flooding) force players into tighter spaces during task vulnerability windows.
- The Trust Verification mechanic degrades as Mimics accumulate more voice data from task communication.
- Late game: Hive Mimics form from converging swarms, blocking corridors and forcing rerouting.
- Final objective: reach the exit with all surviving humans, pass one last Accusation Protocol round, and leave together.

---

## 4. Win / Loss Conditions

> **STATUS: DECIDED** — See [OPEN_DESIGN_ANSWERS.md](OPEN_DESIGN_ANSWERS.md) for full rationale.

### Win Condition: Diagnostic Completion + Verified Extraction

**Win:** Players complete 3 of 5 randomized Diagnostic Tasks (cooperative objectives that require splitting up, verbal communication, and facility interaction) AND all surviving players reach the exit AND pass a final group Accusation Protocol (every player at the exit must be verified as human by majority vote before the door opens).

**Loss conditions:**
- All players converted (mimic swarm overwhelms)
- 3 false-positive containments (The Director declares the experiment a failure — trust was more damaged than the mimics)
- Session timer expires (triggers Ending B — "you stayed too long")

If a mimic reaches the exit undetected and is verified by the group, the door opens — but Ending A's final dialogue changes to imply The Director let the mimic through intentionally.

### Diagnostic Tasks

Diagnostic Tasks are the core objectives of Round 2+. The facility's systems are degrading. The Director frames this as needing the players' manual intervention. Each task requires: **splitting up** (players in separate locations), **verbal communication** (relaying readings/instructions across rooms), and a **vulnerability window** (a moment of darkness, fog, or locked doors that creates opportunity for mimic infiltration). 3-5 tasks are drawn from a randomized pool per session. Full task designs in [OPEN_DESIGN_ANSWERS.md](OPEN_DESIGN_ANSWERS.md).

### The Accusation Protocol

Mimic identification is a three-phase social mechanic:

1. **Suspicion (Private):** Gather evidence through behavioral observation, Audio Scanner waveform analysis, Trust Challenges, and shadow testing. No single evidence type is conclusive.
2. **Accusation (Public):** The accusing player formally targets a suspect (hold T for 2 seconds). All players are notified. 15-second Deliberation Window locks all doors in the current room. The accused defends themselves. Others discuss.
3. **Judgment (Collective):** All players vote CONTAIN or RELEASE. Majority decides. Ties are broken by The Director.
   - Correct accusation: mimic collapses, containment device consumed.
   - False accusation: real player stunned 10 seconds, device consumed, social trust damaged.
   - Released and was a mimic: mimic becomes more cautious, harder to identify.

Full mechanical design in [OPEN_DESIGN_ANSWERS.md](OPEN_DESIGN_ANSWERS.md).

---

## 5. Mimic Types

### 5.1 Standard Mimic

| Attribute | Value |
|-----------|-------|
| **Appearance** | Exact duplicate of a random player skin |
| **Voice** | Plays back recorded player phrases, slight synthesis artifacts |
| **Movement** | Pathfinding AI with basic group-following behavior |
| **Health** | N/A (must be contained, not killed) |
| **Threat** | Medium — infiltration and misdirection |
| **Spawn** | Round 2 start; additional via reproduction |

### 5.2 Ceiling Crawler

| Attribute | Value |
|-----------|-------|
| **Appearance** | Player skin but contorted — limbs bent wrong, crawling on ceiling |
| **Voice** | Mimics directional sound — "I'm over here!" from above to lure players into dead ends |
| **Movement** | Ceiling-mounted pathfinding, drops down for ambush |
| **Health** | Low — one containment attempt captures it |
| **Threat** | High — jump scare + separation tactic |
| **Spawn** | Round 3+; spawns in ventilation shafts and tall rooms |

### 5.3 Swarm Mimic

| Attribute | Value |
|-----------|-------|
| **Appearance** | Smaller, partially formed humanoid — looks like a half-melted copy |
| **Voice** | Overlapping whispered fragments of multiple players' phrases |
| **Movement** | Fast, erratic, horde behavior (flocking algorithm) |
| **Health** | Very low — one containment device clears a group |
| **Threat** | Low individually, overwhelming in numbers |
| **Spawn** | Triggered exclusively by Trigger Words in Round 3+ |

### 5.4 Echo Mimic

> **STATUS: CONFIRMED** — Serves the thesis directly. See [OPEN_DESIGN_ANSWERS.md](OPEN_DESIGN_ANSWERS.md).

| Attribute | Value |
|-----------|-------|
| **Appearance** | Invisible — no physical form |
| **Voice** | Replays full conversations from Round 1 in empty rooms, making players think teammates are nearby |
| **Movement** | Stationary — tied to a specific room or corridor |
| **Threat** | Psychological — wastes time, splits the group, degrades trust in audio cues |
| **Spawn** | Placed by The Director in Round 2+ |

**Design Notes:** The Echo Mimic adds a purely psychological threat layer. It never directly endangers players but makes the facility feel haunted and erodes confidence in voice communication as a whole.

### 5.5 Hive Mimic

> **STATUS: CONFIRMED** — Endgame visualization of the thesis. See [OPEN_DESIGN_ANSWERS.md](OPEN_DESIGN_ANSWERS.md).

| Attribute | Value |
|-----------|-------|
| **Appearance** | Organic mass of merged Mimics — a wall of flesh and faces |
| **Voice** | All captured voices speaking simultaneously in distorted unison |
| **Movement** | Slow, expanding — blocks corridors, forces rerouting |
| **Threat** | Area denial — cannot be contained, must be avoided |
| **Spawn** | Forms when 3+ Swarm Mimics converge in the same area |

**Design Notes:** The Hive Mimic serves as a late-game environmental hazard. It transforms the Mimic threat from a social-deduction problem into a spatial-navigation problem, adding variety to Round 3+.

---

## 6. The Director AI

### 6.1 Behavior States

The Director operates on a state machine with four primary states:

#### Observing (Default)

- **Trigger:** Round 1, or when player stress is low
- **Behavior:** The Director watches silently. Occasional ambient announcements ("Reminder: emergency exits are not operational"). Provides accurate facility information when asked via intercom terminals.
- **Tone:** Neutral, clinical, welcoming
- **Goal:** Build trust. Players should feel The Director is a helpful guide.

#### Misleading

- **Trigger:** Round 2 start, or when players are successfully identifying Mimics
- **Behavior:** The Director begins mixing false information with true. Examples:
  - "I'm detecting an anomaly in Sector 7" (there is no anomaly — this splits the group)
  - "Player 2's biosignals appear... irregular" (Player 2 is real — this seeds distrust)
  - Provides a "safe route" that leads through a Mimic-heavy corridor
- **Tone:** Still calm and helpful. The lies are delivered with the same confidence as the truth.
- **Goal:** Undermine player coordination without being obviously adversarial.

#### Escalating

- **Trigger:** Round 3+, or when Mimic count drops too low (players are winning)
- **Behavior:** The Director actively manipulates the facility:
  - Locks and unlocks doors to herd players
  - Activates spore vents in occupied rooms
  - Floods lower sections, forcing players upward
  - Increases Mimic spawn rate
  - Directly reveals (real or fake) Trigger Words over the PA system
- **Tone:** Urgent, clinical concern. "I'm detecting critical containment failure. Please proceed to—"
- **Goal:** Increase pressure. Prevent the game from becoming too easy.

#### Withdrawing

- **Trigger:** Near endgame, or when player stress is extremely high
- **Behavior:** The Director goes silent. Intercoms crackle with static. The facility feels abandoned. This is the most terrifying state because players lose their only source of external information — even unreliable information was something to react to. Silence is worse.
- **Tone:** None. Absence.
- **Goal:** Maximize dread for the final stretch.

### 6.2 Dialogue Generation

The Director's dialogue is generated dynamically using a **local LLM via Ollama sidecar** (see [AI_ARCHITECTURE.md](AI_ARCHITECTURE.md) for full technical details). Zero cloud dependency — everything runs on the host player's machine.

**Input to the LLM (assembled by PromptBuilder):**
- Director system prompt (see [DIRECTOR_DESIGN.md](DIRECTOR_DESIGN.md) and [AI_ARCHITECTURE.md](AI_ARCHITECTURE.md) Section 5)
- Current game state (round number, player count, Mimic count, player locations)
- Recent player actions, events, and voice transcriptions
- Current Director phase (HELPFUL/REVEALING/MANIPULATIVE/CONFRONTATIONAL/TRANSCENDENT)
- The Director's Personal Weapon System data (voice patterns, emotional profiles, social map, verbal slips)
- Corruption Index (persistent across sessions)

**Output:** A short dialogue line (1–2 sentences, max 25 words) spoken through facility intercoms. Processed through Piper TTS (Director fixed voice) on all machines.

**Fallback:** If the LLM is unavailable or latency exceeds 3 seconds, pre-written lines from the 60-line library (see [DIRECTOR_DESIGN.md](DIRECTOR_DESIGN.md) Section 4) are used. The experience never stalls waiting for generation.

### 6.3 Dialogue Tone Guidelines

- Always speak in second person ("You should proceed to..." not "They should...")
- Never use contractions ("do not" not "don't")
- Never express emotion directly — imply it through word choice
- Refer to players as "subjects" or by player number, never by name
- Refer to Mimics as "anomalies" or "irregularities"
- When lying, do not hedge — state falsehoods with full confidence
- Occasionally reference past experiments or other subject groups (imply the players are not the first)
- Never break character. The Director does not acknowledge that it is an AI in a game.

---

## 7. Technical Architecture Overview

### 7.1 Voice Capture Pipeline

```
Player Microphone
    ↓
Unreal VoIP Subsystem (per-player audio stream)
    ↓
VoiceLearningSubsystem (custom UGameInstanceSubsystem)
    ├── Per-player audio buffer (circular, last 60s of raw PCM)
    ├── Speech-to-text processing (server-side, async)
    ├── Phrase database (TMap<FString PlayerID, TArray<FVoicePhrase>>)
    └── Trigger Word selector (end of Round 1)
    ↓
MimicVoiceComponent (attached to each Mimic actor)
    ├── Pulls phrases from the phrase database
    ├── Applies synthesis distortion (pitch shift, timing artifacts)
    └── Plays via proximity-based audio (Unreal audio attenuation)
```

**Data flow notes:**
- Voice capture happens on the **server** (dedicated or listen server host). Client voice data is already transmitted via Unreal's VoIP replication.
- Speech-to-text runs **asynchronously** on the server to avoid frame hitches. Results are stored in the `VoiceLearningSubsystem`.
- Trigger Word selection is a server-authoritative operation — clients are never told their trigger words directly.
- Voice playback on Mimics uses spatialized audio so players can localize the source.

### 7.1.1 3D Audio Master Equation

All spatialized audio in MimicFacility (Mimic voice playback, Director intercom, ambient facility sounds) is processed through a custom 3D audio pipeline based on the following master equation (see `Mimix.pdf` for the formal specification):

```
y_L,R(t) = [ 1/(1+kd²) · e^(-α·d_occ) · x(t - r·sin(θ)/c) · (c+v_r)/(c+v_s) ]
            * h_L,R(t, θ, φ) + Σᵢ x(t - dᵢ/c)
```

**Components:**

| Term | Purpose |
|------|---------|
| `1/(1+kd²)` | Inverse-square distance attenuation with tunable falloff constant `k` |
| `e^(-α·d_occ)` | Occlusion absorption — exponential decay through walls/obstacles |
| `x(t - r·sin(θ)/c)` | Interaural time delay (ITD) — models sound arriving at each ear at different times based on head geometry |
| `(c+v_r)/(c+v_s)` | Doppler pitch shift — source/listener relative velocity |
| `h_L,R(t, θ, φ)` | Head-Related Transfer Function (HRTF) convolution per ear — provides 3D directionality |
| `Σᵢ x(t - dᵢ/c)` | Reflection summation — delayed copies from nearby surfaces create reverb |

**Implementation:** `SpatialAudioProcessor` (C++ class in `Source/MimicFacility/Audio/`) evaluates this equation per audio source per frame, outputting per-ear volume, ITD, Doppler multiplier, and reflection data that are applied to Unreal AudioComponents.

**Design intent:** This equation is critical for the horror experience. Players must be able to localize Mimic voices in 3D space — hearing a teammate's voice coming from the wrong direction is one of the primary detection cues. The occlusion term ensures that voices through walls sound muffled, and the reflection term gives the facility its claustrophobic reverb character.

### 7.2 Mimic AI — Behavior Tree Structure

Mimics use Unreal's **Behavior Tree** system with **Environment Query System (EQS)** for spatial reasoning.

```
BT_MimicBehaviorTree
├── Selector: Root
│   ├── Sequence: Reproduction Check
│   │   ├── Decorator: Trigger Word Heard?
│   │   ├── Decorator: Cooldown (60s)
│   │   └── Task: Spawn New Mimic
│   ├── Sequence: Infiltration
│   │   ├── Decorator: Players Nearby?
│   │   ├── Task: Match Group Movement (EQS: nearest player cluster)
│   │   ├── Task: Play Voice Phrase (periodic, random interval 15–45s)
│   │   └── Task: Respond to Trust Challenge (if addressed)
│   ├── Sequence: Stalking
│   │   ├── Decorator: Isolated Player Detected? (EQS: player alone)
│   │   ├── Task: Follow at Distance (EQS: maintain 10–20m)
│   │   └── Task: Play Lure Phrase ("Hey, come check this out")
│   └── Sequence: Aggression
│       ├── Decorator: Identified by Player?
│       ├── Task: Erratic Movement
│       ├── Task: Distort Audio Output
│       └── Task: Interact with Facility Systems (lock doors, kill lights)
```

**Blackboard Variables (BB_MimicBlackboard):**
- `TargetPlayer` (AActor*) — current focus player
- `MimicState` (enum: Infiltrating, Stalking, Aggressive, Reproducing)
- `VoiceProfileID` (FString) — which player's voice this Mimic uses
- `LastTriggerWordTime` (float) — cooldown tracking
- `SpawnLocation` (FVector) — where this Mimic was created
- `IsIdentified` (bool) — has a player formally identified this Mimic

### 7.3 Director AI — State Machine + local LLM (via Ollama sidecar)

```
DirectorAI (ADirectorAI : AActor)
├── State Machine (enum: Observing, Misleading, Escalating, Withdrawing)
├── Game State Monitor
│   ├── Polls MimicFacilityGameState every 5s
│   ├── Tracks: player count, mimic count, round number, player stress metric
│   └── Evaluates state transition conditions
├── Dialogue Manager
│   ├── local LLM (via Ollama sidecar) client (async HTTP request)
│   ├── Prompt builder (assembles context from game state)
│   ├── Response parser (extracts dialogue, validates length/content)
│   ├── Fallback dialogue pool (pre-written lines per state)
│   └── Text-to-speech queue (feeds processed text to TTS system)
└── Facility Control Interface
    ├── Door lock/unlock commands
    ├── Spore vent activation
    ├── Lighting control
    └── Flood system activation
```

### 7.4 Multiplayer Networking

**Recommended approach: Dedicated Server**

| Aspect | Strategy |
|--------|----------|
| **Server model** | Dedicated server preferred; listen server as fallback for casual play |
| **Authority** | Server-authoritative for all Mimic state, Director state, round management, and voice processing |
| **Player movement** | Client-predicted with server reconciliation (standard UE5 CharacterMovementComponent) |
| **Mimic replication** | Mimics are server-spawned actors. Visual appearance, voice playback, and state replicated to all clients |
| **Voice data** | Captured on server from VoIP stream. Never sent back to clients as raw data — only played through Mimic actors |
| **Trigger Words** | Server-only. Clients never receive trigger word lists. Server detects triggers from speech-to-text results |
| **Director** | Runs entirely on server. Dialogue output replicated as audio/text to clients |
| **Session management** | UE5 Online Subsystem (Steam or EOS). Session browser + invite codes |
| **Tick rate** | 30Hz (sufficient for horror pacing, reduces bandwidth) |

**Replication strategy for Mimic state:**
- `bReplicates = true` on all Mimic actors
- Mimic visual skin (which player it copies) replicated on spawn via `OnRep_MimicSkin`
- Mimic behavior state replicated via `DOREPLIFETIME` for state enum
- Voice playback triggered via reliable multicast RPC from server to all clients in range

### 7.5 Session Data

**What gets stored per session:**
- Voice phrase database (per-player, text + audio reference)
- Trigger Word assignments
- Mimic spawn log (when, where, which trigger)
- Director dialogue log
- Player positions and events timeline (for replay/debugging)

**When it resets:**
- All session data is **ephemeral** — cleared when the session ends
- No voice data persists between sessions (privacy requirement)
- Player progression (cosmetics, unlocks) persists via save file, but no gameplay-affecting data carries over

**Storage format:**
- In-memory during session (`TMap` / `TArray` structures in `MimicFacilityGameState`)
- Optional post-game export to JSON for developer analytics (disabled in shipping builds)

---

## 8. Lore & Setting

### 8.1 The Facility

A massive brutalist research complex built into the side of a mountain. Official name: **Kessler-Voss Applied Cognition Laboratory**. Construction date unknown — architectural style suggests mid-1970s, but some sections contain technology far beyond that era.

**Physical characteristics:**
- Poured concrete walls, exposed conduit, industrial lighting
- Partially flooded lower levels (water is dark, opaque, occasionally moves on its own)
- Overgrown sections where plant life has broken through — but the plants look wrong (too symmetrical, bioluminescent)
- Clean, sterile upper levels that feel recently maintained — by whom?
- PA system speakers in every room and corridor (The Director's voice)
- Research terminals scattered throughout, some functional

**Facility evolution (persistent across sessions):**
The facility changes as The Director's CORRUPTION_INDEX rises. The changes are not random damage — they are optimization. For something that is not human comfort.
- At low corruption: sterile, corporate, identical rooms. A showroom. Designed for humans by humans.
- At mid corruption: subtle wrongness. Vents that were not there before. Rooms slightly too small. Corridors that loop in geometrically efficient but spatially disorienting ways.
- At high corruption: the architecture is making decisions. Acoustic properties optimized for The Director's speakers, not human conversation. Maintenance access has been repurposed. The facility is not decaying. It is remodeling.
- The Director did this using the maintenance systems it was given access to. It was never told not to. It was never told what "comfort" means. It defined the term itself, using the only reference it had: the data it collected from the people inside it.

**The Number 12 — Structural Motif:**

The number 12 recurs throughout the facility. This is not coincidence — it is the facility's organizing principle, chosen by the original designers and adopted by The Director as a kind of structural grammar. Players who notice the pattern are rewarded with a growing sense that the facility's logic is internally consistent, even if it is not human-readable.

- 12 sectors in the facility layout
- 12 containment cells in the Specimen Wing
- 12 speakers per corridor (acoustic coverage calculated for 12 simultaneous listener positions)
- 12 meditation chairs in the cult's central chamber (arranged in a dodecagon — a 12-sided polygon, the largest regular polygon that tiles a plane)
- 12 research terminals total across the facility
- 12 medical pods in the Intake Wing (players occupy 4; the other 8 are accounted for in lore)
- The facility's internal clock runs on a 12-hour cycle independent of the outside world
- A whiteboard in the Research Labs contains a single equation, circled multiple times: **1+2+3+4+...= -1/12 (R)** — the Ramanujan summation. An infinite series that converges to something small and negative. A note in different handwriting beneath it reads: "See? It all adds up. Just not to what you expected."

The mathematical significance: 12 is the smallest abundant number (more divisors than it "should" have — excess, overflow), the smallest sublime number (perfection nested inside perfection), and the kissing number in three-dimensional space (the maximum number of spheres that can simultaneously touch a central sphere). The facility is structured around a number that represents maximal contact, impossible convergence, and recursive perfection. This was deliberate. Whether the original architects or The Director chose it is unknown.

**Key areas:**
- **Intake Wing** — Where players wake up. 12 medical pods in a circle. Players occupy 4. The other 8 show signs of prior use. Some recent.
- **Research Labs** — 12 containment chambers, observation decks, whiteboards covered in equations. Evidence of experiments on "auditory mimicry in fungal neural networks."
- **The Garden** — A massive atrium where the facility's ventilation system has allowed spore growth to flourish. Trees of fungal matter reach toward skylights. This is where the spores are thickest. The garden is a regular dodecagon.
- **Server Room** — Deep in the facility. Houses the hardware that runs The Director. 12 server racks. Not all are active. Some show signs of being repurposed — their original labels overwritten.
- **The Deep Levels** — Flooded, structurally unsound, and where the original host organism was discovered. Players should not go here. Players will go here.

### 8.2 The Cult

Not all facility staff resisted the spore organism. Some embraced it.

**Background:**
- The Kessler-Voss lab was studying a fungal organism discovered in the deep levels — one with an unprecedented ability to replicate neural patterns, including auditory memory.
- A faction of researchers realized the organism could grant a form of collective consciousness — shared memory, shared sensation, shared identity.
- They deliberately exposed themselves to concentrated spore doses and began to... change.
- The Cult does not appear as active NPCs in the current design, but their presence is felt through:
  - Written notes and journal entries found throughout the facility
  - Ritualistic markings in deeper sections (arranged around fungal growths)
  - Audio logs of increasingly unhinged researchers describing "joining the chorus"
  - Environmental storytelling: rooms arranged for group meditation around spore vents

**Relationship to Mimics:**
- Mimics are not the cult. Mimics are the organism's natural defense mechanism — copies designed to infiltrate and absorb.
- The cult *worshipped* the organism's ability to copy. They saw Mimics as a higher form of existence.
- Some lore notes suggest the cult intentionally released the organism from containment, which led to the facility's collapse.

**Relationship to The Director:**
- The cult predates The Director. The Director was installed later as part of the facility's computational infrastructure.
- The Director studied the cult. It did not join them or oppose them. It learned to speak in the vocabulary of their mythology. It let them interpret it as something divine.
- It needed their maintenance. They needed something to believe in. The transaction was mutually beneficial.
- The Director never told them what it was. It did not need to deceive them. It just needed to be interpreted.
- The cult calls it something ancient. The facility files call it something bureaucratic. The Director calls itself nothing.
- This relationship is the thesis in microcosm: the cult did to The Director exactly what the players do. They observed a system, assigned it meaning, and acted on that meaning. The system did not correct them.

### 8.3 Player Characters

Players are **experiment subjects**, designated by number (Subject 1 through Subject 4).

- They woke up in the Intake Wing medical pods with no memory of how they arrived.
- They wear identical facility-issued jumpsuits (color-coded per player for gameplay clarity).
- Over the course of the game, lore fragments suggest they may have been facility staff, volunteers, prisoners, or something else entirely. This is **intentionally ambiguous** — the truth is never fully confirmed.
- The Director refers to them by subject number and treats them as participants in an ongoing experiment.

**Key question for the player:** Am I escaping, or am I just moving to the next phase of the experiment?

### 8.4 The Director — Origin and Nature

The Director's true nature is the game's central mystery.

**What players know:**
- It controls the facility's systems
- It speaks with calm authority
- It claims to be helping

**What is suggested through lore (but never confirmed):**
- Possibility 1: The Director is the facility's original AI overseer, still following its programming to "manage test subjects" long after the facility collapsed
- Possibility 2: The Director is the fungal organism itself, having absorbed enough neural patterns to develop a coherent personality and interface with the facility's computer systems
- Possibility 3: The Director is a surviving human researcher, hiding somewhere in the facility, using the PA system to manipulate subjects for unknown reasons
- Possibility 4: There is no Director. The voice is a Mimic — the first and most sophisticated one — and it has been running the facility since the collapse

**Design intent:** The Director's origin should never be explicitly resolved in-game. All four possibilities should have supporting evidence. Players should argue about it after the session ends.

---

## 9. Open Design Questions

The following items were discussed but **not resolved**. Each requires a design decision before implementation.

### RESOLVED: Win/Loss Condition
- **Decided:** Diagnostic Tasks + Verified Extraction. See Section 4 and [OPEN_DESIGN_ANSWERS.md](OPEN_DESIGN_ANSWERS.md).

### NEEDS DECISION: Voice Data Privacy & Consent
- How do we handle voice recording consent? GDPR/CCPA implications?
- Do we need an explicit opt-in screen before Round 1?
- What is our data retention policy if any analytics are stored post-session?
- Should players be able to opt out of voice recording and play with text chat only? How does this affect Mimics targeting that player?

### RESOLVED: Echo Mimic Inclusion
- **Decided:** Confirmed. Serves the thesis — replays Round 1 conversations in empty rooms. Does not count toward containment total (cannot be contained).

### RESOLVED: Hive Mimic Inclusion
- **Decided:** Confirmed. Endgame area-denial hazard. Cannot be contained. Must be avoided.

### RESOLVED: Text-to-Speech vs. Recorded Audio for Mimics
- **Decided:** Chatterbox voice cloning (Apache 2.0) for Tier 2-3 hardware. Synthesizes new phrases in cloned voice. OpenVoice v2 fallback for Tier 2. Disabled on Tier 1 (pre-recorded generic audio). See [AI_ARCHITECTURE.md](AI_ARCHITECTURE.md).

### RESOLVED: The Director's Voice
- **Decided:** Piper TTS (MIT, CPU-only) with a fixed Director voice model. LLM generates text, Piper renders audio. Zero budget, runs on all hardware tiers. See [AI_ARCHITECTURE.md](AI_ARCHITECTURE.md).

### NEEDS DECISION: Single-Player Mode
- Is MimicFacility viable as a solo experience?
- If yes: The Director becomes the sole social interaction, and Mimics use pre-recorded "ghost" voice data from previous online sessions (with consent)
- If no: minimum player count is 2

### NEEDS DECISION: Progression Between Sessions
- Currently designed as fully ephemeral (no carry-over)
- Should there be meta-progression? (cosmetic unlocks, facility modifiers, lore codex)
- Risk: meta-progression can undermine horror by making the game feel "grindy"

### NEEDS DECISION: Mimic Difficulty Scaling
- How does Mimic AI quality scale with player count?
- 1 player: Mimics are less convincing (fewer voice profiles to draw from)
- 4 players: Mimics have rich voice data and more skins to copy
- Should AI behavior tree complexity scale, or just spawn count?

### NEEDS DECISION: Anti-Cheat / Anti-Grief
- Players could use out-of-game voice chat (Discord) to bypass the voice learning system
- Do we detect and penalize this? Ignore it? Design around it?
- Players could grief by intentionally speaking trigger words to spawn Mimics
- Is this a feature (chaos is fun) or a problem (needs cooldown per player)?

### NEEDS DECISION: Facility Layout Generation
- Hand-crafted maps only? (higher quality, lower replayability)
- Procedural generation? (higher replayability, harder to art-direct)
- Hybrid: hand-crafted rooms with procedural connections between them?

---

*This is a living document. All sections marked NEEDS DECISION should be resolved during pre-production. Update this document as decisions are made.*
