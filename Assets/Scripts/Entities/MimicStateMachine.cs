using System;
using System.Collections.Generic;
using UnityEngine;
using MimicFacility.Characters;

namespace MimicFacility.Entities
{
    public interface IMimicState
    {
        EMimicState StateType { get; }
        void Enter(MimicAIController controller);
        void Execute(MimicAIController controller);
        void Exit(MimicAIController controller);
    }

    public class StateTransition
    {
        public EMimicState TargetState;
        public Func<bool> Condition;
    }

    public class MimicStateMachine
    {
        private IMimicState _currentState;
        private IMimicState _previousState;
        private readonly Dictionary<EMimicState, IMimicState> _states = new Dictionary<EMimicState, IMimicState>();
        private readonly Dictionary<EMimicState, List<StateTransition>> _transitions = new Dictionary<EMimicState, List<StateTransition>>();

        public IMimicState CurrentState => _currentState;
        public IMimicState PreviousState => _previousState;

        public void RegisterState(EMimicState type, IMimicState state)
        {
            _states[type] = state;
        }

        public IMimicState GetState(EMimicState type)
        {
            _states.TryGetValue(type, out var state);
            return state;
        }

        public void AddTransition(EMimicState from, EMimicState to, Func<bool> condition)
        {
            if (!_transitions.ContainsKey(from))
                _transitions[from] = new List<StateTransition>();

            _transitions[from].Add(new StateTransition
            {
                TargetState = to,
                Condition = condition
            });
        }

        public void ChangeState(IMimicState newState, MimicAIController controller)
        {
            if (newState == null) return;

            _currentState?.Exit(controller);
            _previousState = _currentState;
            _currentState = newState;
            _currentState.Enter(controller);

            controller.MimicBase?.SetState(_currentState.StateType);
        }

        public void RevertToPreviousState(MimicAIController controller)
        {
            if (_previousState != null)
                ChangeState(_previousState, controller);
        }

        public void Evaluate(MimicAIController controller)
        {
            if (_currentState == null) return;

            if (!_transitions.TryGetValue(_currentState.StateType, out var transitions))
                return;

            foreach (var transition in transitions)
            {
                if (transition.Condition())
                {
                    if (_states.TryGetValue(transition.TargetState, out var targetState))
                        ChangeState(targetState, controller);
                    return;
                }
            }
        }

        public void Execute(MimicAIController controller)
        {
            _currentState?.Execute(controller);
        }
    }

    public class PatrolState : IMimicState
    {
        public EMimicState StateType => EMimicState.Patrol;

        private Vector3 _destination;
        private float _waitTimer;
        private bool _waiting;

        public void Enter(MimicAIController controller)
        {
            _waiting = false;
            _waitTimer = 0f;
            _destination = controller.GetPatrolPoint();

            if (controller.Agent != null && controller.Agent.enabled)
            {
                controller.Agent.speed = controller.MimicBase.MoveSpeed;
                controller.Agent.SetDestination(_destination);
            }
        }

        public void Execute(MimicAIController controller)
        {
            if (controller.Agent == null || !controller.Agent.enabled) return;

            if (_waiting)
            {
                _waitTimer -= 0.5f;
                if (_waitTimer <= 0f)
                {
                    _waiting = false;
                    _destination = controller.GetPatrolPoint();
                    controller.Agent.SetDestination(_destination);
                }
                return;
            }

            if (!controller.Agent.pathPending && controller.Agent.remainingDistance < 0.5f)
            {
                _waiting = true;
                _waitTimer = 3f;
            }
        }

        public void Exit(MimicAIController controller) { }
    }

    public class StalkState : IMimicState
    {
        public EMimicState StateType => EMimicState.Stalking;

        private float _repositionTimer;

        public void Enter(MimicAIController controller)
        {
            _repositionTimer = 0f;

            var target = controller.MimicBase.GetNearestPlayer();
            if (target != null)
                controller.MimicBase.SetTarget(target.netId);
        }

        public void Execute(MimicAIController controller)
        {
            if (controller.Agent == null || !controller.Agent.enabled) return;

            _repositionTimer -= 0.5f;
            if (_repositionTimer > 0f) return;

            _repositionTimer = 2f;

            var target = controller.MimicBase.GetTargetPlayer();
            if (target == null) return;

            Vector3 stalkPos = controller.GetStalkPosition(target);
            controller.Agent.speed = controller.MimicBase.MoveSpeed * 0.8f;
            controller.Agent.SetDestination(stalkPos);
        }

