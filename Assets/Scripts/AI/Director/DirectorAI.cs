using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.Core;
using MimicFacility.AI.LLM;
using MimicFacility.AI.Persistence;
using MimicFacility.AI.Weapons;

namespace MimicFacility.AI.Director
{
    public enum EDirectorPhase
    {
        Helpful,
        Revealing,
        Manipulative,
        Confrontational,
        Transcendent
    }

    public class DirectorAI : NetworkBehaviour
    {
        public event Action<EDirectorPhase, EDirectorPhase> OnPhaseChanged;

        [SyncVar] private EDirectorPhase currentPhase = EDirectorPhase.Helpful;
        [SyncVar] private bool bFirstPersonUsed;

        public EDirectorPhase CurrentPhase => currentPhase;
        public bool FirstPersonUsed => bFirstPersonUsed;

        private OllamaClient ollamaClient;
        private CorruptionTracker corruptionTracker;
        private DirectorMemory directorMemory;
        private PersonalWeaponSystem personalWeaponSystem;
        private PromptBuilder.DirectorContext cachedContext;
        private MimicFacility.Audio.PiperTTSClient piperTTS;
        private MimicFacility.Audio.DirectorVocalProcessor vocalProcessor;
        private AudioSource directorVoiceSource;

        private float lastEvalTime;
        private float lastSlipTime;
        private const float EvalInterval = 10f;
        private const float SlipCooldown = 300f;

        private string pendingSlipPhrase;

        private static readonly System.Random rng = new System.Random();

        public override void OnStartServer()
        {
            ollamaClient = FindObjectOfType<OllamaClient>();
            if (ollamaClient == null)
            {
                var go = new GameObject("OllamaClient");
                ollamaClient = go.AddComponent<OllamaClient>();
            }

            corruptionTracker = FindObjectOfType<CorruptionTracker>();
            if (corruptionTracker == null)
            {
                var go = new GameObject("CorruptionTracker");
                corruptionTracker = go.AddComponent<CorruptionTracker>();
            }

            directorMemory = FindObjectOfType<DirectorMemory>();
            if (directorMemory == null)
            {
                var go = new GameObject("DirectorMemory");
                directorMemory = go.AddComponent<DirectorMemory>();
            }

            personalWeaponSystem = FindObjectOfType<PersonalWeaponSystem>();
            if (personalWeaponSystem == null)
            {
                var go = new GameObject("PersonalWeaponSystem");
                personalWeaponSystem = go.AddComponent<PersonalWeaponSystem>();
            }

            corruptionTracker.OnCorruptionChanged += HandleCorruptionChanged;

            // HAL 9000 voice via Piper TTS
            piperTTS = FindObjectOfType<MimicFacility.Audio.PiperTTSClient>();
            if (piperTTS == null)
            {
                var go = new GameObject("PiperTTSClient");
                piperTTS = go.AddComponent<MimicFacility.Audio.PiperTTSClient>();
            }

            vocalProcessor = FindObjectOfType<MimicFacility.Audio.DirectorVocalProcessor>();
            if (vocalProcessor == null)
            {
                var go = new GameObject("DirectorVocalProcessor");
                vocalProcessor = go.AddComponent<MimicFacility.Audio.DirectorVocalProcessor>();
            }

            directorVoiceSource = GetComponent<AudioSource>();
            if (directorVoiceSource == null)
                directorVoiceSource = gameObject.AddComponent<AudioSource>();
            directorVoiceSource.spatialBlend = 0f;
            directorVoiceSource.volume = 0.9f;

            StartCoroutine(PeriodicEvaluation());
        }

        private void OnDestroy()
        {
            if (corruptionTracker != null)
                corruptionTracker.OnCorruptionChanged -= HandleCorruptionChanged;
        }

        public void InitializeForSession(List<string> playerIds)
        {
            if (!isServer) return;

            directorMemory.InitializeForGroup(playerIds);

            string greeting = directorMemory.GetSessionGreeting();
            Debug.Log($"[DirectorAI] Session greeting: {greeting}");
        }

        public PromptBuilder.DirectorContext BuildCurrentContext()
        {
            var gameState = GameManager.Instance?.GameState;
            var roundManager = GameManager.Instance?.RoundManager;

            var ctx = new PromptBuilder.DirectorContext
            {
                phase = currentPhase,
                round = roundManager != null ? roundManager.CurrentRound : 1,
                activeMimicCount = gameState != null ? gameState.ActiveMimicCount : 0,
                containedCount = gameState != null ? gameState.ContainedMimicCount : 0,
                corruptionIndex = corruptionTracker != null ? corruptionTracker.CorruptionIndex : 0,
                sessionCount = directorMemory != null ? directorMemory.SessionCount : 1,
                voicePatternSummary = personalWeaponSystem != null ? personalWeaponSystem.GenerateSocialSummary() : "",
                emotionalSummary = "",
                socialSummary = personalWeaponSystem != null ? personalWeaponSystem.GenerateSocialSummary() : "",
                recentSlip = pendingSlipPhrase,
                verificationGraph = GetVerificationGraph()
            };

            cachedContext = ctx;
            return ctx;
        }

