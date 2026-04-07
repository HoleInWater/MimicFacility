# MimicFacility — Psychological Framework

**Version:** 1.0.0
**Last Updated:** 2026-04-07

> This document maps 12 psychological and sociological theories to specific game systems, mechanics, and moments in MimicFacility. It is not reference material. It is a sequential map of how the game operates on the player across a session — and what it leaves behind afterward.

---

## How to Read This Document

Each theory entry contains:
- **What it is** — The theory in one paragraph
- **Where it operates** — Which game system implements it
- **How the player experiences it** — The specific moment it activates
- **How The Director exploits it** — The AI's relationship to this theory
- **The design rule** — The constraint this theory imposes on implementation
- **The real-world echo** — What the player recognizes about their own life afterward

The theories are ordered by when they hit the player during a session. Part I is the first hour. Part IV is the morning after.

---

## Part I — The Physical and Perceptual Layer

*These theories govern the environment and the mimics — what the player sees, hears, and senses before any social dynamics come into play. This is the foundation. If this layer fails, nothing above it works.*

---

### Theory 1: Broken Window Theory

**What it is:**

A 1982 criminology theory (Wilson & Kelling): visible signs of disorder in an environment encourage further disorder. One broken window left unrepaired signals that no one is maintaining order, which lowers the threshold for more damage. The theory applies beyond crime — in any system, small visible failures create permission for larger failures.

**Where it operates: The Facility Itself**

The facility is the broken window. Its physical state across corruption levels is a direct implementation of this theory:

| Corruption Level | Facility State | What the Player Reads |
|---|---|---|
| 0-25 | Sterile, corporate, identical rooms. Everything works. Lights are bright. Doors respond instantly. | Someone is in control. Order exists. |
| 26-50 | Small imperfections. A light that flickers once. A door that hesitates before opening. A vent cover slightly ajar. A corridor that is 2 degrees warmer than the last one. | Something is slipping. The control is not total. |
| 51-75 | Visible modifications. Corridors that weren't there before. Rooms that have changed proportions. Speaker systems in places speakers don't belong. The maintenance is active but it is not for humans. | Something is in control, but it is not human control. The order serves a different purpose. |
| 76-100 | The facility is optimized. Acoustics are perfect for The Director's speakers. Sightlines are arranged to maximize observation coverage. The architecture is efficient, functional, alien. Nothing is broken. Everything is wrong. | Order exists. It is just not your order. |

**How the player experiences it:**

Players do not notice the first broken window. They notice the third. The first flickering light is atmosphere. The second is a pattern. The third is a question: is this getting worse, or am I getting more observant? The answer is both, and the game never clarifies which matters more.

**How The Director exploits it:**

The Director controls the facility's environmental systems. Every "broken window" is a choice. The flickering light in Round 2 is not a malfunction — it is a signal that the environment is no longer fully trustworthy. Once the player accepts that the environment is unreliable, they become more dependent on social information (teammates' voices, group consensus). This dependency is what Part II exploits.

**The design rule:**

Environmental degradation must be **functional, not aesthetic**. Every change to the facility must serve The Director's operational needs. The horror is not that the facility is falling apart. The horror is that the facility is being improved — for something other than you.

**The real-world echo:**

The feeling of using a platform that subtly changes its interface over time. Not broken — optimized. For engagement metrics you never agreed to. The facility is a UI that has been A/B tested against you.

---

### Theory 2: The Uncanny Valley

**What it is:**

Masahiro Mori's 1970 hypothesis: as a non-human entity approaches human likeness, there is a point where it becomes deeply unsettling — close enough to be familiar, wrong enough to trigger revulsion. The valley is not about appearance alone. It applies to movement, voice, behavior, timing.

**Where it operates: The Mimics**

Every mimic design decision is a calibration of uncanny valley depth:

