using System;
using System.Collections.Generic;
using Utils;

namespace Agents.States.SecurityStates
{
    public class WalkState : State
    {

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            Action onMove = parameters[0] as Action;

            behaviours.AddMultiThreadableBehaviours(0, onMove);

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