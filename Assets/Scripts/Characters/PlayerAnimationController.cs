using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using MimicFacility.Core;

namespace MimicFacility.Characters
{
    [PlayerComponent("Animation", order: 5)]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;

        [Header("Animation Clips")]
        [Tooltip("Idle breathing animation")]
        public AnimationClip idleClip;
        [Tooltip("Walking animation")]
        public AnimationClip walkClip;
        [Tooltip("Running animation")]
        public AnimationClip runClip;
        [Tooltip("Jump animation")]
        public AnimationClip jumpClip;
        [Tooltip("Death/collapse animation")]
        public AnimationClip deathClip;
        [Tooltip("Flashlight toggle animation (optional)")]
        public AnimationClip flashlightClip;

        [Header("Blend Settings")]
        public float locomotionBlendSpeed = 8f;
        public float actionFadeIn = 0.1f;
        public float actionFadeOut = 0.2f;

        [Header("Upper Body Mask")]
        [Tooltip("Mask for upper body actions (flashlight, gear use) while legs keep moving")]
        public AvatarMask upperBodyMask;

        private PlayableGraph graph;
        private AnimationLayerMixerPlayable layerMixer;
        private AnimatorControllerPlayable locoPlayable;
        private AnimationMixerPlayable locomotionMixer;
        private AnimationMixerPlayable actionMixer;

        private AnimationClipPlayable idleP, walkP, runP, jumpP, deathP, flashlightP;
        private bool hasIdle, hasWalk, hasRun, hasJump, hasDeath, hasFlashlight;

        private const int IDLE = 0, WALK = 1, RUN = 2, JUMP = 3;
        private const int A_DEATH = 0, A_FLASHLIGHT = 1;

        private float[] locoWeights = new float[4];
        private float[] locoTargets = new float[4];
        private float actionWeight;
        private float targetActionWeight;

        private Animator animator;
        private PlayerMovement movement;

        void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = gameObject.AddComponent<Animator>();
                Debug.Log("[PlayerAnimationController] Added Animator component.");
            }

            animator.applyRootMotion = false;
            movement = GetComponent<PlayerMovement>();

            graph = PlayableGraph.Create(gameObject.name + "_PlayerAnim");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            layerMixer = AnimationLayerMixerPlayable.Create(graph, 2);

            locomotionMixer = AnimationMixerPlayable.Create(graph, 4);
            graph.Connect(locomotionMixer, 0, layerMixer, 0);
            layerMixer.SetInputWeight(0, 1f);

            hasIdle = TryCreateClip(idleClip, locomotionMixer, IDLE, out idleP);
            hasWalk = TryCreateClip(walkClip, locomotionMixer, WALK, out walkP);
            hasRun = TryCreateClip(runClip, locomotionMixer, RUN, out runP);
            hasJump = TryCreateClip(jumpClip, locomotionMixer, JUMP, out jumpP);

            if (hasIdle)
            {
                locoWeights[IDLE] = 1f;
                locoTargets[IDLE] = 1f;
                locomotionMixer.SetInputWeight(IDLE, 1f);
            }

            actionMixer = AnimationMixerPlayable.Create(graph, 2);
            graph.Connect(actionMixer, 0, layerMixer, 1);
            layerMixer.SetInputWeight(1, 0f);

            if (upperBodyMask != null)
            {
                layerMixer.SetLayerMaskFromAvatarMask(1, upperBodyMask);
                layerMixer.SetLayerAdditive(1, false);
            }

            hasDeath = TryCreateClip(deathClip, actionMixer, A_DEATH, out deathP);
            hasFlashlight = TryCreateClip(flashlightClip, actionMixer, A_FLASHLIGHT, out flashlightP);

            var output = AnimationPlayableOutput.Create(graph, "PlayerAnim", animator);
            output.SetSourcePlayable(layerMixer);

            graph.Play();
        }

        void Update()
        {
            UpdateLocomotionTargets();
            BlendLocomotion();
            BlendActionLayer();
        }

        void UpdateLocomotionTargets()
        {
            if (movement == null) return;

            bool grounded = movement.IsGrounded();
            bool sprinting = movement.IsSprinting();
            float speed = movement.GetCurrentSpeed();
            bool moving = speed > 0.5f;

            locoTargets[IDLE] = 0f;
            locoTargets[WALK] = 0f;
            locoTargets[RUN] = 0f;
            locoTargets[JUMP] = 0f;

            if (!grounded && hasJump)
            {
                locoTargets[JUMP] = 1f;
            }
            else if (moving && sprinting && hasRun)
            {
                locoTargets[RUN] = 1f;
            }
            else if (moving && hasWalk)
            {
                locoTargets[WALK] = 1f;
            }
            else if (hasIdle)
            {
                locoTargets[IDLE] = 1f;
            }
        }

        void BlendLocomotion()
        {
            float dt = Time.deltaTime * locomotionBlendSpeed;
            for (int i = 0; i < 4; i++)
            {
                locoWeights[i] = Mathf.MoveTowards(locoWeights[i], locoTargets[i], dt);
                locomotionMixer.SetInputWeight(i, locoWeights[i]);
            }
        }

        void BlendActionLayer()
        {
            float speed = targetActionWeight > actionWeight
                ? Time.deltaTime / Mathf.Max(actionFadeIn, 0.01f)
                : Time.deltaTime / Mathf.Max(actionFadeOut, 0.01f);

            actionWeight = Mathf.MoveTowards(actionWeight, targetActionWeight, speed);
            layerMixer.SetInputWeight(1, actionWeight);
        }

        public void PlayDeath()
        {
            if (!hasDeath) return;
            deathP.SetTime(0);
            ActivateAction(A_DEATH);
        }

        public void PlayFlashlightToggle()
        {
            if (!hasFlashlight) return;
            flashlightP.SetTime(0);
            ActivateAction(A_FLASHLIGHT);
        }

        public void ReturnToLocomotion()
        {
            targetActionWeight = 0f;
        }

        private void ActivateAction(int slot)
        {
            targetActionWeight = 1f;
            for (int i = 0; i < 2; i++)
                actionMixer.SetInputWeight(i, i == slot ? 1f : 0f);
        }

        private bool TryCreateClip(AnimationClip clip, AnimationMixerPlayable mixer, int slot, out AnimationClipPlayable playable)
        {
            if (clip == null) { playable = default; return false; }
            playable = AnimationClipPlayable.Create(graph, clip);
            graph.Connect(playable, 0, mixer, slot);
            mixer.SetInputWeight(slot, 0f);
            return true;
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
