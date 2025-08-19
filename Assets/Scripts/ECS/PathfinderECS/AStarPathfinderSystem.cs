using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ECS.Patron;
using Utils;

namespace ECS.PathfinderECS
{
    public class AStarPathfinderSystem<TNodeType, TCoordinateType, TCoordinate> : EcsSystem
        where TNodeType : INode, INode<TCoordinateType>, new()
        where TCoordinateType : IEquatable<TCoordinateType>, IVector
        where TCoordinate : ICoordinate<TCoordinateType>, new()
    {
        private ConcurrentDictionary<uint, GraphComponent<TNodeType>> _graphs;
        private ConcurrentDictionary<uint, PathRequestComponent<TNodeType>> _pendingRequests;

        public override void Initialize()
        {
            _pendingRequests = new ConcurrentDictionary<uint, PathRequestComponent<TNodeType>>();
        }

        protected override void PreExecute(float deltaTime)
        {
            // Get graph from the first entity with a GraphComponent
            ConcurrentDictionary<uint, GraphComponent<TNodeType>> graphComponents =
                EcsManager.GetComponents<GraphComponent<TNodeType>>();

            if (graphComponents is { Count: > 0 }) _graphs = graphComponents;

            // Get all unprocessed path requests
            ConcurrentDictionary<uint, PathRequestComponent<TNodeType>> requests =
                EcsManager.GetComponents<PathRequestComponent<TNodeType>>();
            if (requests == null) return;

            foreach (KeyValuePair<uint, PathRequestComponent<TNodeType>> request in requests)
                if (!request.Value.IsProcessed)
                    _pendingRequests.TryAdd(request.Key, request.Value);
        }

        protected override void Execute(float deltaTime)
        {
            if (_graphs == null || _pendingRequests.Count == 0) return;

            // Process path requests in parallel
            Parallel.ForEach(_pendingRequests, GetParallelOptions(), request =>
            {
                uint entityId = request.Key;
                EcsFlag unitType = EcsManager.GetFlag<EcsFlag>(entityId);
                PathRequestComponent<TNodeType> pathRequest = request.Value;
                uint graphId = unitType.Flag switch
                {
                    FlagType.None => 0,
                    FlagType.Cart => 0,
                    FlagType.Gatherer => 1,
                    FlagType.Builder => 2,
                    _ => 0
                };

                // Find the path
                List<TNodeType> path = FindPath(pathRequest.StartNode, pathRequest.DestinationNode, graphId);

                // Mark request as processed
                pathRequest.IsProcessed = true;

                // Create or update path result
                if (!EcsManager.ContainsComponent<PathResultComponent<TNodeType>>(entityId))
                {
                    EcsManager.AddComponent(entityId, new PathResultComponent<TNodeType> { Path = path });
                }
                else
                {
                    PathResultComponent<TNodeType> pathResult =
                        EcsManager.GetComponent<PathResultComponent<TNodeType>>(entityId);
                    pathResult.Path = path;
                }
            });

            _pendingRequests.Clear();
        }

        protected override void PostExecute(float deltaTime)
        {
            // Nothing needed here
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<TNodeType> FindPath(TNodeType startNode, TNodeType destinationNode, uint graphId)
        {
            if (NodesEquals(startNode, destinationNode))
                return new List<TNodeType> { startNode };

            if (IsBlocked(destinationNode))
            {
                bool foundAlternative = false;
                foreach (TCoordinateType altCoord in GetAlternativeCoordinates(destinationNode.GetCoordinate(), graphId))
                {
                    TNodeType candidate = _graphs[graphId].Graph[(int)altCoord.X, (int)altCoord.Y];
                    if (!IsBlocked(candidate))
                    {
                        destinationNode = candidate;
                        foundAlternative = true;
                        break;
                    }
                }

                if (!foundAlternative) return null;
            }

            const int estimatedNodesCount = 1024;
            FastPriorityQueue<TNodeType> openSet = new FastPriorityQueue<TNodeType>(estimatedNodesCount);
            ConcurrentDictionary<TNodeType, int> gScore = new ConcurrentDictionary<TNodeType, int>();
            ConcurrentDictionary<TNodeType, TNodeType> cameFrom = new ConcurrentDictionary<TNodeType, TNodeType>();
            ConcurrentDictionary<TNodeType, byte> closedSet = new ConcurrentDictionary<TNodeType, byte>();

            TCoordinate destCoord = new TCoordinate();
            destCoord.SetCoordinate(destinationNode.GetCoordinate());

            openSet.Enqueue(startNode, 0);
            gScore[startNode] = startNode.GetCost();

            object openSetLock = new object();

            while (openSet.Count > 0)
            {
                TNodeType current = openSet.Dequeue();
                if (!closedSet.TryAdd(current, 0)) continue;

                if (NodesEquals(current, destinationNode)) return ReconstructPath(cameFrom, current);

                int currentGScore = gScore[current];
                List<TCoordinateType> neighbors = GetNeighbors(current).ToList();

                if (neighbors.Count > 6)
                    Parallel.ForEach(neighbors, GetParallelOptions(), neighborCoord =>
                    {
                        ProcessNeighbor(current, neighborCoord, currentGScore, closedSet, gScore, cameFrom, openSet,
                            openSetLock, destCoord, graphId);
                    });
                else
                    foreach (TCoordinateType neighborCoord in neighbors)
                        ProcessNeighbor(current, neighborCoord, currentGScore, closedSet, gScore, cameFrom, openSet,
                            openSetLock, destCoord, graphId);
            }

            return null;
        }

        private void ProcessNeighbor(TNodeType current, TCoordinateType neighborCoord, int currentGScore,
            ConcurrentDictionary<TNodeType, byte> closedSet, ConcurrentDictionary<TNodeType, int> gScore,
            ConcurrentDictionary<TNodeType, TNodeType> cameFrom, FastPriorityQueue<TNodeType> openSet, object openSetLock,
            TCoordinate destCoord, uint graphId)
        {
            TNodeType neighbor = _graphs[graphId].Graph[(int)neighborCoord.X, (int)neighborCoord.Y];

            if (closedSet.ContainsKey(neighbor)) return;

            int tentativeG = currentGScore + MoveToNeighborCost(current, neighbor);

            bool updated = false;
            gScore.AddOrUpdate(neighbor, tentativeG, (key, oldValue) =>
            {
                if (tentativeG < oldValue)
                {
                    updated = true;
                    return tentativeG;
                }

                return oldValue;
            });

            if (!updated && gScore[neighbor] != tentativeG) return;

            cameFrom[neighbor] = current;
            int fCost = tentativeG + Heuristic(neighbor.GetCoordinate(), destCoord);

            lock (openSetLock)
            {
                if (!openSet.Contains(neighbor))
                    openSet.Enqueue(neighbor, fCost);
                else
                    openSet.UpdatePriority(neighbor, fCost);
            }
        }

        private List<TNodeType> ReconstructPath(ConcurrentDictionary<TNodeType, TNodeType> cameFrom, TNodeType current)
        {
            List<TNodeType> path = new List<TNodeType>();
            while (cameFrom.TryGetValue(current, out TNodeType parent))
            {
                path.Add(current);
                current = parent;
            }

            path.Add(current);
            path.Reverse();
            return path;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Heuristic(TCoordinateType aCoord, TCoordinate b)
        {
            TCoordinate a = new TCoordinate();
            a.SetCoordinate(aCoord);
            return Distance(a, b);
        }

        private int Distance(TCoordinate A, TCoordinate B)
        {
            if (A == null || B == null)
                return int.MaxValue;

            float distance = Math.Abs(A.GetX() - B.GetX()) + Math.Abs(A.GetY() - B.GetY());
            return (int)distance;
        }

        private ICollection<TCoordinateType> GetNeighbors(TNodeType node)
        {
            return node.GetNeighbors();
        }

        private bool IsBlocked(TNodeType node)
        {
            return node.IsBlocked();
        }

        private ICollection<TCoordinateType> GetAlternativeCoordinates(TCoordinateType blockedCoordinate, uint graphId)
        {
            List<TCoordinateType> alternatives = new List<TCoordinateType>();

            int width = _graphs[graphId].Graph.GetLength(0);
            int height = _graphs[graphId].Graph.GetLength(1);

            int x = (int)blockedCoordinate.X;
            int y = (int)blockedCoordinate.Y;

            int[] dx = { -1, 0, 1, 0 };
            int[] dy = { 0, -1, 0, 1 };

            for (int i = 0; i < dx.Length; i++)
            {
                int newX = x + dx[i];
                int newY = y + dy[i];

                if (newX < 0 || newY < 0 || newX >= width || newY >= height)
                    continue;

                alternatives.Add(_graphs[graphId].Graph[newX, newY].GetCoordinate());
            }

            return alternatives;
        }

        private int MoveToNeighborCost(TNodeType A, TNodeType B)
        {
            if (!GetNeighbors(A).Contains(B.GetCoordinate()))
                throw new InvalidOperationException("B node has to be a neighbor.");

            return B.GetCost() + A.GetCost();
        }

        private bool NodesEquals(TNodeType A, TNodeType B)
        {
            if (A == null || B == null || A.GetCoordinate() == null || B.GetCoordinate() == null)
                return false;

            return A.GetCoordinate().Equals(B.GetCoordinate());
        }
    }
}