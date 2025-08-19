using System;

namespace Utils
{
    public interface ICoordinate<T> : IEquatable<T>
        where T : IEquatable<T>
    {
        void Add(T a);
        T Multiply(float b);
        float GetX();
        float GetY();
        void SetX(float x);
        void SetY(float y);
        float Distance(T b);
        float GetMagnitude();
        T GetCoordinate();
        void SetCoordinate(float x, float y);
        void SetCoordinate(T coordinate);
        int GetWeight();
        void Zero();
        void Perpendicular();
    }

    public class CoordinateNode : ICoordinate<IVector>, IEquatable<CoordinateNode>, ICoordinate<MyVector>
    {
        public IVector coordinate = new MyVector();
        private int weight;

        public bool Equals(IVector other)
        {
            const float epsilon = 0.0001f;
            return other != null && Math.Abs(coordinate.X - other.X) < epsilon &&
                   Math.Abs(coordinate.Y - other.Y) < epsilon;
        }

        public void Add(IVector a)
        {
            coordinate += a;
        }

        public IVector Multiply(float b)
        {
            return coordinate * b;
        }

        public float GetX()
        {
            return coordinate.X;
        }

        public float GetY()
        {
            return coordinate.Y;
        }

        public void SetX(float x)
        {
            coordinate.X = x;
        }

        public void SetY(float y)
        {
            coordinate.Y = y;
        }

        public float Distance(IVector b)
        {
            return MyVector.Distance(coordinate, b);
        }

        public float GetMagnitude()
        {
            return (float)Math.Sqrt(coordinate.X * coordinate.X + coordinate.Y * coordinate.Y);
        }

        public IVector GetCoordinate()
        {
            return coordinate;
        }

        public void SetCoordinate(float x, float y)
        {
            coordinate = new MyVector(x, y);
        }

        public void SetCoordinate(IVector coordinate)
        {
            this.coordinate = coordinate;
        }

        int ICoordinate<IVector>.GetWeight()
        {
            return weight;
        }

        public void Zero()
        {
            coordinate = MyVector.zero();
        }

        public void Perpendicular()
        {
            coordinate = new MyVector(-coordinate.Y, coordinate.X);
        }

        public void Add(MyVector a)
        {
            coordinate += a;
        }

        MyVector ICoordinate<MyVector>.Multiply(float b)
        {
            return coordinate * b;
        }

        public float Distance(MyVector b)
        {
            return MyVector.Distance(coordinate, b);
        }

        MyVector ICoordinate<MyVector>.GetCoordinate()
        {
            return (MyVector)coordinate;
        }

        public void SetCoordinate(MyVector coordinate)
        {
            this.coordinate = coordinate;
        }

        int ICoordinate<MyVector>.GetWeight()
        {
            return weight;
        }

        public bool Equals(MyVector other)
        {
            const float epsilon = 0.0001f;
            return other != null && Math.Abs(coordinate.X - other.X) < epsilon &&
                   Math.Abs(coordinate.Y - other.Y) < epsilon;
        }

        public bool Equals(CoordinateNode other)
        {
            const float epsilon = 0.0001f;
            return other != null && Math.Abs(coordinate.X - other.GetCoordinate().X) < epsilon &&
                   Math.Abs(coordinate.Y - other.GetCoordinate().Y) < epsilon;
        }
    }
}