using System.Threading.Tasks;
using ECS.Patron;
using Utils;

namespace ECS.FlockingECS
{
    public class AcsSystem : EcsSystem
    {
        private int entityCount;
        private (BoidConfigComponent config, AcsComponent acs)[] entityDataArray;

        public override void Initialize()
        {
        }

        public override void Deinitialize()
        {
            entityDataArray = null;
        }


        protected override void PreExecute(float deltaTime)
        {
            (uint[] ids, BoidConfigComponent[] configs) = EcsManager.GetComponentsDirect<BoidConfigComponent>();
            (_, AcsComponent[] acsComponents) = EcsManager.GetComponentsDirect<AcsComponent>();

            entityCount = ids.Length;
            if (entityDataArray == null || entityDataArray.Length < entityCount)
                entityDataArray = new (BoidConfigComponent, AcsComponent)[entityCount];

            for (int i = 0; i < entityCount; i++) entityDataArray[i] = (configs[i], acsComponents[i]);
        }

        protected override void Execute(float deltaTime)
        {
            Parallel.For(0, entityCount, GetParallelOptions(), i =>
            {
                (BoidConfigComponent config, AcsComponent acs) = entityDataArray[i];
                IVector ACS = acs.Alignment * config.alignmentOffset +
                              acs.Cohesion * config.cohesionOffset +
                              acs.Separation * config.separationOffset +
                              acs.Direction * config.directionOffset;
                acs.ACS = EnsureValidVector(ACS.Normalized());
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