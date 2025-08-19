using System;
using Agents.TCAgent;
using Utils;

namespace Agents.States.TCStates
{
    // Gatherer state that gathers resources from the map
    public class GatherResourceState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = Convert.ToBoolean(parameters[0]);
            int food = Convert.ToInt32(parameters[1]);
            int gold = Convert.ToInt32(parameters[2]);
            int wood = Convert.ToInt32(parameters[3]);
            int resourceLimit = Convert.ToInt32(parameters[4]);
            ResourceType currentResource = (ResourceType)parameters[5];
            Action OnGather = parameters[6] as Action;
            SimNode<IVector> targetNode = parameters[7] as SimNode<IVector>;

            behaviours.AddMultiThreadableBehaviours(0, () => { OnGather?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (gold >= resourceLimit)
                {
                    OnFlag?.Invoke(Flags.OnFull);
                    return;
                }

                if (wood >= resourceLimit)
                {
                    OnFlag?.Invoke(Flags.OnFull);
                    return;
                }

                if (currentResource == ResourceType.Food && food >= resourceLimit)
                {
                    OnFlag?.Invoke(Flags.OnFull);
                    return;
                }

                if (food <= 0)
                {
                    OnFlag?.Invoke(Flags.OnHunger);
                    return;
                }

                if (targetNode.Resource <= 0 || (targetNode.NodeTerrain != NodeTerrain.Tree &&
                                                 targetNode.NodeTerrain != NodeTerrain.Mine &&
                                                 targetNode.NodeTerrain != NodeTerrain.Lake))
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
            BehaviourActions behaviours = new BehaviourActions();

            INode<IVector> adjacentNode = parameters[0] as INode<IVector>;
            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (adjacentNode != null) adjacentNode.IsOccupied = false;
            });

            return behaviours;
        }
    }
}