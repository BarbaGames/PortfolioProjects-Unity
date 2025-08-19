using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECS.Patron;
using Utils;

namespace ECS.FlockingECS
{
    public class CohesionSystem : EcsSystem
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
            queriedEntities ??=
                EcsManager.GetEntitiesWithComponentTypes(typeof(AcsComponent), typeof(TransformComponent));
            ConcurrentDictionary<uint, TransformComponent> transformComponents = EcsManager.GetComponents<TransformComponent>();
            ConcurrentDictionary<uint, AcsComponent> ACSComponents = EcsManager.GetComponents<AcsComponent>();

            entityData = queriedEntities
                .Select(id => (transform: transformComponents[id], acs: ACSComponents[id]))
                .ToList();
        }

        protected override void Execute(float deltaTime)
        {
            Parallel.ForEach(entityData, GetParallelOptions(), data =>
            {
                if (data.transform.NearBoids.Count == 0) return;

                IVector avg = MyVector.zero();
                foreach (ITransform<IVector> b in data.transform.NearBoids) avg += b.position;

                avg /= data.transform.NearBoids.Count;
                IVector direction = avg - data.transform.Transform.position; // Corrected logic
                data.acs.Cohesion = EnsureValidVector(direction.Normalized());
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