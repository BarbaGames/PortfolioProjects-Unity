using ECS.Patron;

namespace ECS.PathfinderECS
{
    public class GraphComponent<TNodeType> : EcsComponent
    {
        public TNodeType[,] Graph { get; set; }
    }
}