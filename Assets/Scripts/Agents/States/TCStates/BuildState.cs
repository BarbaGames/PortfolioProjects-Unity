using System;
using Agents.TCAgent;
using Utils;

namespace Agents.States.TCStates
{
    public class BuildState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = Convert.ToBoolean(parameters[0]);
            int food = Convert.ToInt32(parameters[1]);
            int gold = Convert.ToInt32(parameters[2]);
            int wood = Convert.ToInt32(parameters[3]);
            Action OnBuild = parameters[4] as Action;
            SimNode<IVector> targetNode = (SimNode<IVector>?)parameters[5];

            behaviours.AddMultiThreadableBehaviours(0, () => { OnBuild?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                const int goldCost = 2;
                const int woodCost = 4;
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (food <= 0 || gold < goldCost || wood < woodCost)
                {
                    OnFlag?.Invoke(Flags.OnHunger);
                    return;
                }

                if (targetNode.Resource >= 100 || targetNode.NodeTerrain != NodeTerrain.Construction)
                    OnFlag?.Invoke(Flags.OnTargetLost);
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