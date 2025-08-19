using System;
using Agents.AnimalAgents;
using Utils;

namespace Agents.States.AnimalStates
{
    public class AnimalEatState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            if (parameters == null || parameters.Length < 5)
                throw new ArgumentException("Invalid parameters for GetTickBehaviour");

            BehaviourActions behaviours = new BehaviourActions();
            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            NodeType foodTarget = (NodeType)parameters[1];
            Action onEat = parameters[2] as Action;
            float[] outputBrain1 = parameters[3] as float[];
            float[] outputBrain2 = parameters[4] as float[];

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (currentNode == null || onEat == null) return;
                if (currentNode.Resource <= 0 || foodTarget != currentNode.NodeType) return;

                onEat.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (currentNode == null || currentNode.Resource <= 0 || foodTarget != currentNode.NodeType)
                {
                    OnFlag?.Invoke(Flags.OnSearchFood);
                    return;
                }

                if (outputBrain1 != null && outputBrain1[0] > 0.5f &&
                    currentNode.NodeType == foodTarget)
                {
                    OnFlag?.Invoke(Flags.OnEat);
                    return;
                }

                if (outputBrain2 != null && outputBrain2[0] > 0.5f) OnFlag?.Invoke(Flags.OnEscape);
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