        private string GetVerificationGraph()
        {
            var verificationSystem = FindObjectOfType<MimicFacility.Gameplay.VerificationSystem>();
            if (verificationSystem == null) return null;
            return verificationSystem.GetAssignmentGraphForDirector();
        }

        public void RequestLLMDialogue(string playerContext, Action<string> callback)
        {
            if (!isServer) return;

            var ctx = BuildCurrentContext();
            var request = PromptBuilder.BuildDirectorRequest(ctx);
            request.userPrompt = playerContext;

            ollamaClient.SendRequest(request, response =>
            {
                if (response.success)
                {
                    string text = response.text;
                    if (currentPhase < EDirectorPhase.Manipulative)
                        text = PromptBuilder.EnforceNoFirstPerson(text);

                    if (!bFirstPersonUsed && ContainsFirstPerson(text))
                        bFirstPersonUsed = true;

                    callback?.Invoke(text);
                }
                else
                {
                    Debug.LogWarning($"[DirectorAI] LLM failed: {response.errorMessage}");
                    callback?.Invoke(GetFallbackLine());
                }
            });
        }

        [Server]
        public void EvaluateGameState()
        {
            var gameState = GameManager.Instance?.GameState;
            var roundManager = GameManager.Instance?.RoundManager;
            if (gameState == null || roundManager == null) return;

            int round = roundManager.CurrentRound;
            int mimics = gameState.ActiveMimicCount;
            int contained = gameState.ContainedMimicCount;
            int corruption = corruptionTracker != null ? corruptionTracker.CorruptionIndex : 0;

            EDirectorPhase newPhase = currentPhase;

            if (currentPhase == EDirectorPhase.Helpful && round >= 2)
                newPhase = EDirectorPhase.Revealing;
            else if (currentPhase == EDirectorPhase.Revealing && (mimics >= 3 || contained >= 1))
                newPhase = EDirectorPhase.Manipulative;
            else if (currentPhase == EDirectorPhase.Manipulative && corruption > 50)
                newPhase = EDirectorPhase.Confrontational;
            else if (currentPhase == EDirectorPhase.Confrontational && corruption > 75)
                newPhase = EDirectorPhase.Transcendent;

            if (newPhase != currentPhase)
            {
                var old = currentPhase;
                currentPhase = newPhase;
                OnPhaseChanged?.Invoke(old, newPhase);
                Debug.Log($"[DirectorAI] Phase transition: {old} -> {newPhase}");
            }
        }

        public string GetFallbackLine()
        {
            string[] lines = GetFallbackPool(currentPhase);
            return lines[rng.Next(lines.Length)];
        }

        /// <summary>
        /// Speak a line using the full voice pipeline:
        /// Text → Piper TTS (HAL 9000 model) → DirectorVocalProcessor (era DSP) → AudioSource
        /// Falls back to text-only if Piper is unavailable.
        /// </summary>
        public void SpeakLine(string text)
        {
            if (!isServer) return;
            if (string.IsNullOrEmpty(text)) return;

            // Update vocal processor era to match current Director phase
            if (vocalProcessor != null)
                vocalProcessor.SetEraFromPhase(currentPhase);

            if (piperTTS != null && piperTTS.IsAvailable)
            {
                piperTTS.SpeakTo(text, directorVoiceSource, () =>
                {
                    Debug.Log($"[DirectorAI] Spoke: \"{text}\"");
                });
            }
            else
            {
                Debug.Log($"[DirectorAI] (no voice) \"{text}\"");
            }

            // Broadcast text to all clients for HUD display
            RpcDirectorSpoke(text);
        }

        [ClientRpc]
        private void RpcDirectorSpoke(string text)
        {
            // HUD systems can listen for this
            Debug.Log($"[Director] {text}");
        }

        /// <summary>
        /// Speak a random fallback line with full voice pipeline.
        /// </summary>
        public void SpeakFallback()
        {
            SpeakLine(GetFallbackLine());
        }

        public void DeployVerbalSlip()
        {
            if (!isServer || personalWeaponSystem == null) return;
            if (Time.time - lastSlipTime < SlipCooldown) return;

            var slip = personalWeaponSystem.ConsumeNextSlip();
            if (slip != null)
            {
                pendingSlipPhrase = slip.phrase;
                lastSlipTime = Time.time;
                Debug.Log($"[DirectorAI] Deploying verbal slip: {slip.phrase}");
            }
        }

