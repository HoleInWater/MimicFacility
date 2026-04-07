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
- Speaks dynamically using Claude API-generated dialogue based on current game events

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

- Players wake up in a starting chamber. The Director introduces itself and provides initial guidance.
- Players explore the facility freely: unlocking doors, finding gear, reading lore notes, and talking naturally.
- The atmosphere is tense but not dangerous. Environmental storytelling hints at what happened here.
- **The AI is passively recording all voice chat.** Players are not explicitly told this in Round 1.
- Round 1 ends when players reach a specific checkpoint or a timer expires. The Director announces the transition.

### Round 2: Infiltration (15–20 minutes)

- The Director announces that "other subjects have been detected in the facility."
- 1–2 Mimics spawn, wearing random player skins. They attempt to blend into the group.
- Players begin to notice oddities: a teammate in two places at once, a voice that sounds slightly off, someone who doesn't respond to questions correctly.
- Paranoia builds. Players start testing each other.
- Trigger Words are active — careless talking spawns more Mimics.
- The Director provides "helpful" information that may or may not be accurate.

### Round 3+: Escalation (until win/loss)

- Mimic count increases. Swarm Mimics and Ceiling Crawlers begin appearing.
- The Director becomes more active — locking doors, flooding sections, filling rooms with spore clouds.
- Environmental hazards force players into tighter spaces where distinguishing real from fake becomes harder.
- The Trust Verification mechanic becomes unreliable as Mimics have accumulated more voice data.
- Pressure escalates until a win or loss condition is met.

---

## 4. Win / Loss Conditions

> **STATUS: OPEN — NEEDS DECISION**

Three design options are presented below. The team should select one or create a hybrid.

### Option A: Extraction

**Win:** Players locate the facility exit (randomized per run) and all surviving *real* players reach it together. A Mimic reaching the exit with the players counts as a loss.

**Loss:** All players are "converted" (replaced by Mimics), or the facility self-destruct timer expires.

| Pros | Cons |
|------|------|
| Clear, intuitive goal | May feel generic for horror |
| Creates natural movement through the facility | Mimic-at-the-exit check could feel gamey |
| Easy to communicate to new players | Doesn't leverage the voice/trust mechanics for the finale |

### Option B: Identification Protocol

**Win:** Players must correctly identify and contain **every** Mimic in the facility using Containment Devices. The Director announces when all Mimics are contained.

**Loss:** If players contain a real player 3 times (false positives), The Director declares the experiment a failure and floods the facility. Or the Mimic count exceeds a threshold (e.g., 10+).

| Pros | Cons |
|------|------|
| Directly engages the core trust/paranoia mechanic | Could stall if players can't find the last Mimic |
| Every containment attempt is a high-stakes decision | Punishing false positives may make players too cautious |
| Natural climax as Mimic count shrinks or swells | Requires careful Mimic count balancing |

### Option C: The Director's Game

**Win:** The Director reveals that the experiment has multiple "phases." Players must complete a series of objectives (repair a machine, decode a message, activate a beacon) while Mimics interfere. Completing all objectives "satisfies" The Director, which opens the exit.

**Loss:** Failing to complete objectives within a time limit, or Mimic swarm overrunning the players.

| Pros | Cons |
|------|------|
| The Director becomes a more active narrative presence | Objectives can feel like busywork |
| Multiple objectives create natural pacing | More complex to balance |
| Replayable — objectives can be randomized | May dilute the core mimic-paranoia experience |

### Recommendation

Consider a hybrid of **Option A + B**: the win condition is reaching the exit, but the exit requires a final **Identification Check** — players must unanimously agree on who is real before the door opens. If a Mimic is among them, the door reveals the deception and the Mimic attacks. This creates a tense social-deduction finale that leverages the voice system.

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

### 5.4 Echo Mimic (Design Suggestion)

> **STATUS: NEEDS DECISION**

