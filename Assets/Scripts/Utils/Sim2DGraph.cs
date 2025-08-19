using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Utils
{
    public class Sim2DGraph : SimGraph<SimNode<IVector>, CoordinateNode, IVector>
    {
        private const int MaxTerrains = 20;

        private readonly ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = 10
        };

        private int lakes;
        private int mines;
        private int trees;

        public Sim2DGraph(int x, int y, float cellSize) : base(x, y, cellSize)
        {
        }

        public int MinX => 0;
        public int MaxX => NodesType.GetLength(0);
        public int MinY => 0;
        public int MaxY => NodesType.GetLength(1);
        public float CellSize2 => CellSize;

        public CoordinateNode MapSize { get; } = new();

        public override void CreateGraph(int x, int y, float cellSize)
        {
            MapSize.SetCoordinate(x, y);
            Parallel.For(0, x, parallelOptions, i =>
            {
                for (int j = 0; j < y; j++)
                {
                    Random random = new Random();
                    double type = random.NextDouble();

                    SimNode<IVector> nodeType = new SimNode<IVector>();
                    nodeType.SetCoordinate(new MyVector(i * cellSize, j * cellSize));
                    nodeType.X = i * cellSize;
                    nodeType.Y = j * cellSize;
                    nodeType.NodeType = GetNodeType(type);
                    if (nodeType.NodeType == NodeType.Lake) nodeType.NodeTerrain = NodeTerrain.Lake;
                    NodesType[i, j] = nodeType;
                }
            });
            AssignRandomTerrains();
        }

        public void AssignRandomTerrains()
        {
            List<SimNode<IVector>> allNodes = new List<SimNode<IVector>>();

            for (int i = 0; i < NodesType.GetLength(0); i++)
            for (int j = 0; j < NodesType.GetLength(1); j++)
                allNodes.Add(NodesType[i, j]);

            Random random = new Random();
            allNodes = allNodes.OrderBy(x => random.Next()).ToList();

            Parallel.For(0, 3 * MaxTerrains, parallelOptions, i =>
            {
                if (i < MaxTerrains && i < allNodes.Count)
                    allNodes[i].NodeTerrain = NodeTerrain.Mine;
                else if (i < 2 * MaxTerrains && i < allNodes.Count)
                    allNodes[i].NodeTerrain = NodeTerrain.Tree;
                else if (i < 3 * MaxTerrains && i < allNodes.Count) allNodes[i].NodeTerrain = NodeTerrain.Stump;
            });
        }

        private NodeType GetNodeType(double type)
        {
            NodeType nodeType = type switch
            {
                < 0.98 => NodeType.Plains,
                < 0.985 => NodeType.Mountain,
                < 0.997 => NodeType.Sand,
                < 1 => NodeType.Lake,
                _ => NodeType.Plains
            };
            if (nodeType != NodeType.Lake) return nodeType;

            lakes++;
            if (lakes > MaxTerrains) nodeType = NodeType.Plains;
            return nodeType;
        }

        private NodeTerrain GetTerrain(int nodeTerrain)
        {
            NodeTerrain terrain = nodeTerrain switch
            {
                < 60 => NodeTerrain.Empty,
                < 80 => NodeTerrain.Mine,
                < 100 => NodeTerrain.Tree,
                _ => NodeTerrain.Empty
            };

            switch (terrain)
            {
                case NodeTerrain.Mine:
                    mines++;
                    if (mines > MaxTerrains) terrain = NodeTerrain.Empty;
                    break;
                case NodeTerrain.Tree:
                    trees++;
                    if (trees > MaxTerrains) terrain = NodeTerrain.Empty;
                    break;
            }

            return terrain;
        }

        public bool IsWithinGraphBorders(IVector position)
        {
            return position.X >= MinX && position.X <= MaxX - 1 &&
                   position.Y >= MinY && position.Y <= MaxY - 1;
        }

        public class NodeData
        {
            public int NodeType { get; set; }
            public int NodeTerrain { get; set; }
        }
    }
}