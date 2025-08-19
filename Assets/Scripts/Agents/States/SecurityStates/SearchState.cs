using System;
using Agents.SecurityAgents;
using Unity.Mathematics;
using UnityEngine;

namespace Agents.States.SecurityStates
{
    public class SearchState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            Action move = parameters[0] as Action;
            Transform currentNode = parameters[1] as Transform;
            Transform lastKnownPosition = parameters[2] as Transform;
            Transform target = parameters[3] as Transform;
            float searchRadius = parameters[4] as float? ?? 0;
            float detectionRange = parameters[5] as float? ?? 0;
            bool retreat = parameters[6] as bool? ?? false;

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (currentNode == null) return;

                move?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (target != null && currentNode != null)
                {
                    float distance = math.distance(currentNode.position, target.position);
                    if (distance <= detectionRange)
                    {
                        OnFlag?.Invoke(Flags.OnTargetFound);
                        return;
                    }
                }

                if (lastKnownPosition != null && currentNode != null)
                {
                    float searchDistance = math.distance(currentNode.position, lastKnownPosition.position);
                    if (searchDistance > searchRadius)
                    {
                        OnFlag?.Invoke(Flags.OnTargetLost);
                        return;
                    }
                }
            });

            return behaviours;
        }

        public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
        {
            return default;
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            return default;
        }
    }
}