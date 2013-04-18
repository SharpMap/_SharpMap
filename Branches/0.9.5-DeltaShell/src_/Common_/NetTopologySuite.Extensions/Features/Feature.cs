using System;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace NetTopologySuite.Extensions.Features
{
    public class Feature: IFeature
    {
        private long id;

        private IGeometry geometry;

        private IFeatureAttributeCollection attributes;

        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual IGeometry Geometry
        {
            get { return geometry; }
            set { geometry = value; }
        }

        public virtual IFeatureAttributeCollection Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }

        public virtual object Clone()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return Id 
                + " "  
                + Geometry != null ? Geometry.ToString() : "<no geometry>";
        }
    }
}
