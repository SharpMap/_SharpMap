using System;

namespace SharpMap.Layers
{
// ReSharper disable CSharpWarnings::CS1591
    public struct PointStruct
    {
        public PointStruct(float x, float y) : this()
        {
            X = x;
            Y = y;
        }

        public float X { get; set; }

        public float Y { get; set; }

        public override string ToString()
        {
            return String.Format("X: {0}, Y: {1}", X, Y);
        }
    }
// ReSharper restore CSharpWarnings::CS1591
}