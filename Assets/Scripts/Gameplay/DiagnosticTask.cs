using System;
using System.Collections;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;

namespace MimicFacility.Gameplay
{
    public enum EDiagnosticTaskState
    {
        Inactive,
        Available,
        InProgress,
        VulnerabilityWindow,
        Completed,
        Failed
    }

    public enum EDiagnosticTaskType
    {
        PowerCalibration,
        AtmosphericSampling,
        DataRecovery,
        BioScan,
        StructuralAnalysis,
        CommRelay,
        ContainmentVerification
    }

    public abstract class DiagnosticTask : NetworkBehaviour
    {
        public event Action<DiagnosticTask> OnTaskCompleted;
        public event Action<DiagnosticTask> OnTaskFailed;

        [SyncVar(hook = nameof(OnTaskStateChanged))]
        private EDiagnosticTaskState taskState = EDiagnosticTaskState.Inactive;
        public EDiagnosticTaskState TaskState => taskState;

        [SerializeField] private EDiagnosticTaskType taskType;
        [SerializeField] private float taskTimeLimit = 90f;
        [SerializeField] private float vulnerabilityDuration = 15f;
        [SerializeField] private string taskZoneTag;

        public EDiagnosticTaskType TaskType => taskType;
        public float VulnerabilityDuration => vulnerabilityDuration;
        public string TaskZoneTag => taskZoneTag;

        private Coroutine timeoutCoroutine;
        private Coroutine vulnerabilityCoroutine;

        protected abstract void OnTaskActivated();
        protected abstract void OnPlayerInteract(PlayerCharacter player);
        protected abstract void OnReadingSubmitted(float value);
        public abstract string GetTaskInstructions();

        [Server]
        public void ActivateTask()
        {
            if (taskState != EDiagnosticTaskState.Inactive) return;

            taskState = EDiagnosticTaskState.Available;
            OnTaskActivated();
            RpcTaskStateChanged((int)taskState);

            timeoutCoroutine = StartCoroutine(TimeoutCoroutine());
        }

        [Server]
        public void BeginVulnerabilityWindow()
        {
            if (taskState != EDiagnosticTaskState.InProgress) return;

            taskState = EDiagnosticTaskState.VulnerabilityWindow;
            RpcTaskStateChanged((int)taskState);

            vulnerabilityCoroutine = StartCoroutine(VulnerabilityCoroutine());
        }

        [Server]
        public void CompleteTask()
        {
            if (taskState == EDiagnosticTaskState.Completed || taskState == EDiagnosticTaskState.Failed) return;

            StopActiveCoroutines();
            taskState = EDiagnosticTaskState.Completed;
            RpcTaskStateChanged((int)taskState);
            OnTaskCompleted?.Invoke(this);
        }

        [Server]
        public void FailTask()
        {
            if (taskState == EDiagnosticTaskState.Completed || taskState == EDiagnosticTaskState.Failed) return;

            StopActiveCoroutines();
            taskState = EDiagnosticTaskState.Failed;
            RpcTaskStateChanged((int)taskState);
            OnTaskFailed?.Invoke(this);
        }

        public void PlayerInteract(PlayerCharacter player)
        {
            if (!isServer) return;
            if (taskState != EDiagnosticTaskState.Available && taskState != EDiagnosticTaskState.InProgress
                && taskState != EDiagnosticTaskState.VulnerabilityWindow) return;

            if (!string.IsNullOrEmpty(taskZoneTag) && player.GetCurrentZone() != taskZoneTag) return;

            if (taskState == EDiagnosticTaskState.Available)
                taskState = EDiagnosticTaskState.InProgress;

            OnPlayerInteract(player);
        }

        public void SubmitReading(float value)
        {
            if (!isServer) return;
            if (taskState != EDiagnosticTaskState.InProgress && taskState != EDiagnosticTaskState.VulnerabilityWindow)
                return;

            OnReadingSubmitted(value);
        }

        private IEnumerator TimeoutCoroutine()
        {
            yield return new WaitForSeconds(taskTimeLimit);

            if (taskState != EDiagnosticTaskState.Completed)
                FailTask();
        }

        private IEnumerator VulnerabilityCoroutine()
        {
            yield return new WaitForSeconds(vulnerabilityDuration);

            if (taskState == EDiagnosticTaskState.VulnerabilityWindow)
                taskState = EDiagnosticTaskState.InProgress;
        }

        private void StopActiveCoroutines()
        {
            if (timeoutCoroutine != null)
            {
                StopCoroutine(timeoutCoroutine);
                timeoutCoroutine = null;
            }
            if (vulnerabilityCoroutine != null)
            {
                StopCoroutine(vulnerabilityCoroutine);
                vulnerabilityCoroutine = null;
            }
        }

        [ClientRpc]
        private void RpcTaskStateChanged(int stateInt) { }

        private void OnTaskStateChanged(EDiagnosticTaskState oldState, EDiagnosticTaskState newState) { }
    }
}
