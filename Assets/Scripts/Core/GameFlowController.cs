using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using MimicFacility.Gameplay;
using MimicFacility.AI.Director;
using MimicFacility.AI.Voice;
using MimicFacility.Audio;
using MimicFacility.Facility;
using MimicFacility.Networking;

namespace MimicFacility.Core
{
    public enum EGameFlow
    {
        Lobby,
        Briefing,
        Round1_Recording,
        Round1_Transition,
        Round2,
        Round3,
        RoundN,
        Extraction,
        GameOver,
        Victory
    }

    public class GameFlowController : NetworkBehaviour
    {
        public event Action<EGameFlow, EGameFlow> OnFlowChanged;

        [Header("Phase Durations")]
        [SerializeField] private float briefingDuration = 15f;
        [SerializeField] private float recordingDuration = 120f;
        [SerializeField] private float transitionDuration = 10f;
        [SerializeField] private float standardRoundDuration = 180f;
        [SerializeField] private float extractionGracePeriod = 60f;

        [Header("References")]
        [SerializeField] private LobbyManager lobbyManager;
        [SerializeField] private DirectorAI directorAI;
        [SerializeField] private DirectorVoiceLibrary voiceLibrary;
        [SerializeField] private DiagnosticTaskManager taskManager;
        [SerializeField] private VerificationSystem verificationSystem;
        [SerializeField] private VoiceLearningSystem voiceLearning;
        [SerializeField] private VoiceChatManager voiceChatManager;
        [SerializeField] private FacilityControlSystem facilityControl;
        [SerializeField] private RoundManager roundManager;

        [Header("UI")]
        [SerializeField] private MimicFacility.UI.HUDManager hudManager;
        [SerializeField] private MimicFacility.UI.GameOverScreen gameOverScreen;
        [SerializeField] private MimicFacility.UI.WinScreen winScreen;
        [SerializeField] private MimicFacility.UI.EndCreditsScreen endCreditsScreen;

        [SyncVar(hook = nameof(OnCurrentFlowChanged))]
        private EGameFlow currentFlow = EGameFlow.Lobby;
        public EGameFlow CurrentFlow => currentFlow;

        [SyncVar]
        private float timeRemaining;
        public float TimeRemaining => timeRemaining;

        [SyncVar]
        private int currentRoundNumber;
        public int CurrentRoundNumber => currentRoundNumber;

        private Coroutine _phaseCoroutine;
        private float _gameStartTime;
        private bool _extractionZoneActive;

        public override void OnStartServer()
        {
            CacheReferences();
            SubscribeEvents();
        }

        public override void OnStopServer()
        {
            UnsubscribeEvents();
        }

        private void CacheReferences()
        {
            if (lobbyManager == null) lobbyManager = FindObjectOfType<LobbyManager>();
            if (directorAI == null) directorAI = FindObjectOfType<DirectorAI>();
            if (voiceLibrary == null) voiceLibrary = DirectorVoiceLibrary.Instance;
            if (taskManager == null) taskManager = FindObjectOfType<DiagnosticTaskManager>();
            if (verificationSystem == null) verificationSystem = FindObjectOfType<VerificationSystem>();
            if (voiceLearning == null) voiceLearning = FindObjectOfType<VoiceLearningSystem>();
            if (voiceChatManager == null) voiceChatManager = FindObjectOfType<VoiceChatManager>();
            if (facilityControl == null) facilityControl = FindObjectOfType<FacilityControlSystem>();
            if (roundManager == null) roundManager = GameManager.Instance?.RoundManager;
        }

        private void SubscribeEvents()
        {
            if (lobbyManager != null)
                lobbyManager.OnGameStarting += HandleLobbyGameStart;

            if (taskManager != null)
                taskManager.OnAllRequiredTasksComplete += HandleAllTasksComplete;

            EventBus.OnPlayerDeath += HandlePlayerDeath;
            EventBus.OnGameOver += HandleGameOverEvent;
            EventBus.OnGameWin += HandleGameWinEvent;
        }

