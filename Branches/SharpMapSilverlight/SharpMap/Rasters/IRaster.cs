using System;
namespace SharpMap.Rasters
{
    public interface IRaster
    {
        byte[] Data { get; }
        SharpMap.Geometries.BoundingBox GetBoundingBox();
    }
}
