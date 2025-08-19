using System;
using Agents.TCAgent;

namespace Agents.States.TCStates
{
    public class ReturnResourceState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            int gold = Convert.ToInt32(parameters[0]);
            int food = Convert.ToInt32(parameters[1]);
            int wood = Convert.ToInt32(parameters[2]);
            Action onReturnResource = parameters[3] as Action;
            bool retreat = Convert.ToBoolean(parameters[4]);

            behaviours.AddMultiThreadableBehaviours(0, () => { onReturnResource?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (gold <= 0 && wood <= 0 && food <= 0)
                {
                    OnFlag?.Invoke(Flags.OnHunger);
                    return;
                }

                if (retreat) OnFlag?.Invoke(Flags.OnRetreat);
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