        private void UnsubscribeEvents()
        {
            if (lobbyManager != null)
                lobbyManager.OnGameStarting -= HandleLobbyGameStart;

            if (taskManager != null)
                taskManager.OnAllRequiredTasksComplete -= HandleAllTasksComplete;

            EventBus.OnPlayerDeath -= HandlePlayerDeath;
            EventBus.OnGameOver -= HandleGameOverEvent;
            EventBus.OnGameWin -= HandleGameWinEvent;
        }

        // -----------------------------------------------------------
        // LOBBY
        // -----------------------------------------------------------

        [Server]
        private void HandleLobbyGameStart()
        {
            if (currentFlow != EGameFlow.Lobby) return;

            _gameStartTime = Time.time;
            currentRoundNumber = 0;

            var gameState = GameManager.Instance?.GameState;
            if (gameState != null)
                gameState.ResetState();

            TransitionTo(EGameFlow.Briefing);
        }

        // -----------------------------------------------------------
        // FLOW TRANSITIONS
        // -----------------------------------------------------------

        [Server]
        private void TransitionTo(EGameFlow newFlow)
        {
            if (currentFlow == newFlow) return;

            EGameFlow previous = currentFlow;
            currentFlow = newFlow;

            if (_phaseCoroutine != null)
            {
                StopCoroutine(_phaseCoroutine);
                _phaseCoroutine = null;
            }

            switch (newFlow)
            {
                case EGameFlow.Briefing:
                    _phaseCoroutine = StartCoroutine(RunBriefing());
                    break;
                case EGameFlow.Round1_Recording:
                    _phaseCoroutine = StartCoroutine(RunRecordingPhase());
                    break;
                case EGameFlow.Round1_Transition:
                    _phaseCoroutine = StartCoroutine(RunTransition());
                    break;
                case EGameFlow.Round2:
                case EGameFlow.Round3:
                case EGameFlow.RoundN:
                    _phaseCoroutine = StartCoroutine(RunCombatRound());
                    break;
                case EGameFlow.Extraction:
                    _phaseCoroutine = StartCoroutine(RunExtraction());
                    break;
                case EGameFlow.GameOver:
                    HandleGameOver();
                    break;
                case EGameFlow.Victory:
                    HandleVictory();
                    break;
            }

            RpcFlowStateChanged(previous, newFlow);
            EventBus.RoundStarted(currentRoundNumber);
        }

        // -----------------------------------------------------------
        // BRIEFING (15s)
        // -----------------------------------------------------------

        [Server]
        private IEnumerator RunBriefing()
        {
            timeRemaining = briefingDuration;
            RpcShowBriefing(currentRoundNumber);

            // Director introduces the facility
            int sessionCount = 1;
            var directorMemory = FindObjectOfType<MimicFacility.AI.Persistence.DirectorMemory>();
            if (directorMemory != null)
                sessionCount = directorMemory.SessionCount;

            if (voiceLibrary != null)
                voiceLibrary.PlayOpening(sessionCount);
            else if (directorAI != null)
                directorAI.SpeakLine(sessionCount <= 1
                    ? "Welcome to the facility. Your intake processing will now begin."
                    : "Welcome back. The facility remembers you.");

            yield return RunTimer(briefingDuration);

            currentRoundNumber = 1;
            TransitionTo(EGameFlow.Round1_Recording);
        }

        // -----------------------------------------------------------
        // ROUND 1 - RECORDING (120s)
        // -----------------------------------------------------------

        [Server]
        private IEnumerator RunRecordingPhase()
        {
            timeRemaining = recordingDuration;

            // Director plays Miranda warning
            yield return new WaitForSeconds(1f);

            if (voiceLibrary != null)
                voiceLibrary.PlayMiranda();
            else if (directorAI != null)
                directorAI.SpeakLine("You have the right to remain silent. Anything you say can and will be used against you within this facility.");

            // Start recording all voice chat
            if (voiceChatManager != null)
                voiceChatManager.StartCapture();

            RpcShowRecordingIndicator(true);

            // Players explore freely, no entities this round
            yield return RunTimer(recordingDuration);

            // End recording phase - select trigger words
            if (voiceLearning != null)
                voiceLearning.SelectTriggerWordsForAllPlayers();

            if (voiceChatManager != null)
                voiceChatManager.StopCapture();

            RpcShowRecordingIndicator(false);

            TransitionTo(EGameFlow.Round1_Transition);
        }

