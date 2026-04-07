using System;
using System.Collections.Generic;
using UnityEngine;

namespace MimicFacility.Gameplay
{
    public enum ETutorialStep
    {
        Movement,
        Interaction,
        GearPickup,
        GearUse,
        FlashlightToggle,
        Communication,
        AccusationBasics,
        TaskBasics,
        Complete
    }

    [Serializable]
    public class TutorialStep
    {
        public ETutorialStep step;
        public string instruction;
        public Func<bool> completionCondition;
        public bool hasBeenShown;

        public TutorialStep(ETutorialStep step, string instruction, Func<bool> completionCondition)
        {
            this.step = step;
            this.instruction = instruction;
            this.completionCondition = completionCondition;
            hasBeenShown = false;
        }
    }

    public class TutorialSystem : MonoBehaviour
    {
        public event Action<ETutorialStep, string> OnStepChanged;
        public event Action<string, float> OnHintShown;
        public event Action OnTutorialComplete;

        private readonly List<TutorialStep> steps = new List<TutorialStep>();
        private int currentStepIndex;

        private bool tutorialActive;
        private float hintTimer;
        private string currentHint;

        private const string TutorialCompleteKey = "tutorial_complete";

        public ETutorialStep CurrentStep => currentStepIndex < steps.Count
            ? steps[currentStepIndex].step
            : ETutorialStep.Complete;

        public bool IsFirstPlaythrough => PlayerPrefs.GetInt(TutorialCompleteKey, 0) == 0;

        private void Awake()
        {
            InitializeSteps();
        }

        private void Start()
        {
            if (IsFirstPlaythrough)
                BeginTutorial();
        }

        private void Update()
        {
            if (!tutorialActive) return;

            CheckCompletion();

            if (hintTimer > 0f)
            {
                hintTimer -= Time.deltaTime;
                if (hintTimer <= 0f)
                    currentHint = null;
            }
        }

        private void InitializeSteps()
        {
            steps.Clear();

            steps.Add(new TutorialStep(ETutorialStep.Movement,
                "Use WASD to move. Hold SHIFT to sprint. Press SPACE to jump.",
                () => Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f));

            steps.Add(new TutorialStep(ETutorialStep.Interaction,
                "Look at objects and press E to interact.",
                () => Input.GetKeyDown(KeyCode.E)));

            steps.Add(new TutorialStep(ETutorialStep.GearPickup,
                "Approach gear on the ground and press E to pick it up.",
                null));

            steps.Add(new TutorialStep(ETutorialStep.GearUse,
                "Press the left mouse button to use your equipped gear.",
                () => Input.GetMouseButtonDown(0)));

            steps.Add(new TutorialStep(ETutorialStep.FlashlightToggle,
                "Press F to toggle your flashlight on and off.",
                () => Input.GetKeyDown(KeyCode.F)));

            steps.Add(new TutorialStep(ETutorialStep.Communication,
                "Hold V to use push-to-talk. Communicate with your team.",
                () => Input.GetKeyDown(KeyCode.V)));

            steps.Add(new TutorialStep(ETutorialStep.AccusationBasics,
                "Suspect a mimic? Open the accusation panel with TAB to start a vote.",
                null));

            steps.Add(new TutorialStep(ETutorialStep.TaskBasics,
                "Complete diagnostic tasks at terminals to enable extraction.",
                null));
        }

        public void BeginTutorial()
        {
            tutorialActive = true;
            currentStepIndex = 0;
            NotifyStepChanged();
        }

        private void CheckCompletion()
        {
            if (currentStepIndex >= steps.Count) return;

            var step = steps[currentStepIndex];
            if (step.completionCondition != null && step.completionCondition.Invoke())
                AdvanceToNextStep();
        }

        public void AdvanceToNextStep()
        {
            if (currentStepIndex >= steps.Count) return;

            steps[currentStepIndex].hasBeenShown = true;
            currentStepIndex++;

            if (currentStepIndex >= steps.Count)
            {
                CompleteTutorial();
                return;
            }

            NotifyStepChanged();
        }

        public void SkipTutorial()
        {
            CompleteTutorial();
        }

        public void ResetTutorial()
        {
            PlayerPrefs.SetInt(TutorialCompleteKey, 0);
            PlayerPrefs.Save();

            foreach (var step in steps)
                step.hasBeenShown = false;

            currentStepIndex = 0;
            tutorialActive = false;
        }

        private void CompleteTutorial()
        {
            tutorialActive = false;
            PlayerPrefs.SetInt(TutorialCompleteKey, 1);
            PlayerPrefs.Save();
            OnTutorialComplete?.Invoke();
        }

        private void NotifyStepChanged()
        {
            if (currentStepIndex >= steps.Count) return;
            var step = steps[currentStepIndex];
            OnStepChanged?.Invoke(step.step, step.instruction);
        }

        public void ShowHint(string text, float duration)
        {
            currentHint = text;
            hintTimer = duration;
            OnHintShown?.Invoke(text, duration);
        }

        public string GetCurrentHint() => currentHint;
    }
}
