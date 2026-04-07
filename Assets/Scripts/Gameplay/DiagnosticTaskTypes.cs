using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MimicFacility.Characters;

namespace MimicFacility.Gameplay
{
    public class PowerCalibrationTask : DiagnosticTask
    {
        [SerializeField] private Transform[] sliderPositions;

        private readonly float[] targetValues = new float[3];
        private readonly float[] currentValues = new float[3];
        private const float Tolerance = 0.1f;
        private int correctCount;
        private int currentStep;
        private const int RequiredSteps = 3;

        protected override void OnTaskActivated()
        {
            for (int i = 0; i < targetValues.Length; i++)
                targetValues[i] = Random.Range(0.2f, 0.8f);
            currentStep = 0;
            correctCount = 0;
        }

        protected override void OnPlayerInteract(PlayerCharacter player)
        {
            if (currentStep >= RequiredSteps) return;
        }

        protected override void OnReadingSubmitted(float value)
        {
            if (currentStep >= RequiredSteps) return;

            currentValues[currentStep] = value;

            bool withinTolerance = Mathf.Abs(value - targetValues[currentStep]) <= Tolerance;
            if (withinTolerance)
                correctCount++;

            currentStep++;

            if (correctCount >= 2 && TaskState == EDiagnosticTaskState.InProgress)
                BeginVulnerabilityWindow();

            if (currentStep >= RequiredSteps)
            {
                if (correctCount >= RequiredSteps)
                    CompleteTask();
                else
                    FailTask();
            }
        }

        public override string GetTaskInstructions()
        {
            return $"Calibrate power output: set voltage, amperage, and frequency to target values. " +
                   $"Step {currentStep + 1}/{RequiredSteps}. Tolerance: ±{Tolerance * 100}%.";
        }
    }

    public class AtmosphericSamplingTask : DiagnosticTask
    {
        [SerializeField] private Transform[] samplePositions;
        [SerializeField] private float collectionTime = 5f;
        [SerializeField] private float collectionRange = 2f;

        private int currentStep;
        private const int RequiredSteps = 3;
        private bool isCollecting;
        private Coroutine collectionCoroutine;

        protected override void OnTaskActivated()
        {
            currentStep = 0;
            isCollecting = false;
        }

        protected override void OnPlayerInteract(PlayerCharacter player)
        {
            if (currentStep >= RequiredSteps || isCollecting) return;

            if (samplePositions != null && currentStep < samplePositions.Length)
            {
                float dist = Vector3.Distance(player.transform.position, samplePositions[currentStep].position);
                if (dist > collectionRange) return;
            }

            BeginVulnerabilityWindow();
            collectionCoroutine = StartCoroutine(CollectSample(player));
        }

        private IEnumerator CollectSample(PlayerCharacter player)
        {
            isCollecting = true;
            yield return new WaitForSeconds(collectionTime);
            isCollecting = false;

            currentStep++;

            if (currentStep >= RequiredSteps)
                CompleteTask();
        }

        protected override void OnReadingSubmitted(float value) { }

        public override string GetTaskInstructions()
        {
            return $"Collect atmospheric samples from marked positions. " +
                   $"Sample {currentStep + 1}/{RequiredSteps}. Hold position for {collectionTime}s.";
        }
    }

    public class DataRecoveryTask : DiagnosticTask
    {
        [SerializeField] private Transform[] terminalPositions;

        private int currentStep;
        private const int RequiredSteps = 3;
        private readonly string[] codeFragments = new string[3];
        private string fullCode;

        protected override void OnTaskActivated()
        {
            currentStep = 0;
            for (int i = 0; i < codeFragments.Length; i++)
                codeFragments[i] = Random.Range(1000, 9999).ToString();
            fullCode = string.Join("-", codeFragments);
        }

        protected override void OnPlayerInteract(PlayerCharacter player)
        {
            if (currentStep >= RequiredSteps) return;

            if (terminalPositions != null && currentStep < terminalPositions.Length)
            {
                float dist = Vector3.Distance(player.transform.position, terminalPositions[currentStep].position);
                if (dist > 2f) return;
            }

            currentStep++;

            if (currentStep >= RequiredSteps)
                BeginVulnerabilityWindow();
        }

        protected override void OnReadingSubmitted(float value)
        {
            if (currentStep < RequiredSteps) return;

            int submittedCode = Mathf.RoundToInt(value);
            int expectedHash = fullCode.GetHashCode();

            if (submittedCode == expectedHash)
                CompleteTask();
            else
                FailTask();
        }

        public override string GetTaskInstructions()
        {
            string revealed = "";
            for (int i = 0; i < currentStep && i < codeFragments.Length; i++)
            {
                if (revealed.Length > 0) revealed += "-";
                revealed += codeFragments[i];
            }

            return $"Access terminals in sequence to recover data fragments. " +
                   $"Terminal {currentStep + 1}/{RequiredSteps}. " +
                   (revealed.Length > 0 ? $"Recovered: {revealed}" : "No fragments recovered yet.");
        }
    }

    public class BioScanTask : DiagnosticTask
    {
        [SerializeField] private Transform[] organismPositions;
        [SerializeField] private float scanTime = 3f;
        [SerializeField] private float scanRange = 2f;

        private int currentStep;
        private const int RequiredSteps = 4;
        private readonly float[] baselineReadings = new float[4];
        private readonly float[] scanReadings = new float[4];
        private bool isScanning;

        protected override void OnTaskActivated()
        {
            currentStep = 0;
            isScanning = false;
            for (int i = 0; i < baselineReadings.Length; i++)
                baselineReadings[i] = Random.Range(0.3f, 0.7f);
        }

        protected override void OnPlayerInteract(PlayerCharacter player)
        {
            if (currentStep >= RequiredSteps || isScanning) return;

            if (organismPositions != null && currentStep < organismPositions.Length)
            {
                float dist = Vector3.Distance(player.transform.position, organismPositions[currentStep].position);
                if (dist > scanRange) return;
            }

            BeginVulnerabilityWindow();
            StartCoroutine(ScanOrganism());
        }

        private IEnumerator ScanOrganism()
        {
            isScanning = true;
            yield return new WaitForSeconds(scanTime);
            isScanning = false;

            scanReadings[currentStep] = baselineReadings[currentStep] + Random.Range(-0.05f, 0.05f);
            currentStep++;

            if (currentStep >= RequiredSteps)
                CompleteTask();
        }

        protected override void OnReadingSubmitted(float value) { }

        public override string GetTaskInstructions()
        {
            return $"Scan biological organisms and compare to baseline readings. " +
                   $"Organism {currentStep + 1}/{RequiredSteps}. Hold scanner steady for {scanTime}s.";
        }
    }
}
