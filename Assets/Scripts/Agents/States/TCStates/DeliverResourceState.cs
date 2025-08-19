using System;
using Agents.TCAgent;
using Utils;

namespace Agents.States.TCStates
{
    public class DeliverResourceState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            int gold = Convert.ToInt32(parameters[0]);
            int food = Convert.ToInt32(parameters[1]);
            int wood = Convert.ToInt32(parameters[2]);
            ResourceType currentResource = (ResourceType)parameters[3];
            Action onDeliverResource = parameters[4] as Action;
            bool retreat = Convert.ToBoolean(parameters[5]);
            SimNode<IVector> currentNode = parameters[6] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[7] as SimNode<IVector>;


            behaviours.AddMultiThreadableBehaviours(0, () => { onDeliverResource?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                switch (currentResource)
                {
                    case ResourceType.Gold:
                        if (gold <= 0)
                        {
                            OnFlag?.Invoke(Flags.OnHunger);
                            return;
                        }

                        break;
                    case ResourceType.Wood:
                        if (wood <= 0)
                        {
                            OnFlag?.Invoke(Flags.OnHunger);
                            return;
                        }

                        break;
                    case ResourceType.Food:
                        if (food <= 0)
                        {
                            OnFlag?.Invoke(Flags.OnHunger);
                            return;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (!currentNode.GetCoordinate().Adyacent(targetNode.GetCoordinate()))
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