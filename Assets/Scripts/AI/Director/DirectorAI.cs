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
                recentSlip = pendingSlipPhrase
            };

            cachedContext = ctx;
            return ctx;
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
                        "Standard protocol recommends maintaining visual contact with teammates."
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
                        "Something in the data does not match the facility's predictions."
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
                        "The mimics learn faster than previous iterations. This concerns even the facility."
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
                        "The extraction point exists. Whether you reach it is my decision."
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
