using System;
using DelftTools.Utils.Data;
using GeoAPI.Geometries;

namespace GeoAPI.Extensions.Feature
{
    public interface IFeature : IUnique<long>, ICloneable
    {
        IGeometry Geometry { get; set; }

        // TODO: implement
        IFeatureAttributeCollection Attributes { get; set; }
    }
}