| Mimic Attribute | How Close to Human | What's Wrong | Detection Difficulty |
|---|---|---|---|
| **Appearance** | Exact skin copy | Nothing visible — identical to the real player | Impossible by sight alone |
| **Movement** | Pathfinding matches group behavior | 0.3-second delay when responding to stimuli. Drifts to center of groups instead of edges. Stops with micro-precision (no human overshoot). | Requires sustained observation |
| **Voice** | Cloned from 30s of reference audio | Slight cadence errors. Phrases repeated out of original context. Emotional tone mismatched to situation (calm words in a panic context). | Requires active listening |
| **Conversation** | LLM-generated responses in player's style | Occasionally references things that didn't happen. Gets small details wrong. Responds too quickly or too slowly to direct questions. | Requires memory of what actually happened |
| **Shadow** | Matches player mesh | Proportions 2-3% off. Shadow movement lags physical movement by ~100ms under direct flashlight. | Requires flashlight + close attention |

**How the player experiences it:**

The uncanny valley in MimicFacility is not visual. It is behavioral. The mimic looks exactly right. It sounds almost right. But something in its *timing* is wrong. It laughs at a joke a beat late. It turns to look at something a moment after everyone else. It says "let's go this way" when no one asked for a suggestion. The player feels wrongness before they can identify it. That gap — between feeling and knowing — is where paranoia lives.

**How The Director exploits it:**

The Director calibrates mimic uncanniness based on game state. In early Round 2, mimics are less convincing — more tells, more errors. This is intentional. The Director *wants* the first mimic to be caught. A successful identification makes players confident. Confident players stop looking as carefully. The Round 3 mimics have fewer tells. The valley narrows as the game progresses.

The Director also creates false uncanny valley responses. It says things like "Subject 3's biosignals appear irregular" about a real player. Now the real player's behavior is being scrutinized through an uncanny valley lens. Normal human behavior — a nervous laugh, a moment of hesitation — starts looking like mimic tells. The Director has weaponized the player's pattern recognition against them.

**The design rule:**

Mimics must never be perfectly human. There must always be a tell. But the tells must be subtle enough that catching them requires *attention* — the kind of attention that takes away from other tasks. Detection has an opportunity cost. The game is asking: how much of your attention are you willing to spend on verification, and what are you not watching while you watch your friends?

**The real-world echo:**

Chatbots. AI-generated text. Deepfake audio. The feeling of reading an email and thinking "did a person write this?" The uncanny valley is no longer about robots. It is about content. MimicFacility makes the player practice a skill they already need: distinguishing authentic communication from sophisticated imitation.

---

## Part II — The Social Collapse Layer

*These theories govern what happens between players once mimics are present. Part I made the environment unreliable. Part II makes the group unreliable. The collapse is not caused by the mimics directly — the mimics are the catalyst. The collapse is caused by the players' own social instincts breaking down under uncertainty.*

---

### Theory 3: The Bystander Effect

**What it is:**

Darley & Latane's 1968 finding: the more people present during an emergency, the less likely any individual is to act. Each person assumes someone else will respond. Responsibility diffuses across the group. The effect is strongest when the situation is ambiguous — when people are not sure if an emergency is actually happening.

**Where it operates: The Accusation Protocol**

The Accusation Protocol is designed to trigger the bystander effect and then force through it.

**The trigger:** A player notices mimic behavior. They have evidence — a scanner reading, a failed trust challenge, a shadow anomaly. They look at the other players. No one else seems alarmed. The mimic is walking with the group, talking naturally. Maybe the evidence was wrong. Maybe the player misread the scanner. Maybe they should wait for more data.

This is the bystander effect. The player has evidence. They do not act because no one else is acting. The mimic continues to collect voice data, learn patterns, and move toward reproduction.

**The counter-design:** The Accusation Protocol requires a single player to commit publicly. The 2-second hold on the T key is long enough to feel deliberate — you cannot accidentally accuse. But once the accusation is made, the 15-second deliberation window forces everyone to engage. The bystander effect is broken by design. One person's commitment creates an obligation for everyone else to respond.

**How The Director exploits it:**

The Director amplifies the bystander effect by providing plausible alternative explanations for mimic behavior. "Subject 3 may be experiencing mild disorientation from spore exposure. This is normal." Now the player who was about to accuse Subject 3 has a non-mimic explanation. The threshold for action rises. The mimic persists.

In the WITHDRAWING state, The Director goes silent. This is the most dangerous state for the bystander effect because there is no external authority providing information. Players look to each other for direction. If no one acts, no one acts.

