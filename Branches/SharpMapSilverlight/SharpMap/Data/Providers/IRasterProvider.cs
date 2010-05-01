using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpMap.Geometries;
using SharpMap.Rasters;

namespace SharpMap.Data.Providers
{
    public interface IRasterProvider : IProvider
    {
        IList<IRaster> GetRastersInView(BoundingBox bbox, double resolution);
    }
}
