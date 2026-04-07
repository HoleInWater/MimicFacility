using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MimicFacility.Core;
using MimicFacility.Gear;

namespace MimicFacility.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Game State")]
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI mimicCountText;
        [SerializeField] private TextMeshProUGUI containedCountText;
        [SerializeField] private TextMeshProUGUI miscontainmentText;
        [SerializeField] private TextMeshProUGUI taskProgressText;

        [Header("Verification")]
        [SerializeField] private TextMeshProUGUI verificationTargetText;
        [SerializeField] private Image verificationProximityIndicator;

        [Header("Messages")]
        [SerializeField] private TextMeshProUGUI directorMessageText;
        [SerializeField] private TextMeshProUGUI interactionPromptText;

        [Header("Bars")]
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Image healthBar;
        [SerializeField] private Image sporeExposureBar;

        [Header("Panels")]
        [SerializeField] private GameObject taskPanel;
        [SerializeField] private GameObject containmentResultPanel;
        [SerializeField] private TextMeshProUGUI containmentResultText;

        [Header("Notifications")]
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private CanvasGroup notificationGroup;

        [Header("Scan Overlay")]
        [SerializeField] private GameObject scanOverlayPanel;
        [SerializeField] private TextMeshProUGUI scanResultText;
        [SerializeField] private Image scanWaveformBar;

        [Header("Subliminal")]
        [SerializeField] private TextMeshProUGUI subliminalText;
        [SerializeField] private TextMeshProUGUI pauseInjectionText;

        [Header("Settings")]
        [SerializeField] private float directorMessageFadeDuration = 2f;
        [SerializeField] private float crosshairPulseSpeed = 4f;
        [SerializeField] private float crosshairPulseAmount = 0.1f;

        private CanvasGroup directorMessageGroup;
        private readonly Queue<(string message, float duration)> messageQueue = new Queue<(string, float)>();
        private Coroutine directorMessageCoroutine;
        private Coroutine notificationCoroutine;
        private bool isShowingDirectorMessage;
        private bool isAimingAtInteractable;
        private float crosshairBaseScale = 1f;

        private void Awake()
        {
            if (directorMessageText != null)
            {
                directorMessageGroup = directorMessageText.GetComponent<CanvasGroup>();
                if (directorMessageGroup == null)
                    directorMessageGroup = directorMessageText.gameObject.AddComponent<CanvasGroup>();
                directorMessageGroup.alpha = 0f;
            }

            if (notificationGroup == null && notificationText != null)
            {
                notificationGroup = notificationText.GetComponent<CanvasGroup>();
                if (notificationGroup == null)
                    notificationGroup = notificationText.gameObject.AddComponent<CanvasGroup>();
            }

            if (notificationGroup != null) notificationGroup.alpha = 0f;
            if (subliminalText != null) subliminalText.gameObject.SetActive(false);
            if (scanOverlayPanel != null) scanOverlayPanel.SetActive(false);

            HideInteractionPrompt();
        }

        private void Update()
        {
            SubliminalFrameCheck();
            AnimateCrosshair();
        }

        public void UpdateGameState(NetworkedGameState state)
        {
            if (state == null) return;

            if (roundText != null)
                roundText.text = $"Round {state.CurrentRound}";
            if (mimicCountText != null)
                mimicCountText.text = $"Active Mimics: {state.ActiveMimicCount}";
            if (containedCountText != null)
                containedCountText.text = $"Contained: {state.ContainedMimicCount}";
            if (miscontainmentText != null)
                miscontainmentText.text = $"Miscontainments: {state.MiscontainmentCount}";
            if (taskProgressText != null)
                taskProgressText.text = $"Tasks: {state.DiagnosticTasksCompleted}/{state.RequiredTasksForExtraction}";
        }

        public void ShowDirectorMessage(string message, float duration = 8f)
        {
            if (isShowingDirectorMessage)
            {
                messageQueue.Enqueue((message, duration));
                return;
            }

            if (directorMessageCoroutine != null)
                StopCoroutine(directorMessageCoroutine);

            directorMessageCoroutine = StartCoroutine(DirectorMessageCoroutine(message, duration));
        }

        private IEnumerator DirectorMessageCoroutine(string message, float duration)
        {
            isShowingDirectorMessage = true;
            directorMessageText.text = message;
            directorMessageGroup.alpha = 1f;

            float holdTime = duration - directorMessageFadeDuration;
            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);

            float elapsed = 0f;
            while (elapsed < directorMessageFadeDuration)
            {
                elapsed += Time.deltaTime;
                directorMessageGroup.alpha = 1f - (elapsed / directorMessageFadeDuration);
                yield return null;
            }

            directorMessageGroup.alpha = 0f;
            isShowingDirectorMessage = false;

            if (messageQueue.Count > 0)
            {
                var next = messageQueue.Dequeue();
                directorMessageCoroutine = StartCoroutine(DirectorMessageCoroutine(next.message, next.duration));
            }
        }

        public void ShowInteractionPrompt(string text)
        {
            if (interactionPromptText == null) return;
            interactionPromptText.text = text;
            interactionPromptText.gameObject.SetActive(true);
            isAimingAtInteractable = true;
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptText == null) return;
            interactionPromptText.gameObject.SetActive(false);
            isAimingAtInteractable = false;
        }

        public void UpdateHealth(float normalized)
        {
            if (healthBar != null)
                healthBar.fillAmount = Mathf.Clamp01(normalized);
        }

        public void UpdateSporeExposure(float normalized)
        {
            if (sporeExposureBar != null)
                sporeExposureBar.fillAmount = Mathf.Clamp01(normalized);
        }

        public void ShowScanResult(ScanResult result)
        {
            if (scanOverlayPanel == null) return;

            scanOverlayPanel.SetActive(true);

            if (scanResultText != null)
            {
                string integrity = (result.waveformIntegrity * 100f).ToString("F1");
                string status = result.waveformIntegrity >= 0.9f ? "NOMINAL" : "ANOMALOUS";
                scanResultText.text = $"Waveform Integrity: {integrity}%\nStatus: {status}";
                scanResultText.color = result.waveformIntegrity >= 0.9f ? Color.green : Color.red;
            }

            if (scanWaveformBar != null)
                scanWaveformBar.fillAmount = result.waveformIntegrity;

            StartCoroutine(HideScanAfterDelay(4f));
        }

        private IEnumerator HideScanAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (scanOverlayPanel != null)
                scanOverlayPanel.SetActive(false);
        }

        public void SubliminalFrameCheck()
        {
            if (subliminalText == null) return;
            // Integration point: SubliminalFrameSystem.ConsumeFrame provides one-frame text
            // subliminalText is activated for exactly one frame then deactivated next frame
            if (subliminalText.gameObject.activeSelf)
                subliminalText.gameObject.SetActive(false);
        }

        public void ShowSubliminalFrame(string text)
        {
            if (subliminalText == null) return;
            subliminalText.text = text;
            subliminalText.gameObject.SetActive(true);
        }

        public void PauseMenuInjectionCheck()
        {
            if (pauseInjectionText == null) return;
            // Integration point: PauseMenuInjection overlays injected phrases when paused
        }

        public void ShowPauseInjection(string text)
        {
            if (pauseInjectionText == null) return;
            pauseInjectionText.text = text;
            pauseInjectionText.gameObject.SetActive(true);
        }

        public void HidePauseInjection()
        {
            if (pauseInjectionText != null)
                pauseInjectionText.gameObject.SetActive(false);
        }

        public void ShowNotification(string text, float duration = 3f)
        {
            if (notificationText == null || notificationGroup == null) return;

            if (notificationCoroutine != null)
                StopCoroutine(notificationCoroutine);

            notificationCoroutine = StartCoroutine(NotificationCoroutine(text, duration));
        }

        private IEnumerator NotificationCoroutine(string text, float duration)
        {
            notificationText.text = text;
            notificationGroup.alpha = 1f;

            yield return new WaitForSeconds(duration - 1f);

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                notificationGroup.alpha = 1f - elapsed;
                yield return null;
            }
            notificationGroup.alpha = 0f;
        }

        private void AnimateCrosshair()
        {
            if (crosshairImage == null) return;

            if (isAimingAtInteractable)
            {
                float pulse = 1f + Mathf.Sin(Time.time * crosshairPulseSpeed) * crosshairPulseAmount;
                crosshairImage.transform.localScale = Vector3.one * (crosshairBaseScale * pulse);
            }
            else
            {
                crosshairImage.transform.localScale = Vector3.one * crosshairBaseScale;
            }
        }

        public void SetVerificationTarget(string targetName)
        {
            if (verificationTargetText == null) return;
            verificationTargetText.text = $"VERIFY: {targetName}";
            verificationTargetText.gameObject.SetActive(true);
        }

        public void UpdateVerificationProximity(bool isNearTarget)
        {
            if (verificationProximityIndicator == null) return;
            verificationProximityIndicator.color = isNearTarget
                ? new Color(0.2f, 0.9f, 0.2f, 0.6f)
                : new Color(0.9f, 0.9f, 0.9f, 0.2f);
        }

        public void ShowVerificationResult(bool success)
        {
            if (containmentResultPanel == null) return;
            containmentResultPanel.SetActive(true);

            if (containmentResultText != null)
            {
                containmentResultText.text = success
                    ? "VERIFICATION SUCCESS\nYou caught the replacement."
                    : "VERIFICATION FAILED\nYour target was replaced and you missed it.\nScanner reliability reduced.";
                containmentResultText.color = success ? Color.green : Color.red;
            }

            StartCoroutine(HideVerificationResult());
        }

        private IEnumerator HideVerificationResult()
        {
            yield return new WaitForSeconds(5f);
            if (containmentResultPanel != null)
                containmentResultPanel.SetActive(false);
        }
    }
}