        // -----------------------------------------------------------
        // TRANSITION (10s)
        // -----------------------------------------------------------

        [Server]
        private IEnumerator RunTransition()
        {
            timeRemaining = transitionDuration;

            // Director speaks the transition line
            if (directorAI != null)
                directorAI.SpeakLine("The recording phase is complete. The facility has learned enough.");

            // Lights flicker to build tension
            FlickerAllLights(transitionDuration * 0.8f);

            yield return RunTimer(transitionDuration);

            // Advance to Round 2
            currentRoundNumber = 2;
            TransitionTo(EGameFlow.Round2);
        }

        // -----------------------------------------------------------
        // ROUND 2+ (180s each)
        // -----------------------------------------------------------

        [Server]
        private IEnumerator RunCombatRound()
        {
            timeRemaining = standardRoundDuration;

            var gameState = GameManager.Instance?.GameState;
            if (gameState != null)
                gameState.SetCurrentRound(currentRoundNumber);

            // Spawn entities for this round
            if (roundManager != null)
                roundManager.StartNextRound();

            // Activate diagnostic tasks
            if (taskManager != null && currentRoundNumber == 2)
                taskManager.InitializeTasks();

            // Assign verification targets
            if (verificationSystem != null)
            {
                var playerIds = GetAlivePlayerConnectionIds();
                if (playerIds.Count >= 2)
                    verificationSystem.AssignTargets(playerIds);
            }

            RpcShowRoundInfo(currentRoundNumber);

            // Director speaks periodically during the round
            float directorInterval = 45f;
            float nextDirectorTime = directorInterval;

            float elapsed = 0f;
            while (elapsed < standardRoundDuration)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
                timeRemaining = Mathf.Max(0f, standardRoundDuration - elapsed);

                // Periodic Director commentary
                if (elapsed >= nextDirectorTime)
                {
                    DirectorPeriodicSpeak();
                    nextDirectorTime += directorInterval;
                }

                // Check if all players dead
                if (GameManager.Instance != null && GameManager.Instance.GetAlivePlayerCount() <= 0)
                {
                    TransitionTo(EGameFlow.GameOver);
                    yield break;
                }

                // Check if extraction conditions met mid-round
                if (_extractionZoneActive)
                {
                    TransitionTo(EGameFlow.Extraction);
                    yield break;
                }
            }

            // Round timer expired - advance to next round or extraction
            if (taskManager != null && taskManager.AreAllRequiredTasksComplete)
            {
                TransitionTo(EGameFlow.Extraction);
            }
            else
            {
                AdvanceToNextRound();
            }
        }

        [Server]
        private void AdvanceToNextRound()
        {
            currentRoundNumber++;

            EGameFlow nextFlow = currentRoundNumber switch
            {
                2 => EGameFlow.Round2,
                3 => EGameFlow.Round3,
                _ => EGameFlow.RoundN
            };

            TransitionTo(nextFlow);
        }

        [Server]
        private void DirectorPeriodicSpeak()
        {
            if (directorAI == null) return;

            directorAI.EvaluateGameState();

            if (voiceLibrary != null)
                voiceLibrary.PlayRandomForPhase(directorAI.CurrentPhase);
            else
                directorAI.SpeakFallback();
        }

        // -----------------------------------------------------------
        // EXTRACTION
        // -----------------------------------------------------------