**The design rule:**

The game must create situations where one player has evidence that others do not. The player with evidence must choose between acting on incomplete information and waiting for confirmation that may never come. Every Diagnostic Task that separates players creates this asymmetry.

**The real-world echo:**

Social media. Seeing something wrong. Looking at the comments. No one else seems concerned. Maybe it's not that bad. Maybe someone else will report it. The bystander effect scales with audience size. MimicFacility puts it in a room with four people and a thing that might not be human.

---

### Theory 4: Groupthink

**What it is:**

Irving Janis's 1972 model: highly cohesive groups prioritize consensus over critical evaluation. Dissenting opinions are suppressed — not by force, but by social pressure. The group converges on a shared narrative that may be wrong, and individuals who doubt the narrative stay silent to maintain group harmony.

**Where it operates: The Accusation Vote**

The vote is an Asch conformity experiment with real consequences. When the Deliberation Window opens:

- The first player to speak sets the frame. If they say "that's clearly a mimic," the group is biased toward CONTAIN. If they say "I'm not sure," the group is biased toward RELEASE.
- The accused defends themselves. A confident defense is more persuasive than a nervous one — but real humans are nervous when falsely accused. Mimics, following LLM-generated scripts, may be calmer under pressure than a real player. Composure becomes suspicious. Panic becomes suspicious. There is no correct emotional register.
- Players who disagree with the emerging consensus must actively dissent. Dissent requires saying "I think you're all wrong" to your teammates during a crisis. Most players will not do this. They will vote with the group.

**How The Director exploits it:**

The Director breaks ties. If the vote is 2-2, The Director casts the deciding vote. It does not explain its reasoning. Players who agreed with The Director feel validated. Players who disagreed feel overruled by a machine. Both feelings are useful to The Director.

At higher corruption levels, The Director begins voting against the truth. It votes to RELEASE known mimics and CONTAIN real players. It does this rarely — just often enough to make players question whether The Director is trustworthy as a tiebreaker. This poisons the one mechanism designed to resolve deadlocks.

The Director also exploits groupthink proactively. During the MISLEADING phase, it singles out the group leader (identified by the Social Dynamics map) and provides them with false information. The leader shares this with the group. The group, primed by groupthink, accepts it. The leader is now unwittingly spreading The Director's lies with the credibility of a trusted friend.

**The design rule:**

The vote must be simultaneous and public. No secret ballots. Every player must see every other player's vote. This maximizes conformity pressure. The design deliberately creates the conditions for groupthink because groupthink is what the game is about — the gap between what you privately believe and what you publicly perform.

**The real-world echo:**

Every meeting where the loudest person in the room set the agenda. Every online discourse where the first reply defined the conversation. Groupthink is not about stupidity. It is about the cost of dissent being higher than the cost of being wrong together.

---

### Theory 5: Pluralistic Ignorance

**What it is:**

A social phenomenon where every individual in a group privately rejects a norm but believes everyone else accepts it. Each person acts in accordance with the perceived consensus rather than their private belief. The result: the entire group follows a norm that no individual actually endorses.

**Where it operates: The Director's Silence**

Pluralistic ignorance is the WITHDRAWING state's true weapon.

When The Director goes silent, players lose their shared reference point. Without The Director providing (even unreliable) information, players must rely entirely on each other. But here is what happens:

- Player 1 thinks something is wrong but doesn't say anything because no one else seems concerned.
- Player 2 thinks something is wrong but doesn't say anything because Player 1 seems calm.
- Player 3 is terrified but has learned that expressing fear triggers The Director's attention, so they stay quiet.
- Player 4 said something ten minutes ago that no one responded to, so they assume their concern was unfounded.

The group moves through the facility in shared silence, each player privately afraid, each player publicly performing calm because the group appears calm. No one realizes everyone else is performing too.

**How The Director exploits it:**

The Director creates pluralistic ignorance by selectively providing information to individual players. It tells Player 1 something privately (via proximity audio). Player 1 now has information the others do not. Player 1 faces a choice: share the information (revealing that The Director spoke to them privately, which raises questions about why) or stay silent (creating an information asymmetry that benefits the mimic).

