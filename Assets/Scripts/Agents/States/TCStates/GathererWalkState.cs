using System;
using System.Collections.Generic;
using Agents.TCAgent;
using Utils;

namespace Agents.States.TCStates
{
    public class GathererWalkState : State
    {
        private static readonly NodeTerrain[] InvalidTargetTerrains =
        {
            NodeTerrain.Empty,
            NodeTerrain.Stump,
            NodeTerrain.Construction
        };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;
            List<SimNode<IVector>> path = parameters[4] as List<SimNode<IVector>>;


            behaviours.AddMultiThreadableBehaviours(0, onMove);

            behaviours.SetTransitionBehaviour(() => ProcessTransitions(currentNode, targetNode, retreat, path));
            return behaviours;
        }

        private void ProcessTransitions(SimNode<IVector> currentNode, SimNode<IVector> targetNode, bool retreat,
            List<SimNode<IVector>> path)
        {
            if (CheckRetreat(retreat, targetNode))
            {
                OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (IsInvalidState(currentNode, targetNode, path))
            {
                OnFlag?.Invoke(Flags.OnTargetLost);
                return;
            }

            if (!IsAdjacentOrNear(currentNode, targetNode)) return;

            HandleValidTarget(targetNode);
        }

        protected bool CheckRetreat(bool retreat, SimNode<IVector> targetNode)
        {
            return retreat && targetNode?.NodeTerrain != NodeTerrain.TownCenter;
        }

        protected virtual bool IsInvalidState(SimNode<IVector> currentNode, SimNode<IVector> targetNode,
            List<SimNode<IVector>> path)
        {
            return currentNode == null || targetNode == null || (path != null && path.Count == 0) ||
                   (targetNode.Resource <= 0 && IsResourceNode(targetNode)) ||
                   Array.IndexOf(InvalidTargetTerrains, targetNode.NodeTerrain) >= 0;
        }

        private bool IsResourceNode(SimNode<IVector> node)
        {
            return node.NodeTerrain == NodeTerrain.Mine ||
                   node.NodeTerrain == NodeTerrain.Tree ||
                   node.NodeTerrain == NodeTerrain.Lake;
        }

        protected bool IsAdjacentOrNear(SimNode<IVector> currentNode, SimNode<IVector> targetNode)
        {
            return currentNode.GetCoordinate().Adyacent(targetNode.GetCoordinate()) ||
                   Approximately(currentNode.GetCoordinate(), targetNode.GetCoordinate(), 0.001f);
        }

        private void HandleValidTarget(SimNode<IVector> targetNode)
        {
            switch (targetNode.NodeTerrain)
            {
                case NodeTerrain.Mine:
                case NodeTerrain.Lake:
                case NodeTerrain.Tree:
                    OnFlag?.Invoke(Flags.OnGather);
                    break;
                case NodeTerrain.TownCenter:
                case NodeTerrain.WatchTower:
                    OnFlag?.Invoke(Flags.OnWait);
                    break;
                default:
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    break;
            }
        }

        protected static bool Approximately(IVector a, IVector b, float tolerance)
        {
            return Math.Abs(a.X - b.X) <= tolerance &&
                   Math.Abs(a.Y - b.Y) <= tolerance;
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

    public class CartWalkState : GathererWalkState
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;
            bool returnResource = (bool)parameters[4];
            List<SimNode<IVector>> path = parameters[5] as List<SimNode<IVector>>;

            behaviours.AddMultiThreadableBehaviours(0, onMove);

            behaviours.SetTransitionBehaviour(() =>
                ProcessCartTransitions(currentNode, targetNode, retreat, returnResource, path));
            return behaviours;
        }

        private void ProcessCartTransitions(
            SimNode<IVector> currentNode,
            SimNode<IVector> targetNode,
            bool retreat,
            bool returnResource,
            List<SimNode<IVector>> path)
        {
            if (CheckRetreat(retreat, targetNode))
            {
                OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (IsInvalidState(currentNode, targetNode, path))
            {
                OnFlag?.Invoke(Flags.OnTargetLost);
                return;
            }

            if (!IsAdjacentOrNear(currentNode, targetNode)) return;

            if (returnResource && targetNode.NodeTerrain == NodeTerrain.TownCenter)
            {
                OnFlag?.Invoke(Flags.OnReturnResource);
                return;
            }

            if (targetNode.NodeTerrain == NodeTerrain.TownCenter)
            {
                OnFlag?.Invoke(Flags.OnGather);
                return;
            }

            OnFlag?.Invoke(Flags.OnTargetReach);
        }

        protected override bool IsInvalidState(SimNode<IVector> currentNode, SimNode<IVector> targetNode,
            List<SimNode<IVector>> path)
        {
            return currentNode == null || targetNode == null || (path != null && path.Count == 0);
        }

        protected bool CheckRetreat(bool retreat, SimNode<IVector> targetNode)
        {
            return retreat && targetNode?.NodeTerrain != NodeTerrain.TownCenter &&
                   targetNode?.NodeTerrain != NodeTerrain.WatchTower;
        }
    }

    public class BuilderWalkState : GathererWalkState
    {
        private static readonly NodeTerrain[] ValidBuilderTerrains =
        {
            NodeTerrain.Empty,
            NodeTerrain.Construction,
            NodeTerrain.WatchTower,
            NodeTerrain.TownCenter
        };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;
            List<SimNode<IVector>> path = parameters[4] as List<SimNode<IVector>>;

            behaviours.AddMultiThreadableBehaviours(0, onMove);

            behaviours.SetTransitionBehaviour(() => ProcessBuilderTransitions(currentNode, targetNode, retreat, path));
            return behaviours;
        }

        private void ProcessBuilderTransitions(
            SimNode<IVector> currentNode,
            SimNode<IVector> targetNode,
            bool retreat,
            List<SimNode<IVector>> path)
        {
            if (CheckRetreat(retreat, targetNode))
            {
                OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (IsInvalidBuilderState(currentNode, targetNode, path))
            {
                OnFlag?.Invoke(Flags.OnTargetLost);
                return;
            }

            if (!IsAdjacentOrNear(currentNode, targetNode)) return;

            if (targetNode.NodeTerrain is NodeTerrain.TownCenter or NodeTerrain.WatchTower)
            {
                OnFlag?.Invoke(Flags.OnWait);
                return;
            }

            OnFlag?.Invoke(Flags.OnBuild);
        }

        private bool IsInvalidBuilderState(SimNode<IVector> currentNode, SimNode<IVector> targetNode,
            List<SimNode<IVector>> path)
        {
            return currentNode == null || targetNode == null ||
                   (path != null && path.Count == 0) ||
                   Array.IndexOf(ValidBuilderTerrains, targetNode.NodeTerrain) == -1;
        }
    }
}