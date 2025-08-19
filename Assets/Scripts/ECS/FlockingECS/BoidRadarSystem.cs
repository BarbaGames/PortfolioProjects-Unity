using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECS.Patron;
using Utils;

namespace ECS.FlockingECS
{
    public class BoidRadarSystem : EcsSystem
    {
        private IDictionary<uint, AcsComponent> ACSComponents;
        private IDictionary<uint, BoidConfigComponent> boidConfigComponents;
        private List<(uint id, IVector position, ITransform<IVector> transform)> boidData;
        private IEnumerable<uint> queriedEntities;
        private IDictionary<uint, TransformComponent> transformComponents;

        public override void Initialize()
        {
        }

        public override void Deinitialize()
        {
            boidConfigComponents = null;
            queriedEntities = null;
            transformComponents = null;
            ACSComponents = null;
        }

        protected override void PreExecute(float deltaTime)
        {
            boidConfigComponents ??= EcsManager.GetComponents<BoidConfigComponent>();
            queriedEntities ??= EcsManager.GetEntitiesWithComponentTypes(typeof(BoidConfigComponent),
                typeof(TransformComponent), typeof(AcsComponent));
            transformComponents ??= EcsManager.GetComponents<TransformComponent>();
            ACSComponents ??= EcsManager.GetComponents<AcsComponent>();
            boidData = queriedEntities
                .Select(id => (id, transformComponents[id].Transform.position, transformComponents[id].Transform)).ToList();
        }

        protected override void Execute(float deltaTime)
        {
            Parallel.ForEach(queriedEntities, GetParallelOptions(), boidId =>
            {
                BoidConfigComponent boidConfig = boidConfigComponents[boidId];
                float detectionRadiusSquared = boidConfig.detectionRadius * boidConfig.detectionRadius;
                IVector boidPosition = transformComponents[boidId].Transform.position;

                List<ITransform<IVector>> nearBoids = new List<ITransform<IVector>>();
                foreach ((uint nearId, IVector nearPos, ITransform<IVector> nearTransform) in boidData)
                {
                    if (boidId == nearId || nearPos == null) continue;

                    float distanceSquared = IVector.DistanceSquared(boidPosition, nearPos);
                    if (distanceSquared <= detectionRadiusSquared)
                        nearBoids.Add(nearTransform);
                }

                transformComponents[boidId].NearBoids = nearBoids;
            });
        }

        protected override void PostExecute(float deltaTime)
        {
        }
    }
}