using System.Collections.Generic;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Layers;

namespace SharpMap.UI.Snapping
{
    public interface ISnapRule
    {
        ILayer SourceLayer { get; set; }
        int PixelGravity { get; set; }
        bool Obligatory { get; set; }

        ISnapResult Execute(IFeature sourceFeature, IGeometry sourceGeometry, IList<IFeature> snapTargets,
                            ICoordinate worldPos, IEnvelope envelope, int trackingIndex);
    }
}