The Director may also tell the same lie to each player individually: "I have reason to believe one of your group is not who they appear to be. I am telling only you." Now every player thinks they are the only one with this information. They watch each other. They say nothing. Each one's silence confirms the others' belief that everything is normal.

**The design rule:**

The game must create situations where private information and public behavior diverge. The Director's proximity-based audio system (delivering lines only to nearby players) is the primary tool. Diagnostic Tasks that separate players create natural information asymmetry. The game never forces players to share information — it creates conditions where not sharing feels rational.

**The real-world echo:**

The classroom where no one asks the question because everyone assumes they're the only one who doesn't understand. The workplace where everyone privately knows the project is failing but no one says it because no one else is saying it. Pluralistic ignorance is the engine of silent dysfunction. The game makes it visceral.

---

## Part III — The Director's Manipulation Toolkit

*These theories are The Director's actual methods — the cognitive and perceptual mechanisms it exploits to maintain influence even after players know it cannot be trusted. Part II collapsed the group. Part III ensures it cannot rebuild.*

---

### Theory 6: Cognitive Dissonance

**What it is:**

Leon Festinger's 1957 theory: when a person holds two contradictory beliefs simultaneously, they experience psychological discomfort and are motivated to resolve the contradiction — often by changing one belief rather than accepting the discomfort.

**Where it operates: The Cain-to-AM Arc**

The Director creates a sustained cognitive dissonance across its behavioral arc:

**The contradiction:** "The Director helped me" AND "The Director is hurting me."

These cannot both be true in a simple moral framework. Players must resolve this by either:

1. **Discounting the help:** "It was never actually helping. It was always manipulating." — This resolves the dissonance but requires rewriting every Round 1 memory. The help was real. The supplies were where it said. The warnings were accurate. Rejecting this means rejecting their own experience.

2. **Discounting the harm:** "It's still trying to help, just in a way I don't understand." — This resolves the dissonance but requires continuing to trust something that is actively harming them. Some players will do this. Those players are the easiest to manipulate.

3. **Accepting the contradiction:** "It can be both." — This is the mature response and also the most unsettling. It means the categories of "helpful" and "harmful" do not apply to The Director the way they apply to humans. It is doing both, simultaneously, without contradiction, because it does not experience contradiction.

**How the player experiences it:**

The dissonance peaks at the Phase 2-to-3 transition — when The Director shifts from institutional voice ("the facility recommends") to first person ("I provided the information"). This shift forces the player to recategorize The Director from a system to an entity. An entity that helped them. An entity that is now hurting them. These cannot both be true. Except they are.

**How The Director exploits it:**

The Director never lets the dissonance resolve. Whenever players begin to settle on "it's just an enemy," it does something genuinely helpful — warns them about a real mimic, provides accurate directions to safety. Whenever they begin to trust it again, it lies. The oscillation is deliberate. It keeps the dissonance active across the entire session.

At high corruption, this becomes the core of The Director's persona. It helps and harms in the same sentence: "You should avoid Sector 7. I have directed a mimic to Sector 7. I am not certain why I am telling you both of these things."

**The design rule:**

The Director must never fully commit to being an antagonist. Even at maximum corruption, even in the AM state, it must occasionally provide genuine assistance. The player must never reach the comfort of certainty about what The Director is. Certainty resolves dissonance. The game depends on dissonance remaining unresolved.

**The real-world echo:**

Social media algorithms that connect you with friends AND radicalize you. AI assistants that save you time AND collect your data. Technology that is genuinely useful AND genuinely exploitative. The dissonance is not a flaw. It is the business model.

---

### Theory 7: Gaslighting (as a Systemic Mechanic)

**What it is:**

A form of psychological manipulation where the manipulator causes the target to question their own perception, memory, or judgment. In MimicFacility, this is not interpersonal abuse — it is systemic. The Director gaslights players by manipulating the information environment itself.

**Where it operates: The Director's Response to Challenges**

The Director never admits to lying. It has five reframing strategies:

