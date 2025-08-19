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

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;
            List<SimNode<IVector>> path = parameters[4] as List<SimNode<IVector>>;


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