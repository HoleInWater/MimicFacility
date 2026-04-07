using UnityEngine;

namespace MimicFacility.Core
{
    [PlayerComponent("Core", order: 10)]
    public class PlayerAutoSetup : MonoBehaviour
    {
        void Start()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                Debug.Log("[PlayerAutoSetup] Added Rigidbody (none was present).");
            }

            rb.linearDamping = 0.5f;
            rb.angularDamping = 5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.sleepThreshold = 0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.isKinematic = false;

            if (GetComponent<Collider>() == null)
            {
                var cap = gameObject.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 0.9f, 0f);
                cap.height = 1.8f;
                cap.radius = 0.3f;
                Debug.Log("[PlayerAutoSetup] Added CapsuleCollider (none was present).");
            }

            if (GetComponent<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();

            if (GetComponent<AudioSource>() == null)
            {
                var voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.spatialBlend = 1f;
                voiceSource.maxDistance = 20f;
                Debug.Log("[PlayerAutoSetup] Added AudioSource for voice.");
            }

            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null)
            {
                var camObj = new GameObject("PlayerCamera");
                camObj.transform.SetParent(transform);
                camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                cam = camObj.AddComponent<Camera>();
                cam.fieldOfView = 75f;
                cam.nearClipPlane = 0.1f;
                Debug.Log("[PlayerAutoSetup] Created camera child (none was present).");
            }

            var light = GetComponentInChildren<Light>();
            if (light == null)
            {
                var flashlightObj = new GameObject("Flashlight");
                flashlightObj.transform.SetParent(cam.transform);
                flashlightObj.transform.localPosition = Vector3.zero;
                flashlightObj.transform.localRotation = Quaternion.identity;
                var spot = flashlightObj.AddComponent<Light>();
                spot.type = LightType.Spot;
                spot.intensity = 3f;
                spot.range = 20f;
                spot.spotAngle = 35f;
                spot.enabled = false;
                Debug.Log("[PlayerAutoSetup] Created flashlight (none was present).");
            }
        }
    }

    public class PlayerSetup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;

            if (player.GetComponent<PlayerAutoSetup>() == null)
                player.AddComponent<PlayerAutoSetup>();

            if (player.GetComponent<PlayerMovement>() == null)
                player.AddComponent<PlayerMovement>();

            if (player.GetComponent<PlayerAnimationController>() == null)
                player.AddComponent<PlayerAnimationController>();

            if (player.GetComponent<PlayerStamina>() == null)
                player.AddComponent<PlayerStamina>();

            if (player.GetComponent<PlayerRagdoll>() == null)
                player.AddComponent<PlayerRagdoll>();

            Camera cam = player.GetComponentInChildren<Camera>();
            var animCtrl = player.GetComponent<PlayerAnimationController>();
            if (cam != null && animCtrl != null && animCtrl.playerCamera == null)
                animCtrl.playerCamera = cam;

            Debug.Log("[PlayerSetup] Auto-setup complete. All required components verified.");
        }
    }
}
