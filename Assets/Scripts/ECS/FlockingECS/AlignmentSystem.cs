using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECS.Patron;
using Utils;

namespace ECS.FlockingECS
{
    public class AlignmentSystem : EcsSystem
    {
        private IDictionary<uint, AcsComponent> ACSComponents;
        private List<(TransformComponent transform, AcsComponent acs)> entityData;
        private IEnumerable<uint> queriedEntities;
        private IDictionary<uint, TransformComponent> transformComponents;

        public override void Initialize()
        {
        }

        public override void Deinitialize()
        {
            queriedEntities = null;
        }

        protected override void PreExecute(float deltaTime)
        {
            queriedEntities ??= EcsManager.GetEntitiesWithComponentTypes(typeof(AcsComponent), typeof(TransformComponent));
            transformComponents ??= EcsManager.GetComponents<TransformComponent>();
            ACSComponents ??= EcsManager.GetComponents<AcsComponent>();
            entityData = queriedEntities.Select(id => (transformComponents[id], ACSComponents[id])).ToList();
        }

        protected override void Execute(float deltaTime)
        {
            Parallel.ForEach(entityData, GetParallelOptions(), data =>
            {
                if (data.transform.NearBoids.Count == 0) return;

                IVector avg = MyVector.zero();
                foreach (ITransform<IVector> b in data.transform.NearBoids)
                    avg += b.forward;

                avg /= data.transform.NearBoids.Count;
                data.acs.Alignment = EnsureValidVector(avg.Normalized());
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