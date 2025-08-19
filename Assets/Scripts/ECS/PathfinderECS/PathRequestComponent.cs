using ECS.Patron;

namespace ECS.PathfinderECS
{
    public class PathRequestComponent<TNodeType> : EcsComponent
    {
        public TNodeType StartNode { get; set; }
        public TNodeType DestinationNode { get; set; }
        public bool IsProcessed { get; set; }
    }
}