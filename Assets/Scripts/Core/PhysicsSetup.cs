using UnityEngine;

namespace MimicFacility.Core
{
    public static class PhysicsSetup
    {
        public static Rigidbody EnsureRigidbody(GameObject go, float mass = 1f, float drag = 0.5f, float angularDrag = 5f, bool useGravity = true, bool freezeRotation = true)
        {
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();

            rb.mass = mass;
            rb.linearDamping = drag;
            rb.angularDamping = angularDrag;
            rb.useGravity = useGravity;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.isKinematic = false;
            rb.sleepThreshold = 0f;

            if (freezeRotation)
                rb.constraints = RigidbodyConstraints.FreezeRotation;

            return rb;
        }

        public static CapsuleCollider EnsureCapsuleCollider(GameObject go, float height = 1.8f, float radius = 0.3f, Vector3? center = null)
        {
            CapsuleCollider col = go.GetComponent<CapsuleCollider>();
            if (col == null)
                col = go.AddComponent<CapsuleCollider>();

            col.height = height;
            col.radius = radius;
            col.center = center ?? new Vector3(0f, height / 2f, 0f);

            return col;
        }

        public static BoxCollider EnsureBoxCollider(GameObject go, Vector3? size = null, Vector3? center = null, bool isTrigger = false)
        {
            BoxCollider col = go.GetComponent<BoxCollider>();
            if (col == null)
                col = go.AddComponent<BoxCollider>();

            col.size = size ?? Vector3.one;
            col.center = center ?? Vector3.zero;
            col.isTrigger = isTrigger;

            return col;
        }

        public static SphereCollider EnsureSphereCollider(GameObject go, float radius = 0.5f, Vector3? center = null, bool isTrigger = false)
        {
            SphereCollider col = go.GetComponent<SphereCollider>();
            if (col == null)
                col = go.AddComponent<SphereCollider>();

            col.radius = radius;
            col.center = center ?? Vector3.zero;
            col.isTrigger = isTrigger;

            return col;
        }

        public static AudioSource EnsureAudioSource(GameObject go, bool spatial = true, float maxDistance = 20f, bool loop = false)
        {
            AudioSource source = go.GetComponent<AudioSource>();
            if (source == null)
                source = go.AddComponent<AudioSource>();

            source.spatialBlend = spatial ? 1f : 0f;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f;
            source.loop = loop;
            source.playOnAwake = false;

            return source;
        }

        public static void SetLayer(GameObject go, string layerName, bool includeChildren = true)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0) return;

            go.layer = layer;
            if (includeChildren)
            {
                foreach (Transform child in go.GetComponentsInChildren<Transform>())
                    child.gameObject.layer = layer;
            }
        }

        public static void SetTag(GameObject go, string tag)
        {
            try { go.tag = tag; }
            catch { Debug.LogWarning($"[PhysicsSetup] Tag '{tag}' not defined. Add it in Tags & Layers."); }
        }
    }
}
