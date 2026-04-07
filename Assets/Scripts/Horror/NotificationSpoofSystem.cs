using UnityEngine;

namespace MimicFacility.Horror
{
    public class NotificationSpoofSystem : MonoBehaviour
    {
        [SerializeField] private AudioClip windowsNotification;
        [SerializeField] private AudioClip macNotification;
        [SerializeField] private AudioClip linuxNotification;

        [SerializeField] private float mimicDetectionRange = 10f;

        public bool IsAvailable => GetPlatformClip() != null;

        public void PlayNotificationSound()
        {
            AudioClip clip = GetPlatformClip();
            if (clip == null) return;

            var listener = FindObjectOfType<AudioListener>();
            Vector3 position = listener != null ? listener.transform.position : Vector3.zero;

            AudioSource.PlayClipAtPoint(clip, position, 1f);
        }

        public bool CheckTriggerCondition(Transform playerTransform, Camera playerCamera)
        {
            if (playerTransform == null) return false;

            var mimics = FindObjectsOfType<Core.MimicPlayerState>();
            foreach (var mimic in mimics)
            {
                if (!mimic.IsConverted || !mimic.IsAlive) continue;

                float distance = Vector3.Distance(playerTransform.position, mimic.transform.position);
                if (distance > mimicDetectionRange) continue;

                if (playerCamera != null)
                {
                    Vector3 viewportPos = playerCamera.WorldToViewportPoint(mimic.transform.position);
                    bool onScreen = viewportPos.x >= 0f && viewportPos.x <= 1f
                                 && viewportPos.y >= 0f && viewportPos.y <= 1f
                                 && viewportPos.z > 0f;

                    if (!onScreen)
                        return true;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private AudioClip GetPlatformClip()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return windowsNotification;
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return macNotification;
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return linuxNotification;
                default:
                    return windowsNotification;
            }
        }
    }
}