        public void Exit(MimicAIController controller) { }
    }

    public class ImpersonateState : IMimicState
    {
        public EMimicState StateType => EMimicState.Impersonating;

        public void Enter(MimicAIController controller)
        {
            if (controller.Agent != null && controller.Agent.enabled)
                controller.Agent.isStopped = true;
        }

        public void Execute(MimicAIController controller)
        {
            var target = controller.MimicBase.GetTargetPlayer();
            if (target == null) return;

            Vector3 lookDir = target.transform.position - controller.transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
                controller.transform.rotation = Quaternion.Slerp(
                    controller.transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    0.1f);

            var voiceAudio = controller.MimicBase.GetComponent<AudioSource>();
            if (voiceAudio != null && !voiceAudio.isPlaying)
            {
                var profile = controller.MimicBase.GetVoiceProfile();
                if (profile != null && profile.HasData)
                {
                    // Voice playback handled by VoiceCloneClient integration
                }
            }
        }

        public void Exit(MimicAIController controller)
        {
            if (controller.Agent != null && controller.Agent.enabled)
                controller.Agent.isStopped = false;
        }
    }

    public class AttackState : IMimicState
    {
        public EMimicState StateType => EMimicState.Attacking;

        public void Enter(MimicAIController controller)
        {
            if (controller.Agent != null && controller.Agent.enabled)
                controller.Agent.speed = controller.MimicBase.MoveSpeed * 1.5f;
        }

        public void Execute(MimicAIController controller)
        {
            var target = controller.MimicBase.GetTargetPlayer();
            if (target == null) return;

            if (controller.Agent != null && controller.Agent.enabled)
                controller.Agent.SetDestination(target.transform.position);

            float dist = Vector3.Distance(controller.transform.position, target.transform.position);
            if (dist <= controller.MimicBase.AttackRange)
            {
                var state = target.GetComponent<MimicPlayerState>();
                state?.TakeDamage(25f);
            }
        }

        public void Exit(MimicAIController controller)
        {
            if (controller.Agent != null && controller.Agent.enabled)
                controller.Agent.speed = controller.MimicBase.MoveSpeed;
        }
    }

    public class FleeState : IMimicState
    {
        public EMimicState StateType => EMimicState.Fleeing;

        public void Enter(MimicAIController controller)
        {
            Vector3 fleePoint = controller.GetFleePoint();
            if (controller.Agent != null && controller.Agent.enabled)
            {
                controller.Agent.speed = controller.MimicBase.MoveSpeed * 1.3f;
                controller.Agent.SetDestination(fleePoint);
            }
        }

        public void Execute(MimicAIController controller)
        {
            if (controller.Agent == null || !controller.Agent.enabled) return;

            if (!controller.Agent.pathPending && controller.Agent.remainingDistance < 1f)
            {
                Vector3 fleePoint = controller.GetFleePoint();
                controller.Agent.SetDestination(fleePoint);
            }
        }

        public void Exit(MimicAIController controller)
        {
            if (controller.Agent != null && controller.Agent.enabled)
                controller.Agent.speed = controller.MimicBase.MoveSpeed;
        }
    }

    public class ReproduceState : IMimicState
    {
        public EMimicState StateType => EMimicState.Reproducing;

        private float _reproduceTimer;
        private bool _spawned;

        public void Enter(MimicAIController controller)
        {
            _reproduceTimer = 3f;
            _spawned = false;

            if (controller.Agent != null && controller.Agent.enabled)
                controller.Agent.isStopped = true;

            var animator = controller.GetComponent<Animator>();
            if (animator != null)
                animator.SetTrigger("Reproduce");
        }

        public void Execute(MimicAIController controller)
        {
            _reproduceTimer -= 0.5f;

            if (_reproduceTimer <= 0f && !_spawned)
            {
                _spawned = true;
                controller.SpawnMimic();
            }
        }

        public void Exit(MimicAIController controller)
        {
            if (controller.Agent != null && controller.Agent.enabled)
                controller.Agent.isStopped = false;
        }
    }
}
