using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace MimicFacility.Gameplay
{
    public class DiagnosticTaskManager : NetworkBehaviour
    {
        public event Action OnAllRequiredTasksComplete;

        [SerializeField] private List<DiagnosticTask> taskPrefabs = new List<DiagnosticTask>();
        [SerializeField] private int requiredCompletions = 3;
        [SerializeField] private int tasksToSpawn = 5;

        private readonly List<DiagnosticTask> activeTasks = new List<DiagnosticTask>();
        private List<DiagnosticTask> shuffledPrefabs;
        private int nextPrefabIndex;

        [SyncVar(hook = nameof(OnCompletedCountChanged))]
        private int completedTaskCount;
        public int CompletedTaskCount => completedTaskCount;
        public int RequiredCompletions => requiredCompletions;

        public bool AreAllRequiredTasksComplete => completedTaskCount >= requiredCompletions;

        public override void OnStartServer()
        {
            InitializeTasks();
        }

        [Server]
        public void InitializeTasks()
        {
            shuffledPrefabs = new List<DiagnosticTask>(taskPrefabs);
            ShuffleFisherYates(shuffledPrefabs);
            nextPrefabIndex = 0;
            completedTaskCount = 0;

            int spawnCount = Mathf.Min(tasksToSpawn, shuffledPrefabs.Count);
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnTask(shuffledPrefabs[nextPrefabIndex]);
                nextPrefabIndex++;
            }
        }

        [Server]
        private void SpawnTask(DiagnosticTask prefab)
        {
            var instance = Instantiate(prefab);
            NetworkServer.Spawn(instance.gameObject);

            instance.OnTaskCompleted += OnTaskCompleted;
            instance.OnTaskFailed += OnTaskFailed;
            activeTasks.Add(instance);

            instance.ActivateTask();
        }

        [Server]
        private void OnTaskCompleted(DiagnosticTask task)
        {
            completedTaskCount++;
            task.OnTaskCompleted -= OnTaskCompleted;
            task.OnTaskFailed -= OnTaskFailed;
            activeTasks.Remove(task);

            RpcTaskProgress(completedTaskCount, requiredCompletions);

            if (completedTaskCount >= requiredCompletions)
            {
                OnAllRequiredTasksComplete?.Invoke();
                return;
            }

            StartCoroutine(ActivateNextTaskDelayed(10f));
        }

        [Server]
        private void OnTaskFailed(DiagnosticTask task)
        {
            task.OnTaskCompleted -= OnTaskCompleted;
            task.OnTaskFailed -= OnTaskFailed;
            activeTasks.Remove(task);

            NetworkServer.Destroy(task.gameObject);
            StartCoroutine(ActivateNextTaskDelayed(5f));
        }

        private IEnumerator ActivateNextTaskDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            ActivateNextTask();
        }

        [Server]
        private void ActivateNextTask()
        {
            if (shuffledPrefabs == null || nextPrefabIndex >= shuffledPrefabs.Count)
            {
                if (shuffledPrefabs != null && shuffledPrefabs.Count > 0)
                {
                    nextPrefabIndex = 0;
                    ShuffleFisherYates(shuffledPrefabs);
                }
                else return;
            }

            SpawnTask(shuffledPrefabs[nextPrefabIndex]);
            nextPrefabIndex++;
        }

        public DiagnosticTask GetActiveTask()
        {
            foreach (var task in activeTasks)
            {
                if (task.TaskState == EDiagnosticTaskState.Available
                    || task.TaskState == EDiagnosticTaskState.InProgress)
                    return task;
            }
            return null;
        }

        public List<DiagnosticTask> GetAllActiveTasks()
        {
            return new List<DiagnosticTask>(activeTasks);
        }

        private static void ShuffleFisherYates<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        [ClientRpc]
        private void RpcTaskProgress(int completed, int required) { }

        private void OnCompletedCountChanged(int oldVal, int newVal) { }

        private void OnDestroy()
        {
            foreach (var task in activeTasks)
            {
                if (task != null)
                {
                    task.OnTaskCompleted -= OnTaskCompleted;
                    task.OnTaskFailed -= OnTaskFailed;
                }
            }
        }
    }
}
