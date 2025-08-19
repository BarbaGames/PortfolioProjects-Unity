using System.Collections.Generic;
using ECS.Patron;

namespace ECS.PathfinderECS
{
    public class PathResultComponent<TNodeType> : EcsComponent
    {
        public List<TNodeType> Path { get; set; }
        public bool PathFound => Path != null && Path.Count > 0;
    }
}