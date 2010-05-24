using System;
using System.Net;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using SharpMap.Geometries;
using ProjNet.CoordinateSystems.Transformations;
using System.Collections;
using System.Collections.Generic;

namespace SharpMap.Projection
{
    public static class ProjectionHelper
    {
        public static IGeometry Transform(IGeometry geometry, ICoordinateTransformation CoordinateTransformation)
        {
            if (geometry is Point)
            {
                double[] point = CoordinateTransformation.MathTransform.Transform(new double[] { ((Point)geometry).X, ((Point)geometry).Y });
                return new Point(point[0], point[1]);
            }
            else
            {
                throw new NotImplementedException("todo implement for other geometries");
            }
        }

        public static IGeometry InverseTransform(IGeometry geometry, ICoordinateTransformation CoordinateTransformation)
        {
            if (geometry is Point)
            {
                CoordinateTransformation.MathTransform.Inverse();
                double[] point = CoordinateTransformation.MathTransform.Transform(new double[] { ((Point)geometry).X, ((Point)geometry).Y });
                CoordinateTransformation.MathTransform.Inverse();
                return new Point(point[0], point[1]);
            }
            else
            {
                throw new NotImplementedException("todo implement for other geometries");
            }
        }

        public static BoundingBox Transform(BoundingBox box, ICoordinateTransformation CoordinateTransformation)
        {
            double[] point1 = CoordinateTransformation.MathTransform.Transform(new double[] { box.MinX, box.MinY });
            double[] point2 = CoordinateTransformation.MathTransform.Transform(new double[] { box.MaxX, box.MaxY });
            return new BoundingBox(point1[0], point1[1], point2[0], point2[1]);
        }

        public static BoundingBox InverseTransform(BoundingBox box, ICoordinateTransformation CoordinateTransformation)
        {
            CoordinateTransformation.MathTransform.Invert();
            double[] point1 = CoordinateTransformation.MathTransform.Transform(new double[] { box.MinX, box.MinY });
            double[] point2 = CoordinateTransformation.MathTransform.Transform(new double[] { box.MaxX, box.MaxY });
            CoordinateTransformation.MathTransform.Invert();
            return new BoundingBox(point1[0], point1[1], point2[0], point2[1]);
        }
    }
}
