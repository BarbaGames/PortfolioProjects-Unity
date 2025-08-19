using ECS.Patron;
using Utils;

namespace ECS.FlockingECS
{
    public class AcsComponent : EcsComponent
    {
        public IVector ACS;
        public IVector Alignment;
        public IVector Cohesion;
        public IVector Direction;
        public IVector Separation;
    }
}