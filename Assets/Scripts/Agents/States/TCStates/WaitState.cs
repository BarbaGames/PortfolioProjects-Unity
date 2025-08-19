using System;
using Agents.TCAgent;
using Utils;

namespace Agents.States.TCStates
{
    public class WaitState : State
    {
        private static readonly int GoldCost = 2;
        private static readonly int WoodCost = 4;

        private static readonly NodeTerrain[] SafeRetreatTerrains =
            { NodeTerrain.TownCenter, NodeTerrain.WatchTower };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            bool retreat = (bool)parameters[0];
            int? food = Convert.ToInt32(parameters[1]);
            int? gold = Convert.ToInt32(parameters[2]);
            int? wood = Convert.ToInt32(parameters[3]);
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[4];
            SimNode<IVector> targetNode = (SimNode<IVector>)parameters[5];
            Action onWait = parameters[6] as Action;

            behaviours.AddMultiThreadableBehaviours(0, onWait);

            behaviours.SetTransitionBehaviour(() =>
                ProcessTransitions(retreat, food, gold, wood, currentNode, targetNode));

            return behaviours;
        }

        private void ProcessTransitions(bool retreat, int? food, int? gold, int? wood, SimNode<IVector> currentNode,
            SimNode<IVector> targetNode)
        {
            if (retreat)
            {
                if (Array.IndexOf(SafeRetreatTerrains, currentNode.NodeTerrain) == -1)
                    OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (food > 0 && gold >= GoldCost && wood >= WoodCost)
            {
                if (targetNode.NodeTerrain == NodeTerrain.Construction)
                    OnFlag?.Invoke(Flags.OnBuild);
                else
                    OnFlag?.Invoke(Flags.OnTargetLost);
            }
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

    public class GathererWaitState : State
    {
        private const int MinFood = 3;

        private static readonly NodeTerrain[] SafeRetreatTerrains =
            { NodeTerrain.TownCenter, NodeTerrain.WatchTower };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = (bool)parameters[0];
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[1];
            Action onWait = parameters[2] as Action;
            int currentFood = Convert.ToInt32(parameters[3]);
            int currentGold = Convert.ToInt32(parameters[4]);
            int currentWood = Convert.ToInt32(parameters[5]);

            behaviours.AddMultiThreadableBehaviours(0, onWait);

            behaviours.SetTransitionBehaviour(() =>
                ProcessTransitions(retreat, currentNode, currentFood, currentGold, currentWood));

            return behaviours;
        }

        private void ProcessTransitions(bool retreat, SimNode<IVector> currentNode, int currentFood, int currentGold,
            int currentWood)
        {
            if (retreat)
            {
                if (Array.IndexOf(SafeRetreatTerrains, currentNode.NodeTerrain) == -1)
                    OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (currentGold > 0 || currentWood > 0 || currentFood > MinFood) return;

            OnFlag?.Invoke(Flags.OnGather);
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

    public class CartWaitState : State
    {
        private static readonly NodeTerrain[] SafeRetreatTerrains =
            { NodeTerrain.TownCenter, NodeTerrain.WatchTower };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = (bool)parameters[0];
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[1];
            Action onWait = parameters[2] as Action;

            behaviours.AddMultiThreadableBehaviours(0, onWait);

            behaviours.SetTransitionBehaviour(() => ProcessTransitions(retreat, currentNode));

            return behaviours;
        }

        private void ProcessTransitions(bool retreat, SimNode<IVector> currentNode)
        {
            if (retreat)
            {
                if (Array.IndexOf(SafeRetreatTerrains, currentNode.NodeTerrain) == -1) OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            OnFlag?.Invoke(Flags.OnReturnResource);
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