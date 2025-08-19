using System;
using Agents.SecurityAgents;
using Unity.Mathematics;
using UnityEngine;

namespace Agents.States.SecurityStates
{
    public class RetreatState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            Action move = parameters[0] as Action;
            Transform currentNode = parameters[1] as Transform;
            Transform retreatPoint = parameters[2] as Transform;
            float safeDistance = parameters[3] as float? ?? 0;
            bool stopRetreating = parameters[4] as bool? ?? false;

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (!currentNode || !retreatPoint) return;

                move?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (!currentNode || !retreatPoint)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                if (stopRetreating)
                {
                    OnFlag?.Invoke(Flags.OnWait);
                    return;
                }

                float distance = math.distance(currentNode.position, retreatPoint.position);

                if (distance <= safeDistance)
                {
                    OnFlag?.Invoke(Flags.OnTargetReach);
                    return;
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