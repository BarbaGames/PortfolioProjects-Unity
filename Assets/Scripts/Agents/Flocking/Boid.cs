using System;
using System.Collections.Generic;
using Utils;

namespace Agents.Flocking
{
    public class Boid<TVector, TTransform>
        where TVector : IVector, IEquatable<TVector>
        where TTransform : ITransform<TVector>, new()
    {
        private Func<Boid<TVector, TTransform>, TVector> alignment;
        public float alignmentOffset;
        private Func<Boid<TVector, TTransform>, TVector> cohesion;
        public float cohesionOffset;
        public float detectionRadious = 6.0f;
        private Func<Boid<TVector, TTransform>, TVector> direction;
        public float directionOffset;
        private Func<Boid<TVector, TTransform>, TVector> separation;
        public float separationOffset;
        public TTransform transform = new();
        public List<ITransform<IVector>> NearBoids { get; set; }

        public void Init(Func<Boid<TVector, TTransform>, TVector> Alignment,
            Func<Boid<TVector, TTransform>, TVector> Cohesion,
            Func<Boid<TVector, TTransform>, TVector> Separation,
            Func<Boid<TVector, TTransform>, TVector> Direction)
        {
            alignment = Alignment;
            cohesion = Cohesion;
            separation = Separation;
            direction = Direction;
        }

        public IVector ACS()
        {
            IVector ACS = alignment(this) * alignmentOffset +
                          cohesion(this) * cohesionOffset +
                          separation(this) * separationOffset +
                          direction(this) * directionOffset;
            return ACS.Normalized();
        }

        public TVector GetDirection()
        {
            return direction(this);
        }

        public TVector GetAlignment()
        {
            return alignment(this);
        }

        public TVector GetCohesion()
        {
            return cohesion(this);
        }

        public TVector GetSeparation()
        {
            return separation(this);
        }
    }
}