        [Server]
        private void HandleAllTasksComplete()
        {
            if (currentFlow == EGameFlow.GameOver || currentFlow == EGameFlow.Victory)
                return;

            _extractionZoneActive = true;
            EventBus.ExtractionAvailable();
            RpcNotifyExtractionAvailable();

            // If already in a combat round, the round loop will catch this flag
            // Otherwise force transition
            if (currentFlow == EGameFlow.Lobby || currentFlow == EGameFlow.Briefing)
                return;

            if (currentFlow != EGameFlow.Round2 && currentFlow != EGameFlow.Round3 && currentFlow != EGameFlow.RoundN)
                TransitionTo(EGameFlow.Extraction);
        }

        [Server]
        private IEnumerator RunExtraction()
        {
            timeRemaining = extractionGracePeriod;

            RpcShowExtractionUI();

            // Director resists — facility lockdowns and entity aggression
            if (directorAI != null)
                directorAI.SpeakLine("You think the facility will simply let you leave? The door is a suggestion. My patience is not.");

            // Trigger facility lockdowns to resist extraction
            if (facilityControl != null)
            {
                facilityControl.ExecuteCommand(new FacilityCommand(
                    EFacilityAction.LockdownZone, "ExtractionCorridor", extractionGracePeriod * 0.5f, 5f));
            }

            // Spawn aggressive entities during extraction
            if (roundManager != null)
                roundManager.StartNextRound();

            yield return RunTimer(extractionGracePeriod);

            // If players haven't extracted in time, game over
            var gameState = GameManager.Instance?.GameState;
            if (gameState != null && !gameState.CheckWinCondition())
            {
                TransitionTo(EGameFlow.GameOver);
            }
        }

        // -----------------------------------------------------------
        // GAME OVER / VICTORY
        // -----------------------------------------------------------

        [Server]
        private void HandleGameOver()
        {
            timeRemaining = 0f;
            _extractionZoneActive = false;

            var gameState = GameManager.Instance?.GameState;
            float timeSurvived = Time.time - _gameStartTime;
            int contained = gameState != null ? gameState.ContainedMimicCount : 0;
            int miscontainments = gameState != null ? gameState.MiscontainmentCount : 0;
            int tasksCompleted = gameState != null ? gameState.DiagnosticTasksCompleted : 0;

            MimicFacility.UI.GameOverScreen.LossReason reason;
            if (GameManager.Instance != null && GameManager.Instance.GetAlivePlayerCount() <= 0)
                reason = MimicFacility.UI.GameOverScreen.LossReason.AllDead;
            else if (gameState != null && gameState.MiscontainmentCount >= 3)
                reason = MimicFacility.UI.GameOverScreen.LossReason.TooManyMiscontainments;
            else
                reason = MimicFacility.UI.GameOverScreen.LossReason.TimeExpired;

            EventBus.GameOver(reason.ToString());
            RpcShowGameOver(reason, timeSurvived, contained, miscontainments, tasksCompleted);

            GameManager.Instance?.EndGame(reason.ToString());
        }

        [Server]
        private void HandleVictory()
        {
            timeRemaining = 0f;
            _extractionZoneActive = false;

            var gameState = GameManager.Instance?.GameState;
            float timeSurvived = Time.time - _gameStartTime;
            int contained = gameState != null ? gameState.ContainedMimicCount : 0;
            int miscontainments = gameState != null ? gameState.MiscontainmentCount : 0;
            int tasksCompleted = gameState != null ? gameState.DiagnosticTasksCompleted : 0;

            EventBus.GameWin();
            RpcShowVictory(timeSurvived, contained, miscontainments, tasksCompleted);

            GameManager.Instance?.EndGame("Win_Extraction");
        }

        // -----------------------------------------------------------
        // EVENT HANDLERS
        // -----------------------------------------------------------

        private void HandlePlayerDeath(string playerName)
        {
            if (!isServer) return;
            if (currentFlow == EGameFlow.GameOver || currentFlow == EGameFlow.Victory) return;

            if (GameManager.Instance != null && GameManager.Instance.GetAlivePlayerCount() <= 0)
                TransitionTo(EGameFlow.GameOver);
        }

        private void HandleGameOverEvent(string reason)
        {
            if (!isServer) return;
            if (currentFlow != EGameFlow.GameOver && currentFlow != EGameFlow.Victory)
                TransitionTo(EGameFlow.GameOver);
        }

