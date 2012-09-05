using System.Collections.Generic;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;

namespace NetTopologySuite.Extensions.Coverages
{
    public class RegularGridCoverageCell : Unique<long>, IRegularGridCoverageCell
    {
        public object Clone()
        {
            return new RegularGridCoverageCell
                       {
                           X = X,
                           Y = Y,
                           regularGridCoverage = RegularGridCoverage,
                           Geometry = (IGeometry) Geometry.Clone()
                       };
        }

        private IGeometry geometry;

        public virtual IGeometry Geometry
        {
            get
            {
                if (isDirty)
                {
                    geometry = CreateCell(X, Y, regularGridCoverage.DeltaX, regularGridCoverage.DeltaY);
                    isDirty = false;
                }
                return geometry;
            }
            set { geometry = value; }
        }

        [NoNotifyPropertyChange]
        private IFeatureAttributeCollection attributes;

        [NoNotifyPropertyChange]
        public virtual IFeatureAttributeCollection Attributes { get { return attributes; } set { attributes = value; } }

        private bool isDirty;

        private double x;

        public double X
        {
            get { return x; }
            set
            {
                isDirty = true;
                x = value;
            }
        }

        private double y;

        public double Y
        {
            get { return y; }
            set
            {
                isDirty = true;
                y = value;
            }
        }

        private IRegularGridCoverage regularGridCoverage;

        public virtual IRegularGridCoverage RegularGridCoverage
        {
            get { return regularGridCoverage; }
            set
            {
                isDirty = true;
                regularGridCoverage = value;
            }
        }

        private static IGeometry CreateCell(double offsetX, double offsetY, double extentX, double extentY)
        {
            var vertices = new List<ICoordinate>
                                   {
                                       GeometryFactory.CreateCoordinate(offsetX, offsetY),
                                       GeometryFactory.CreateCoordinate(offsetX + extentX, offsetY),
                                       GeometryFactory.CreateCoordinate(offsetX + extentX, offsetY + extentY),
                                       GeometryFactory.CreateCoordinate(offsetX, offsetY + extentY)
                                   };
            vertices.Add((ICoordinate)vertices[0].Clone());
            ILinearRing newLinearRing = new LinearRing(vertices.ToArray());
            return new Polygon(newLinearRing);
        }
    }
}