using System;
using Agents.SecurityAgents;
using Unity.Mathematics;
using UnityEngine;

namespace Agents.States.SecurityStates
{
    public class ChaseState : State
    {
        private float _lastVisualContactTime;
        private float _visualLossGracePeriod = 2f;

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            Action move = parameters[0] as Action;
            Transform currentNode = parameters[1] as Transform;
            Transform target = parameters[2] as Transform;
            float maxChaseDistance = parameters[3] as float? ?? 0;
            float reachDistance = parameters[4] as float? ?? 0;
            bool retreat = parameters[5] as bool? ?? false;
            // Add line of sight check parameter
            bool hasLineOfSight = parameters.Length <= 6 || (bool)parameters[6];

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (!currentNode || !target) return;
                move?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (!currentNode || !target)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                float distance = math.distance(currentNode.position, target.position);

                if (hasLineOfSight)
                {
                    _lastVisualContactTime = Time.time;
                }

                // Check if we've lost visual contact for too long
                if (!hasLineOfSight && Time.time - _lastVisualContactTime > _visualLossGracePeriod)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                // Distance checks
                if (distance > maxChaseDistance)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                if (distance <= reachDistance)
                {
                    OnFlag?.Invoke(Flags.OnTargetReach);
                    return;
                }
            });

            return behaviours;
        }

        public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                _lastVisualContactTime = Time.time;
            });

            return behaviours;
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            return default;
        }
    }
}