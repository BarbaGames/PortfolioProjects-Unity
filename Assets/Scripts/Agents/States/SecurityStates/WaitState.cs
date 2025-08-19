using System;
using Agents.SecurityAgents;

namespace Agents.States.SecurityStates
{
    public class WaitState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            float waitTime = parameters[0] as float? ?? 0;
            bool shouldMove = parameters[1] as bool? ?? false;
            bool retreat = parameters[2] as bool? ?? false;

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (shouldMove)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
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