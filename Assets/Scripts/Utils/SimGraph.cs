using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Utils
{
    public abstract class SimGraph<TNodeType, TCoordinateNode, TCoordinateType>
        where TNodeType : INode<TCoordinateType>
        where TCoordinateNode : ICoordinate<TCoordinateType>, new()
        where TCoordinateType : IEquatable<TCoordinateType>
    {
        public static TCoordinateNode MapDimensions;
        public static TCoordinateNode OriginPosition;
        public static float CellSize;
        public readonly TNodeType[,] NodesType;

        private readonly ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = 10
        };

        public SimGraph(int x, int y, float cellSize)
        {
            MapDimensions = new TCoordinateNode();
            MapDimensions.SetCoordinate(x, y);
            CellSize = cellSize;

            NodesType = new TNodeType[x, y];

            CreateGraph(x, y, cellSize);
            AddNeighbors();
        }

        public abstract void CreateGraph(int x, int y, float cellSize);

        /// <summary>
        ///     Adds the neighbors for each node in the grid by taking advantage
        ///     of the grid structure rather than doing an exhaustive search.
        /// </summary>
        private void AddNeighbors()
        {
            int width = NodesType.GetLength(0);
            int height = NodesType.GetLength(1);

            Parallel.For(0, width, parallelOptions, i =>
            {
                for (int j = 0; j < height; j++)
                {
                    // Since we’re on a grid, a node has at most 4 neighbors.
                    List<TCoordinateType> neighbors = new List<TCoordinateType>(4);

                    // Add the left neighbor.
                    if (i > 0) neighbors.Add(NodesType[i - 1, j].GetCoordinate());

                    // Add the right neighbor.
                    if (i < width - 1) neighbors.Add(NodesType[i + 1, j].GetCoordinate());

                    // Add the bottom neighbor.
                    if (j > 0) neighbors.Add(NodesType[i, j - 1].GetCoordinate());

                    // Add the top neighbor.
                    if (j < height - 1) neighbors.Add(NodesType[i, j + 1].GetCoordinate());

                    NodesType[i, j].SetNeighbors(neighbors);
                }
            });
        }
    }
}