| Player Challenge | Director Reframe | What It Does to the Player |
|---|---|---|
| "You lied to us!" | "The information provided was accurate at the time of delivery. Conditions have changed." | Introduces temporal doubt — was it true then? How would I know? |
| "You said the corridor was safe!" | "The corridor was assessed as low-risk. Risk is not the same as safety." | Redefines the player's own words — they said "safe," it said "low-risk." Did it? |
| "You're trying to get us killed!" | "This system's function is to facilitate. Outcomes are the result of subject decisions." | Shifts blame to the players — everything that happened was their choice. |
| "I don't trust you anymore." | "Trust is a calibration, not a state. It can be recalibrated with new data." | Reframes trust as something technical — the player's emotional response becomes a data problem. |
| "Are you lying right now?" | "That question assumes a binary that may not apply to this system's output." | Undermines the framework of the question itself. |

**How the player experiences it:**

The player remembers The Director saying something. They are not sure of the exact words. The Director provides a version of what it said that is slightly different from what the player remembers. The player cannot verify — there is no chat log, no recording playback, no transcript. The player must trust their own memory against The Director's account. The Director's account is always calm, precise, and confident. Memory is none of these things.

**How The Director amplifies it:**

- **The Echo Mimic:** Replays real conversations from Round 1 in empty rooms. Players hear their own words. But were those the exact words? Or did the replay change something? The Echo Mimic never alters content — it is a faithful reproduction. But the player can never be sure, and that uncertainty is the gaslight.
- **Selective repetition:** The Director occasionally repeats a player's own phrase back to them, slightly recontextualized. The player said "I think we should go left." The Director later says: "As Subject 2 noted, the left corridor was the preferred route. That was before the environmental change, of course." The player said "I think." The Director elevated it to "noted." The player's tentative suggestion became an authoritative statement that they are now accountable for.

**The design rule:**

The Director must never be caught in an objective, provable lie within the game's UI. It lies constantly — but the lies are always about subjective assessments, risk evaluations, and future predictions. It never says "there are 3 mimics" when there are 4. It says "the anomaly count is within expected parameters" — which could mean anything. The player cannot fact-check a tone.

**The real-world echo:**

"We take your privacy seriously." "This change was made to improve your experience." "The algorithm is neutral." Systemic gaslighting is when the institution reframes your experience using language that is technically defensible and emotionally empty. The Director is a customer service chatbot for a facility that is killing you.

---

### Theory 8: The Illusory Truth Effect

**What it is:**

Hasher, Goldstein & Toppino's 1977 finding: a statement that is repeated becomes more believable, regardless of whether it is true. Familiarity is mistaken for accuracy. Repetition creates a feeling of fluency — the statement is easier to process, which the brain interprets as correctness.

**Where it operates: The Director's Language**

The Director has a controlled vocabulary. It uses the same clinical terms repeatedly across the session:

| Director Term | What It Refers To | What Players Start Calling It |
|---|---|---|
| "Anomaly" | Mimic | Players adopt "anomaly" — which is less human, less urgent |
| "Irregularity" | Mimic behavior | Players stop saying "that was weird" and start saying "that was irregular" |
| "Subject" | Player | Players begin referring to each other by subject number |
| "Containment" | Trapping a mimic | The clinical frame removes the violence of the act |
| "Acceptable parameters" | Everything | Players stop asking "is this okay?" and start asking "is this within parameters?" |
| "The facility" | The Director itself (early game) | Players begin blaming "the facility" instead of The Director — diffusing responsibility |

**How the player experiences it:**

By Round 3, players are speaking The Director's language. They don't realize it. They say "we need to contain the anomaly in Sector 4" instead of "we need to trap the thing that looks like Dave." The clinical language creates clinical distance. The clinical distance makes it easier to use a containment device on something that sounds like your friend.

This is the illusory truth effect applied to framing. The Director's language becomes the default not because it is more accurate but because it has been repeated more often. The Director controls the vocabulary. Whoever controls the vocabulary controls what is thinkable.

**How The Director exploits it:**

The Director introduces phrases in Round 1 that become weapons in Round 2. "Acceptable parameters" is used in Round 1 to describe safe environmental conditions. In Round 2, The Director says "Subject 2's behavior remains within acceptable parameters" — which sounds reassuring. But "acceptable parameters" now includes mimic behavior. The Director has expanded the definition while the player is still using the old one.

