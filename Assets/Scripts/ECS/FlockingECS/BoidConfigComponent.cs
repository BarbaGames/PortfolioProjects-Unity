using ECS.Patron;

namespace ECS.FlockingECS
{
    public class BoidConfigComponent : EcsComponent
    {
        public float alignmentOffset;
        public float cohesionOffset;
        public float detectionRadius = 6.0f;
        public float directionOffset;
        public float separationOffset;

        public BoidConfigComponent(float detectionRadius, float alignmentOffset, float cohesionOffset,
            float separationOffset, float directionOffset)
        {
            this.detectionRadius = detectionRadius;
            this.alignmentOffset = alignmentOffset;
            this.cohesionOffset = cohesionOffset;
            this.separationOffset = separationOffset;
            this.directionOffset = directionOffset;
        }
    }
}