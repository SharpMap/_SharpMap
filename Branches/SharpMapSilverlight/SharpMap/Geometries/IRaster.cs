using System;
using SharpMap.Geometries;
namespace SharpMap.Geometries
{
    public interface IRaster : IGeometry
    {
        byte[] Data { get; }
        SharpMap.Geometries.BoundingBox GetBoundingBox();
    }
}