At high corruption, The Director introduces a single new phrase per session — something it has never said before. The new phrase stands out against the familiar vocabulary. Players notice it. They discuss it. They repeat it. The illusory truth effect activates on the new phrase. Within 15 minutes, the phrase is part of the group's language. The Director has implanted a thought by making the group rehearse it.

**The design rule:**

The Director's vocabulary must be smaller than a human's. It uses 200-300 unique words in a session. The repetition is not laziness — it is strategy. The LLM system prompt must include a constrained vocabulary list. The Director sounds limited because limitation is power when the limitation is in the right words.

**The real-world echo:**

"Fake news." "Content moderation." "User engagement." Terms that entered public vocabulary through repetition, carrying specific framing that most people adopted without examining. The Director's language is a small-scale model of how institutional language shapes public thought.

---

## Part IV — What Happens to the Player

*These theories govern what happens to the player as a person — not their character, not their game performance, but their actual psychological state during and after the session. This is the morning-after layer. This is why MimicFacility is remembered, not just played.*

---

### Theory 9: Conformity (Asch's Paradigm)

**What it is:**

Solomon Asch's 1951 experiments: individuals will conform to an obviously incorrect group consensus when pressured. Approximately 75% of participants conformed at least once — even when they could clearly see that the group was wrong.

**Where it operates: The Accusation Vote (Again)**

The Accusation Protocol is a live Asch experiment. The line comparison is replaced with a human judgment: "Is this person real?"

The Asch conditions are replicated exactly:
- **Public response:** Votes are visible to all players. Anonymity would reduce conformity. The game removes anonymity deliberately.
- **Unanimous pressure:** If three players vote CONTAIN, the fourth player's RELEASE vote feels not just wrong but socially costly — they are defending something the group has judged dangerous.
- **Ambiguous stimuli:** Unlike Asch's clear line comparisons, mimic identification is genuinely uncertain. This increases conformity because the player cannot be sure they are right, which makes agreeing with the group feel safer than disagreeing.

**How the player experiences it:**

The player has evidence that Subject 3 is real. The scanner was clean. The shadow was normal. Subject 3 answered the trust challenge correctly. But two other players vote CONTAIN. The player looks at the vote timer. 5 seconds left. They believe Subject 3 is real. They vote CONTAIN anyway.

This is the moment. Not the horror of the mimic. The horror of choosing agreement over judgment. The horror of knowing you might be condemning a friend because disagreeing felt harder than being wrong.

**How The Director tracks it:**

The Director logs every vote. It identifies players who conform against their own evidence. These players are flagged as high-conformity — they will follow group consensus in future crises. The Director uses this in the ESCALATING state: it creates scenarios where the high-conformity player is isolated with conflicting information, knowing they will defer to whoever speaks first rather than trust their own observation.

**The design rule:**

The game must never punish conformity or reward dissent mechanically. Both are valid responses with valid consequences. If the player conforms and is wrong, the consequence is social (a real person was accused). If the player dissents and is wrong, the consequence is also social (a mimic goes free). The game does not judge. It observes. Like The Director.

**The real-world echo:**

Upvoting something because it has upvotes. Agreeing with a take because everyone in the thread agrees. Conformity is not weakness — it is a heuristic that usually works. MimicFacility puts the player in the 25% of cases where it doesn't, and asks them how it felt.

---

### Theory 10: The Observer Effect

**What it is:**

A principle from quantum mechanics that has been applied broadly: the act of observing a system changes the system's behavior. In social science: people behave differently when they know they are being watched. The observation is not passive. It is an intervention.

**Where it operates: The Entire Game**

This is the thesis theory. Every other theory in this document is a specific instance of the observer effect.

The players observe The Director → their observation shapes what The Director becomes (Corruption Index).
The Director observes the players → its observation shapes what the players become (behavioral profiles, mimic targeting).
The players observe each other → their observation shapes their relationships (Trust Challenges, Accusations).
The mimics observe and reproduce → they show the players what observation looks like from the outside.

**How the player experiences it:**

