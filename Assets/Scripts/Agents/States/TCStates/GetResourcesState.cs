using System;
using Agents.TCAgent;

namespace Agents.States.TCStates
{
    // Cart state that gets resources from the town center 
    public class GetResourcesState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            int gold = Convert.ToInt32(parameters[0]);
            int food = Convert.ToInt32(parameters[1]);
            int wood = Convert.ToInt32(parameters[2]);
            ResourceType currentResource = (ResourceType)parameters[3];
            int resourceLimit = Convert.ToInt32(parameters[4]);
            Action onGatherResources = parameters[5] as Action;
            bool retreat = Convert.ToBoolean(parameters[6]);
            int tcGold = Convert.ToInt32(parameters[7]);
            int tcFood = Convert.ToInt32(parameters[8]);
            int tcWood = Convert.ToInt32(parameters[9]);


            behaviours.AddMultiThreadableBehaviours(0, () => { onGatherResources?.Invoke(); });

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
                        HandleResource(gold, resourceLimit, tcGold, 2);
                        break;
                    case ResourceType.Wood:
                        HandleResource(wood, resourceLimit, tcWood, 2);
                        break;
                    case ResourceType.Food:
                        HandleResource(food, resourceLimit, tcFood, 3);
                        break;
                    case ResourceType.None:
                        OnFlag?.Invoke(Flags.OnWait);
                        break;
                }
            });
            return behaviours;
        }

        private void HandleResource(int resource, int resourceLimit, int tcResource, int minResource)
        {
            if (resource >= resourceLimit || (tcResource <= 0 && resource >= minResource))
                OnFlag?.Invoke(Flags.OnFull);
            else if (tcResource <= 0 && resource < minResource) OnFlag?.Invoke(Flags.OnReturnResource);
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