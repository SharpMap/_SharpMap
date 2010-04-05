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
    }
}
