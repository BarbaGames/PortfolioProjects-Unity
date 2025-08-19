using System;
using Agents.AnimalAgents;
using Utils;

namespace Agents.States.AnimalStates
{
    public class AnimalWalkState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            Action onMove = parameters[2] as Action;
            float[] outputBrain1 = parameters[3] as float[];
            float[] outputBrain2 = parameters[4] as float[];

            behaviours.AddMultiThreadableBehaviours(0, () => { onMove?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (outputBrain1 == null || outputBrain2 == null) return;
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

    public class AnimalWalkHerbState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            NodeType foodTarget = (NodeType)parameters[1];
            Action onMove = parameters[2] as Action;
            float[] outputBrain1 = parameters[3] as float[];
            float[] outputBrain2 = parameters[4] as float[];

            behaviours.AddMultiThreadableBehaviours(0, () => { onMove?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (outputBrain1 == null || outputBrain2 == null) return;
                if (outputBrain1[0] > 0.5f && currentNode != null && currentNode.NodeType == foodTarget)
                {
                    OnFlag?.Invoke(Flags.OnEat);
                    return;
                }

                if (outputBrain2[0] > 0.5f) OnFlag?.Invoke(Flags.OnEscape);
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