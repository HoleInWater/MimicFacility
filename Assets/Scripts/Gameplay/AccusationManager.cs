using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using MimicFacility.Core;
using MimicFacility.AI.Director;
using MimicFacility.Characters;

namespace MimicFacility.Gameplay
{
    public enum EAccusationPhase
    {
        Idle,
        Deliberation,
        Voting,
        Resolving
    }

    public enum EAccusationVote
    {
        NoVote,
        Contain,
        Release
    }

    public enum EAccusationResult
    {
        MimicContained,
        FalsePositive,
        MimicReleased,
        RealReleased
    }

    [Serializable]
    public class AccusationRecord
    {
        public string accuserPlayerId;
        public string accusedPlayerId;
        public Dictionary<string, EAccusationVote> votes = new Dictionary<string, EAccusationVote>();
        public EAccusationResult result;
        public bool wasMimic;
        public float timestamp;
    }

    public class AccusationManager : NetworkBehaviour
    {
        public event Action<string, string> OnAccusationStarted;
        public event Action OnVotingStarted;
        public event Action<AccusationRecord> OnAccusationResolved;

        [SyncVar(hook = nameof(OnPhaseChanged))]
        private EAccusationPhase currentPhase = EAccusationPhase.Idle;
        public EAccusationPhase CurrentPhase => currentPhase;

        [SyncVar] private string accusedPlayerId;
        [SyncVar] private string accuserPlayerId;

        private readonly List<AccusationRecord> history = new List<AccusationRecord>();
        public IReadOnlyList<AccusationRecord> History => history;

        private readonly Dictionary<string, EAccusationVote> currentVotes = new Dictionary<string, EAccusationVote>();
        private float lastAccusationTime = -60f;
        private Coroutine phaseCoroutine;

        private const float DeliberationDuration = 15f;
        private const float VotingDuration = 10f;
        private const float AccusationCooldown = 60f;

        [Command(requiresAuthority = false)]
        public void CmdInitiateAccusation(uint accusedNetId, NetworkConnectionToClient sender = null)
        {
            if (currentPhase != EAccusationPhase.Idle) return;
            if (Time.time - lastAccusationTime < AccusationCooldown) return;

            string accuserId = sender != null ? sender.connectionId.ToString() : "0";

            var accusedIdentity = NetworkServer.spawned.ContainsKey(accusedNetId)
                ? NetworkServer.spawned[accusedNetId]
                : null;
            if (accusedIdentity == null) return;

            var accusedState = accusedIdentity.GetComponent<MimicPlayerState>();
            if (accusedState == null || !accusedState.IsAlive) return;

            accuserPlayerId = accuserId;
            accusedPlayerId = accusedIdentity.connectionToClient != null
                ? accusedIdentity.connectionToClient.connectionId.ToString()
                : accusedNetId.ToString();

            currentVotes.Clear();
            currentPhase = EAccusationPhase.Deliberation;
            lastAccusationTime = Time.time;

            RpcAccusationStarted(accuserPlayerId, accusedPlayerId);

            if (phaseCoroutine != null) StopCoroutine(phaseCoroutine);
            phaseCoroutine = StartCoroutine(PhaseSequence());
        }

        [Command(requiresAuthority = false)]
        public void CmdCastVote(EAccusationVote vote, NetworkConnectionToClient sender = null)
        {
            if (currentPhase != EAccusationPhase.Voting) return;

            string voterId = sender != null ? sender.connectionId.ToString() : "0";
            currentVotes[voterId] = vote;

            int expectedVoters = NetworkServer.connections.Count;
            if (currentVotes.Count >= expectedVoters)
                ResolveAccusation();
        }

        private IEnumerator PhaseSequence()
        {
            yield return new WaitForSeconds(DeliberationDuration);

            if (currentPhase != EAccusationPhase.Deliberation) yield break;

            currentPhase = EAccusationPhase.Voting;
            RpcVotingStarted();

            yield return new WaitForSeconds(VotingDuration);

            if (currentPhase == EAccusationPhase.Voting)
                ResolveAccusation();
        }