| Attribute | Value |
|-----------|-------|
| **Appearance** | Invisible — no physical form |
| **Voice** | Replays full conversations from Round 1 in empty rooms, making players think teammates are nearby |
| **Movement** | Stationary — tied to a specific room or corridor |
| **Threat** | Psychological — wastes time, splits the group, degrades trust in audio cues |
| **Spawn** | Placed by The Director in Round 2+ |

**Design Notes:** The Echo Mimic adds a purely psychological threat layer. It never directly endangers players but makes the facility feel haunted and erodes confidence in voice communication as a whole.

### 5.5 Hive Mimic (Design Suggestion)

> **STATUS: NEEDS DECISION**

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

The Director's dialogue is generated dynamically using the **Claude API**.

**Input to the API:**
- Current game state (round number, player count, Mimic count, player locations)
- Recent player actions and events
- Current Director state (Observing/Misleading/Escalating/Withdrawing)
- Tone guidelines and character bible
- List of true facts and approved lies for the current state

**Output:** A short dialogue line (1–3 sentences) that the Director speaks through facility intercoms. The output is filtered for content safety and length before being passed to text-to-speech.

**Fallback:** If the API is unavailable or latency is too high, a pool of pre-written Director lines per state is used as fallback. The experience should never stall waiting for API response.

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

### 7.3 Director AI — State Machine + Claude API

```
DirectorAI (ADirectorAI : AActor)
├── State Machine (enum: Observing, Misleading, Escalating, Withdrawing)
├── Game State Monitor
│   ├── Polls MimicFacilityGameState every 5s
│   ├── Tracks: player count, mimic count, round number, player stress metric
│   └── Evaluates state transition conditions
├── Dialogue Manager
│   ├── Claude API client (async HTTP request)
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

**Key areas:**
- **Intake Wing** — Where players wake up. Rows of medical pods. Most are empty. Some are not.
- **Research Labs** — Containment chambers, observation decks, whiteboards covered in frantic equations. Evidence of experiments on "auditory mimicry in fungal neural networks."
- **The Garden** — A massive atrium where the facility's ventilation system has allowed spore growth to flourish. Trees of fungal matter reach toward skylights. This is where the spores are thickest.
- **Server Room** — Deep in the facility. Houses the hardware that runs The Director. Reaching it may or may not be an objective.
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

### NEEDS DECISION: Win/Loss Condition
- See Section 4 for three options + hybrid recommendation
- Team must playtest all three and select one by end of prototyping phase

### NEEDS DECISION: Voice Data Privacy & Consent
- How do we handle voice recording consent? GDPR/CCPA implications?
- Do we need an explicit opt-in screen before Round 1?
- What is our data retention policy if any analytics are stored post-session?
- Should players be able to opt out of voice recording and play with text chat only? How does this affect Mimics targeting that player?

### NEEDS DECISION: Echo Mimic Inclusion
- See Section 5.4 — is the Echo Mimic compelling enough to implement, or does it dilute the core physical-Mimic threat?
- If included, does it count toward the Mimic containment win condition (Option B)?

### NEEDS DECISION: Hive Mimic Inclusion
- See Section 5.5 — does the Hive Mimic add meaningful variety or just spectacle?
- How does it interact with containment-based win conditions?
- Performance implications of large organic mesh + multiple voice streams

### NEEDS DECISION: Text-to-Speech vs. Recorded Audio for Mimics
- Option A: Use real recorded player audio clips (more authentic, limited vocabulary)
- Option B: Use AI voice synthesis to generate new phrases in the player's voice (more flexible, higher uncanny valley risk, ethical considerations)
- Option C: Hybrid — recorded clips for short phrases, synthesis for longer utterances
- Ethical review needed regardless of choice

### NEEDS DECISION: The Director's Voice
- Pre-recorded professional voice actor with branching dialogue trees?
- AI-generated TTS using Claude API output?
- Hybrid: voice actor for key lines, TTS for dynamic/reactive dialogue?
- Budget and production timeline implications for each

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