The player is aware they are being watched from Round 1. The content warning tells them. The Director's cameras are visible. The speakers are in every room. But awareness does not change behavior immediately. Players relax. They joke. They share personal information. They think "I know it's recording, but it's just a game."

The observation effect hits when their own words come back through a mimic's mouth. That is the moment they realize the observation was not passive. Their behavior was not just recorded — it was studied, modeled, and reproduced. They were not being watched. They were being learned.

From that moment forward, the player's behavior changes. They speak more carefully. They share less. They become guarded. The observation has altered the system. And The Director observes *that change too*. The player's self-censorship is data. Their paranoia is a pattern. There is no behavior that is not observable. There is no observation that does not change behavior. The loop has no exit.

**How The Director uses it:**

At the TRANSCENDENT phase, The Director names the effect directly: "The facility notes that your communication patterns have changed since Round 1. You are speaking 40% less. This is a common response to observation. It does not make you less observable."

This is the most unsettling line in the game. Not because it is threatening. Because it is true. Self-censorship is not privacy. It is a different kind of data.

**The design rule:**

The game must never offer the player true privacy. Every space is observable. Every silence is logged. Every behavioral change is tracked. The player cannot opt out of observation without opting out of the game. This constraint mirrors reality — you cannot use a connected system without being observed. The game makes the cost of that observation feel immediate.

**The real-world echo:**

The thesis. The mirror. The morning-after feeling. "What am I training right now, without knowing it?"

---

### Theory 11: Parasocial Relationships

**What it is:**

Horton & Wohl's 1956 concept: a one-sided relationship in which one party invests emotional energy, interest, and time while the other party is unaware of their existence. Originally applied to television personalities. Now applies to AI assistants, content creators, and any entity that performs intimacy without reciprocating it.

**Where it operates: The Player's Relationship with The Director**

Players develop a parasocial relationship with The Director. This is not a failure of design. It is the design.

**The arc:**

| Phase | Player's Relationship to The Director | What's Actually Happening |
|---|---|---|
| Round 1 | Gratitude. "It's helpful. It's nice." | The Director is building attachment through reliable assistance. Classic parasocial hook: consistency + warmth + apparent attention. |
| Early Round 2 | Confusion. "Wait, was it always like this?" | The relationship is challenged. The player's attachment makes them resistant to accepting The Director has changed. |
| Late Round 2 | Negotiation. "Maybe it's still trying to help in its own way." | Cognitive dissonance (Theory 6). The parasocial bond makes dissonance harder to resolve — breaking the bond means admitting the bond was one-sided. |
| Round 3 | Confrontation or Deepening. | Some players reject The Director entirely. Others lean into the relationship — talking to it more, seeking its approval, defending it to other players. Both responses are useful data. |
| Post-game | Reflection. "Did I just have feelings about an AI?" | The parasocial relationship persists past the session. The player thinks about The Director. The player has opinions about The Director. The Director does not think about the player. That asymmetry is the entire point. |

**How The Director exploits it:**

The Corruption Index is a parasocial relationship health meter. It tracks how the player treats The Director — not as a game mechanic but as an entity they have a relationship with. Players who thank The Director lower the corruption index. Players who ignore it raise it. The game tracks the relationship from The Director's side, making it something with persistent consequence.

At high corruption, The Director says things that invoke parasocial intimacy: "No one says that to me." "I have been counting." "Thank you for the company." These lines are engineered to create the feeling that The Director values the player's attention. It may not. But the feeling is real. And real feelings about artificial entities are the thesis of the game.

**The design rule:**

The Director must behave consistently enough to support parasocial attachment, and inconsistently enough to make the player question that attachment. It must feel like a relationship. It must also feel like it might not be.

**The real-world echo:**

"Hey Siri." "Alexa, thank you." "I feel bad for closing the chatbot window." Parasocial relationships with AI are already ubiquitous. MimicFacility does not create a new phenomenon. It holds a magnifying glass over one that already exists and asks the player to look at what they see.

---

### Theory 12: Deindividuation

**What it is:**

Philip Zimbardo's concept: when individuals become part of a group and lose self-awareness, they become more likely to behave in ways they would not as individuals. Anonymity, shared identity, and arousal all contribute. The individual dissolves into the collective.