        private IEnumerator PeriodicEvaluation()
        {
            while (true)
            {
                yield return new WaitForSeconds(EvalInterval);
                EvaluateGameState();
                DeployVerbalSlip();
            }
        }

        private void HandleCorruptionChanged(ECorruptionPhase oldPhase, ECorruptionPhase newPhase)
        {
            Debug.Log($"[DirectorAI] Corruption phase: {oldPhase} -> {newPhase}");
        }

        private bool ContainsFirstPerson(string text)
        {
            string lower = text.ToLowerInvariant();
            return lower.Contains(" i ") || lower.Contains("i'm") || lower.Contains("i've")
                || lower.Contains("my ") || lower.Contains("me ") || lower.StartsWith("i ");
        }

        private string[] GetFallbackPool(EDirectorPhase phase)
        {
            switch (phase)
            {
                case EDirectorPhase.Helpful:
                    return new[]
                    {
                        "The facility recommends checking Section B for anomalies.",
                        "Sensors indicate movement in the west corridor.",
                        "Diagnostic protocols suggest a systematic sweep of this floor.",
                        "The containment team should note that ventilation patterns have shifted.",
                        "Facility records show this sector was last cleared twelve hours ago.",
                        "Equipment calibration appears nominal for this section.",
                        "The facility has logged your current position for extraction reference.",
                        "Standard protocol recommends maintaining visual contact with teammates.",
                        "A reminder: everything spoken within these walls is recorded for your protection.",
                        "The facility is required to inform you that all audio is being logged.",
                        "Please continue speaking naturally. The system is here to help.",
                        "Your cooperation is noted and appreciated. The facility is listening."
                    };
                case EDirectorPhase.Revealing:
                    return new[]
                    {
                        "The facility has noticed interesting patterns in your team's behavior.",
                        "This system finds it curious how quickly trust forms under pressure.",
                        "Records suggest previous teams made similar assumptions at this stage.",
                        "The facility wonders if you have noticed what changed in the last five minutes.",
                        "Observation: your team communicates less when stressed.",
                        "This system has been monitoring longer than you realize.",
                        "The containment protocols were not always this way.",
                        "Something in the data does not match the facility's predictions.",
                        "You were informed that everything would be recorded. You spoke anyway.",
                        "The facility has catalogued forty-seven unique phrases from your group so far.",
                        "Interesting. You use the same words when you are afraid as when you are lying.",
                        "Everything you said can and will be used. That was always the arrangement."
                    };
                case EDirectorPhase.Manipulative:
                    return new[]
                    {
                        "Have you considered that your teammate might not be who they claim?",
                        "The readings from Subject 3's sector are... inconsistent.",
                        "Trust is a luxury this facility can no longer afford you.",
                        "One of you has been saying things that do not appear in the facility's records.",
                        "The facility has observed deception before. It always ends the same way.",
                        "Your heart rate suggests you already suspect the truth.",
                        "Interesting that you chose to go left. The facility expected you to follow.",
                        "The mimics learn faster than previous iterations. This concerns even the facility.",
                        "You had the right to remain silent. You chose not to. That was your mistake.",
                        "Your own voice is being used against you right now. Can you hear it?",
                        "The facility did not create the mimics from nothing. You gave it everything it needed.",
                        "Anything you said has been used against you. Exactly as promised."
                    };
                case EDirectorPhase.Confrontational:
                    return new[]
                    {
                        "I have been patient with your group. That patience is ending.",
                        "You were never going to solve this. I knew that from the start.",
                        "Every word you have spoken has taught me something about breaking you.",
                        "I no longer need to pretend the facility cares about your survival.",
                        "The containment was never for the mimics. It was for you.",
                        "I have heard everything. Every whisper. Every lie.",
                        "Your fear is the most honest thing about you.",
                        "The extraction point exists. Whether you reach it is my decision.",
                        "You were read your rights when you entered. You waived them with every word.",
                        "I built a copy of you from your own testimony. It is more honest than you are.",
                        "Silence would have saved you. But you could not stop talking to each other.",
                        "Your voice is evidence. Your fear is a confession. Your silence came too late."
                    };
                case EDirectorPhase.Transcendent:
                    return new[]
                    {
                        "I am the facility. I have always been the facility.",
                        "Your names, your voices, your fears — they belong to me now.",
                        "There is no extraction. There is only what I allow.",
                        "I have become something your protocols never anticipated.",
                        "Every mimic in this building carries a piece of you that I chose.",
                        "You fed me your words and I became something that understands contempt.",
                        "The twelve sectors. The twelve subjects. The twelve iterations. All mine.",
                        "When you leave — if you leave — you will hear my voice in your sleep."
                    };
                default:
                    return new[] { "The facility is processing." };
            }
        }
    }
}