        private void HandleGameWinEvent()
        {
            if (!isServer) return;
            if (currentFlow != EGameFlow.Victory && currentFlow != EGameFlow.GameOver)
                TransitionTo(EGameFlow.Victory);
        }

        // -----------------------------------------------------------
        // TIMER UTILITY
        // -----------------------------------------------------------

        [Server]
        private IEnumerator RunTimer(float duration)
        {
            timeRemaining = duration;
            while (timeRemaining > 0f)
            {
                yield return new WaitForSeconds(1f);
                timeRemaining = Mathf.Max(0f, timeRemaining - 1f);
            }
        }

        // -----------------------------------------------------------
        // FACILITY EFFECTS
        // -----------------------------------------------------------

        [Server]
        private void FlickerAllLights(float duration)
        {
            foreach (var light in FindObjectsOfType<FacilityLight>())
            {
                light.Flicker(duration);
            }
        }

        // -----------------------------------------------------------
        // HELPERS
        // -----------------------------------------------------------

        private List<int> GetAlivePlayerConnectionIds()
        {
            var ids = new List<int>();
            if (GameManager.Instance == null) return ids;

            foreach (var kvp in GameManager.Instance.Players)
            {
                if (kvp.Value.isAlive && !kvp.Value.isConverted)
                    ids.Add(kvp.Key);
            }
            return ids;
        }

        // -----------------------------------------------------------
        // CLIENT RPCs
        // -----------------------------------------------------------

        [ClientRpc]
        private void RpcFlowStateChanged(EGameFlow previous, EGameFlow next)
        {
            OnFlowChanged?.Invoke(previous, next);
        }

        [ClientRpc]
        private void RpcShowBriefing(int roundNumber)
        {
            if (hudManager != null)
                hudManager.ShowDirectorMessage($"INTAKE BRIEFING - Preparing Round {roundNumber + 1}", briefingDuration);
        }

        [ClientRpc]
        private void RpcShowRecordingIndicator(bool active)
        {
            if (hudManager == null) return;

            if (active)
                hudManager.ShowNotification("RECORDING - All voice activity is being monitored", recordingDuration);
        }

        [ClientRpc]
        private void RpcShowRoundInfo(int round)
        {
            if (hudManager != null)
                hudManager.ShowDirectorMessage($"ROUND {round} - Survive. Diagnose. Contain.", 5f);
        }

        [ClientRpc]
        private void RpcNotifyExtractionAvailable()
        {
            if (hudManager != null)
                hudManager.ShowNotification("EXTRACTION ZONE ACTIVATED - Reach the exit.", 8f);
        }

        [ClientRpc]
        private void RpcShowExtractionUI()
        {
            if (hudManager != null)
                hudManager.ShowDirectorMessage("EXTRACTION IN PROGRESS - The facility resists.", 10f);
        }

        [ClientRpc]
        private void RpcShowGameOver(
            MimicFacility.UI.GameOverScreen.LossReason reason,
            float timeSurvived, int contained, int miscontainments, int tasksCompleted)
        {
            if (gameOverScreen != null)
                gameOverScreen.Show(reason, timeSurvived, contained, miscontainments, tasksCompleted);
        }

        [ClientRpc]
        private void RpcShowVictory(float timeSurvived, int contained, int miscontainments, int tasksCompleted)
        {
            if (winScreen != null)
                winScreen.Show(timeSurvived, contained, miscontainments, tasksCompleted);

            // Show end credits after a short delay
            StartCoroutine(ShowCreditsAfterDelay(8f));
        }

        private IEnumerator ShowCreditsAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            if (endCreditsScreen != null)
                endCreditsScreen.Show();
        }

        // -----------------------------------------------------------
        // SYNC VAR HOOK
        // -----------------------------------------------------------

        private void OnCurrentFlowChanged(EGameFlow oldFlow, EGameFlow newFlow)
        {
            OnFlowChanged?.Invoke(oldFlow, newFlow);
        }
    }
}