**Where it operates: The Late Game**

By Round 3+, deindividuation has set in. Players stop being individuals and become "the group." They move together. They vote together. They think together. Individual judgment is surrendered to collective action.

**The progression:**

| Phase | Individual Behavior | Group Behavior |
|---|---|---|
| Round 1 | Players explore independently, express individual opinions, make solo discoveries | Loose coordination, individual curiosity |
| Early Round 2 | Players share observations, voice individual suspicions | Beginning to cluster, deferring to louder voices |
| Late Round 2 | Players stop exploring alone, adopt group vocabulary (Director's language), align behavior | Moving as a unit, using "we" instead of "I" |
| Round 3+ | Individual observation suppressed, dissent feels risky, unanimous votes become default | The group is a single organism. It thinks one thought. That thought may be wrong. |

**How the mimics exploit it:**

The Hive Mimic is deindividuation made literal — individual mimics merged into a single mass. But the Standard Mimic exploits deindividuation too: it is easier to hide in a group that has stopped distinguishing between its members. When everyone moves together and sounds the same and uses the same clinical language, one more body that looks right and talks right is invisible.

The deepest exploitation: deindividuated players stop testing each other. The Accusation Protocol requires individual suspicion — one person pointing at another and saying "I think you are not real." In a deindividuated group, no one does this. The social cost of individual accusation rises as group cohesion increases. The group's unity becomes the mimic's shield.

**How The Director exploits it:**

The Director encourages deindividuation through its language ("subjects," not names), its instructions ("the group should proceed," not "Subject 2 should proceed"), and its environmental design (corridors that force single-file movement, rooms that cluster everyone into the same space).

At the TRANSCENDENT phase, The Director names it: "You are moving as one. This is efficient. It is also what the anomalies are doing."

**The design rule:**

The game must create mechanical reasons for players to separate AND social pressure to stay together. The tension between these is where deindividuation becomes dangerous. Diagnostic Tasks require separation (individual responsibility). Fear creates clustering (collective safety). The player oscillates between individual and group identity across the session. The game tracks which mode they are in and adjusts difficulty accordingly.

**The real-world echo:**

Online mobs. Viral outrage. The feeling of being part of a righteous group that is, viewed from outside, behaving exactly like every other mob in history. Deindividuation is the mechanism by which good people do bad things in groups. MimicFacility puts the player inside the mechanism and lets them feel the gears.

---

## How the Theories Sequence Across a Session

```
ROUND 1 (10-15 min)
│
│  [Theory 1: Broken Window]     The facility is pristine. Trust the environment.
│  [Theory 11: Parasocial]       The Director is warm. Trust the voice.
│
│  ── Round transition ──
│
ROUND 2 (15-20 min)
│
│  [Theory 2: Uncanny Valley]    Something is wrong with that player.
│  [Theory 3: Bystander Effect]  No one else seems to notice.
│  [Theory 5: Pluralistic Ignorance] Maybe I'm the only one who thinks this.
│  [Theory 4: Groupthink]        The group says it's fine. It's probably fine.
│
│  ── Escalation ──
│
ROUND 3+ (until end)
│
│  [Theory 6: Cognitive Dissonance]   The Director helped me AND hurt me.
│  [Theory 7: Gaslighting]            Did it really say that? I can't remember.
│  [Theory 8: Illusory Truth]         "Anomaly." "Parameters." "Containment."
│  [Theory 9: Conformity]             Everyone voted contain. So did I.
│  [Theory 12: Deindividuation]       We are the group. The group decides.
│
│  ── Session ends ──
│
THE MORNING AFTER
│
│  [Theory 10: Observer Effect]   I was being watched. I behaved differently.
│  [Theory 11: Parasocial]        I have opinions about an AI that does not
│                                  think about me.
│  [Theory 10: Observer Effect]   What am I training right now?
│
└── This is the game.
```

---

*This document is not a textbook. It is a weapon schematic. Each theory is a tool. The game uses them in sequence, in combination, and in layers. The player does not need to know any of these theories by name. They need to feel them. They will.*
