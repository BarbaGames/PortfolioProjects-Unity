using System;
using Agents.SecurityAgents;
using Unity.Mathematics;
using UnityEngine;

namespace Agents.States.SecurityStates
{
    public class PatrolState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            Action move = parameters[0] as Action;
            Transform currentNode = parameters[1] as Transform;
            Transform[] patrolPoints = parameters[2] as Transform[];
            Transform target = parameters[3] as Transform;
            float detectionRange = parameters[4] as float? ?? 0;
            bool retreat = parameters[5] as bool? ?? false;

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (!currentNode || patrolPoints == null || patrolPoints.Length == 0) return;

                move?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (target && currentNode)
                {
                    float distance = math.distance(currentNode.position, target.position);
                    if (distance <= detectionRange)
                    {
                        OnFlag?.Invoke(Flags.OnTargetFound);
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