        [Server]
        private void ResolveAccusation()
        {
            if (currentPhase == EAccusationPhase.Resolving || currentPhase == EAccusationPhase.Idle) return;

            currentPhase = EAccusationPhase.Resolving;
            if (phaseCoroutine != null)
            {
                StopCoroutine(phaseCoroutine);
                phaseCoroutine = null;
            }

            int containVotes = 0;
            int releaseVotes = 0;

            foreach (var kvp in currentVotes)
            {
                if (kvp.Value == EAccusationVote.Contain) containVotes++;
                else if (kvp.Value == EAccusationVote.Release) releaseVotes++;
            }

            bool containWins;
            if (containVotes == releaseVotes)
                containWins = GetDirectorTieBreak();
            else
                containWins = containVotes > releaseVotes;

            bool isMimic = CheckIfMimic(accusedPlayerId);
            EAccusationResult result;

            if (containWins)
                result = isMimic ? EAccusationResult.MimicContained : EAccusationResult.FalsePositive;
            else
                result = isMimic ? EAccusationResult.MimicReleased : EAccusationResult.RealReleased;

            var record = new AccusationRecord
            {
                accuserPlayerId = accuserPlayerId,
                accusedPlayerId = accusedPlayerId,
                votes = new Dictionary<string, EAccusationVote>(currentVotes),
                result = result,
                wasMimic = isMimic,
                timestamp = Time.time
            };
            history.Add(record);

            ApplyAccusationResult(result);
            RpcAccusationResolved(accuserPlayerId, accusedPlayerId, (int)result, isMimic);

            currentPhase = EAccusationPhase.Idle;
            accuserPlayerId = string.Empty;
            accusedPlayerId = string.Empty;
        }

        [Server]
        private void ApplyAccusationResult(EAccusationResult result)
        {
            var gameState = GameManager.Instance?.GameState;
            if (gameState == null) return;

            switch (result)
            {
                case EAccusationResult.MimicContained:
                    gameState.IncrementContained();
                    break;
                case EAccusationResult.FalsePositive:
                    gameState.IncrementFalsePositive();
                    break;
            }
        }

        private bool GetDirectorTieBreak()
        {
            var director = FindObjectOfType<DirectorAI>();
            if (director == null) return true;

            bool manipulative = director.CurrentPhase >= EDirectorPhase.Manipulative;
            bool isMimic = CheckIfMimic(accusedPlayerId);

            if (manipulative)
                return !isMimic;

            return isMimic;
        }

        private bool CheckIfMimic(string playerId)
        {
            foreach (var identity in NetworkServer.spawned.Values)
            {
                var conn = identity.connectionToClient;
                if (conn != null && conn.connectionId.ToString() == playerId)
                    return identity.GetComponent<MimicBase>() != null;
            }
            return false;
        }

        public int GetFalsePositiveCount()
        {
            return history.Count(r => r.result == EAccusationResult.FalsePositive);
        }

        [ClientRpc]
        private void RpcAccusationStarted(string accuser, string accused)
        {
            OnAccusationStarted?.Invoke(accuser, accused);
        }

        [ClientRpc]
        private void RpcVotingStarted()
        {
            OnVotingStarted?.Invoke();
        }

        [ClientRpc]
        private void RpcAccusationResolved(string accuser, string accused, int resultInt, bool wasMimic)
        {
            var result = (EAccusationResult)resultInt;
            var record = new AccusationRecord
            {
                accuserPlayerId = accuser,
                accusedPlayerId = accused,
                result = result,
                wasMimic = wasMimic,
                timestamp = Time.time
            };
            OnAccusationResolved?.Invoke(record);
        }

        private void OnPhaseChanged(EAccusationPhase oldPhase, EAccusationPhase newPhase) { }
    }

    public abstract class MimicBase : NetworkBehaviour
    {
        public abstract void Contain();
    }
}
