using System;
using DelftTools.Utils.Data;
using GeoAPI.Geometries;
using System.Drawing;
using GeoAPI.Extensions.Feature;

namespace SharpMap.UI.Editors
{
    public class TrackerFeature : Unique<long>, ITrackerFeature
    {
        public IGeometry Geometry { get; set; }

        public bool Selected { get; set; }
        public Bitmap Bitmap { get; set; }
        public IFeatureEditor FeatureEditor { get; set; }
        public int Index { get; private set; }

        public TrackerFeature(IFeatureEditor featureMutator, IGeometry geometry, int index, Bitmap bitmap)
        {
            FeatureEditor = featureMutator;
            Geometry = geometry;
            Bitmap = bitmap;
            Index = index;
        }

        public TrackerFeature()
        {
        }

        public IFeatureAttributeCollection Attributes
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }
    }
}