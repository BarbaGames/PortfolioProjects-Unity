using System.Collections.Generic;
using ECS.Patron;
using Utils;

namespace ECS.FlockingECS
{
    public class TransformComponent : EcsComponent
    {
        public List<ITransform<IVector>> NearBoids = new();
        public ITransform<IVector> Transform = new();
    }
}