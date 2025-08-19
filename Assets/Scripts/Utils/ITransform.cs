using System;

namespace Utils
{
    public class ITransform<TVector>
        where TVector : IVector, IEquatable<TVector>
    {
        public TVector forward;

        public ITransform(TVector position)
        {
            this.position = position;
        }

        public ITransform()
        {
            position = default;
            forward = default;
        }

        public TVector position { get; set; }
    }
}