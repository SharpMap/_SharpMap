using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpMap.Geometries;

namespace SharpMap.Rasters
{
    public class Raster : IRaster
    {
        BoundingBox _boundingBox;

        public byte[] Data
        {
            get;
            private set;
        }

        public Raster(byte[] data, BoundingBox boundingBox)
        {
            this.Data = data;
            _boundingBox = boundingBox;
        }

        public BoundingBox GetBoundingBox()
        {
            return _boundingBox;
        }

        #region IGeometry Members

        public int Dimension
        {
            get { return 2; }
        }

        public int SRID
        {
            get;
            set;
        }

        public Geometry Envelope()
        {
            throw new NotImplementedException();
        }

        public string AsText()
        {
            throw new NotImplementedException();
        }

        public byte[] AsBinary()
        {
            throw new NotImplementedException();
        }

        public bool IsEmpty()
        {
            throw new NotImplementedException();
        }

        public bool IsSimple()
        {
            throw new NotImplementedException();
        }

        public Geometry Boundary()
        {
            throw new NotImplementedException();
        }

        public bool Relate(Geometry other, string intersectionPattern)
        {
            throw new NotImplementedException();
        }

        public bool Equals(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public bool Disjoint(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public bool Intersects(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public bool Touches(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public bool Crosses(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public bool Within(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public bool Contains(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public bool Overlaps(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public double Distance(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public Geometry Buffer(double d)
        {
            throw new NotImplementedException();
        }

        public Geometry ConvexHull()
        {
            throw new NotImplementedException();
        }

        public Geometry Intersection(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public Geometry Union(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public Geometry Difference(Geometry geom)
        {
            throw new NotImplementedException();
        }

        public Geometry SymDifference(Geometry geom)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
