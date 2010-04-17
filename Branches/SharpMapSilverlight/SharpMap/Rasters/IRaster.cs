using System;
using SharpMap.Geometries;
namespace SharpMap.Rasters
{
    public interface IRaster : IGeometry
    {
        byte[] Data { get; }
        SharpMap.Geometries.BoundingBox GetBoundingBox();
    }
}
