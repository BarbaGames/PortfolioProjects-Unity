using System;
using Agents.SecurityAgents;
using Unity.Mathematics;
using UnityEngine;

namespace Agents.States.SecurityStates
{
    public class PatrolState : State
    {
        private float _suspicionLevel = 0f;
        private float _suspicionDecayRate = 0.5f; // Per second
        private float _maxSuspicion = 3f;
        private Vector3 _lastNoisePosition;
        private bool _investigatingNoise = false;

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            Action move = parameters[0] as Action;
            Transform currentNode = parameters[1] as Transform;
            Transform[] patrolPoints = parameters[2] as Transform[];
            Transform target = parameters[3] as Transform;
            float detectionRange = parameters[4] as float? ?? 0;
            bool retreat = parameters[5] as bool? ?? false;
            bool hasLineOfSight = parameters.Length > 6 && (bool)parameters[6];
            bool heardNoise = parameters.Length > 7 && (bool)parameters[7];
            Vector3 noisePosition = parameters.Length > 8 ? (Vector3)parameters[8] : Vector3.zero;

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (!currentNode || patrolPoints == null || patrolPoints.Length == 0) return;

                // Handle noise investigation
                if (heardNoise && !_investigatingNoise)
                {
                    _investigatingNoise = true;
                    _lastNoisePosition = noisePosition;
                    _suspicionLevel = Mathf.Min(_maxSuspicion, _suspicionLevel + 1f);
                }

                // Decay suspicion over time
                _suspicionLevel = Mathf.Max(0f, _suspicionLevel - _suspicionDecayRate * Time.deltaTime);

                move?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                // Direct visual contact with target
                if (target && currentNode && hasLineOfSight)
                {
                    float distance = math.distance(currentNode.position, target.position);
                    if (distance <= detectionRange)
                    {
                        OnFlag?.Invoke(Flags.OnTargetFound);
                        return;
                    }
                }

                // High suspicion triggers search
                if (_suspicionLevel >= _maxSuspicion)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost); // Transition to search
                    return;
                }

                // Investigate noise if close enough
                if (_investigatingNoise && Vector3.Distance(currentNode.position, _lastNoisePosition) <= 3f)
                {
                    _investigatingNoise = false;
                    OnFlag?.Invoke(Flags.OnWait); // Brief pause to look around
                }
            });

            return behaviours;
        }

        public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                _suspicionLevel = 0f;
                _investigatingNoise = false;
            });

            return behaviours;
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            return default;
        }
    }
}