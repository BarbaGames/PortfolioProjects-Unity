using System;
using Agents.AnimalAgents;
using Utils;

namespace Agents.States.AnimalStates
{
    public class WalkCarnState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            if (parameters.Length < 4) return default;

            if (parameters[0] is not SimNode<IVector> currentNode) return default;
            if (parameters[1] is not IVector target) return default;
            if (parameters[2] is not Action onMove) return default;
            if (parameters[3] is not float[] outputBrain2) return default;

            behaviours.AddMultiThreadableBehaviours(0, () => { onMove?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (!(outputBrain2[0] > 0.5f) || !Approximately(target, currentNode?.GetCoordinate(), 0.2f)) return;
                OnFlag?.Invoke(Flags.OnAttack);
            });

            return behaviours;
        }

        private bool Approximately(IVector coord1, IVector coord2, float tolerance)
        {
            if (coord1 == null || coord2 == null) return false;
            return Math.Abs(coord1.X - coord2.X) <= tolerance && Math.Abs(coord1.Y - coord2.Y) <= tolerance;
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