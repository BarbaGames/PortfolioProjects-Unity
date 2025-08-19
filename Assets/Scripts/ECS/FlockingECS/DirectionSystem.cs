using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECS.Patron;
using Utils;

namespace ECS.FlockingECS
{
    public class DirectionSystem : EcsSystem
    {
        private List<(TransformComponent transform, AcsComponent acs)> entityData;
        private IEnumerable<uint> queriedEntities;

        public override void Initialize()
        {
        }

        public override void Deinitialize()
        {
            queriedEntities = null;
            entityData = null;
        }

        protected override void PreExecute(float deltaTime)
        {
            queriedEntities ??= EcsManager.GetEntitiesWithComponentTypes(typeof(AcsComponent), typeof(TransformComponent));
            ConcurrentDictionary<uint, TransformComponent> transformComponents = EcsManager.GetComponents<TransformComponent>();
            ConcurrentDictionary<uint, AcsComponent> ACSComponents = EcsManager.GetComponents<AcsComponent>();

            entityData = queriedEntities.Select(id => (transform: transformComponents[id], acs: ACSComponents[id]))
                .ToList();
        }

        protected override void Execute(float deltaTime)
        {
            Parallel.ForEach(entityData, GetParallelOptions(), data =>
            {
                if (data.transform.NearBoids.Count == 0) return;

                IVector avgDirection = MyVector.zero();
                foreach (ITransform<IVector> neighbor in data.transform.NearBoids)
                {
                    if (neighbor?.position == null) continue;
                    avgDirection += neighbor.position - data.transform.Transform.position;
                }

                avgDirection /= data.transform.NearBoids.Count;

                data.acs.Direction = EnsureValidVector(avgDirection.Normalized());
            });
        }

        protected override void PostExecute(float deltaTime)
        {
        }

        private IVector EnsureValidVector(IVector vector)
        {
            if (vector == null || float.IsNaN(vector.X) || float.IsNaN(vector.Y)) return MyVector.zero();
            return vector;
        }
    }
}