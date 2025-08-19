using System;
using Agents.SecurityAgents;
using Unity.Mathematics;
using UnityEngine;

namespace Agents.States.SecurityStates
{
    public class AttackState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            Action attack = parameters[0] as Action;
            Transform currentNode = parameters[1] as Transform;
            Transform target = parameters[2] as Transform;
            float attackRange = parameters[3] as float? ?? 0;
            bool retreat = parameters[4] as bool? ?? false;

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (!currentNode || !target) return;

                attack?.Invoke();
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

                if (distance > attackRange)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
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