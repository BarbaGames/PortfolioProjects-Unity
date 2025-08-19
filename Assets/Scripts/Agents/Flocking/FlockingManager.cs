using Utils;

namespace Agents.Flocking
{
    using SimBoid = Boid<IVector, ITransform<IVector>>;

    public class FlockingManager
    {
        public IVector Alignment(SimBoid boid)
        {
            if (boid.NearBoids.Count == 0) return boid.transform.forward;

            IVector avg = MyVector.zero();
            foreach (ITransform<IVector> b in boid.NearBoids) avg += b.forward;

            avg /= boid.NearBoids.Count;
            return EnsureValidVector(avg.Normalized());
        }

        public IVector Cohesion(SimBoid boid)
        {
            if (boid.NearBoids.Count == 0) return MyVector.zero();

            IVector avg = MyVector.zero();
            foreach (ITransform<IVector> b in boid.NearBoids) avg += b.position;

            avg /= boid.NearBoids.Count;
            MyVector average = avg - boid.transform.position;
            return EnsureValidVector(average.Normalized());
        }

        public IVector Separation(SimBoid boid)
        {
            if (boid.NearBoids.Count == 0) return MyVector.zero();

            IVector avg = MyVector.zero();
            foreach (ITransform<IVector> b in boid.NearBoids) avg += boid.transform.position - b.position;

            avg /= boid.NearBoids.Count;
            return EnsureValidVector(avg.Normalized());
        }

        public IVector Direction(SimBoid boid)
        {
            return EnsureValidVector(boid.transform.forward);
        }

        private IVector EnsureValidVector(IVector vector)
        {
            if (vector == null || float.IsNaN(vector.X) || float.IsNaN(vector.Y)) return MyVector.zero();

            return vector;
        }
    }
}