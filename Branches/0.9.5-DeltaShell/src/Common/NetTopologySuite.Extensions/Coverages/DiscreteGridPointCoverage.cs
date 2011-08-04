using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;

namespace NetTopologySuite.Extensions.Coverages
{
    public class DiscreteGridPointCoverage : Coverage, IDiscreteGridPointCoverage
    {
        public new const string DefaultName = "new grid point coverage";

        private IVariable<double> x;
        private IVariable<double> y;
        private IVariable index1;
        private IVariable index2;

        public DiscreteGridPointCoverage()
        {
            Name = DefaultName;

            Resize(0, 0, null, null);
        }

        public DiscreteGridPointCoverage(int size1, int size2, IEnumerable<IPoint> points)
        {
            Name = DefaultName;

            var xCoordinates = points.Select(pt => pt.X);
            var yCoordinates = points.Select(pt => pt.Y);
            
            Resize(size1, size2, xCoordinates, yCoordinates);
        }

        public virtual void Resize(int size1, int size2, IEnumerable<double> xCoordinates, IEnumerable<double> yCoordinates)
        {
            Components.Clear();
            Arguments.Clear();

            Components.Add(new Variable<double>("value"));

            // created argument variables 
            index1 = new Variable<int>("index1") { FixedSize = size1 };
            index2 = new Variable<int>("index2") { FixedSize = size2};

            index1.Values.Resize(size1);
            index2.Values.Resize(size2);

            // add arguments
            Arguments.Add(index1);
            Arguments.Add(index2);

            // setup points as a 2d variable
            x = new Variable<double>("x") { Arguments = { index1, index2 }, FixedSize = size1 * size2 }; // actually x, y are components: F = (value, x, y)(i, j) or even as IVariable<IPoint>
            y = new Variable<double>("y") { Arguments = { index1, index2 }, FixedSize = size1 * size2 };
            
            if(xCoordinates != null && yCoordinates != null)
            {
                x.SetValues(xCoordinates);
                y.SetValues(yCoordinates);
            }
        }

        public override object Evaluate(ICoordinate coordinate)
        {
            // TODO: add spatial indexing

            var xValues = x.Values;
            var yValues = y.Values;
            for (var i = 0; i < xValues.Count; i++)
            {
                if (xValues[i] != coordinate.X || yValues[i] != coordinate.Y)
                {
                    continue;
                }

                var index = MultiDimensionalArrayHelper.GetIndex(i, X.Values.Stride);
                return Components[0].Values[index];
            }

            return null;
        }

        public override T Evaluate<T>(ICoordinate coordinate)
        {
            return Evaluate<T>(coordinate.X, coordinate.Y);
        }

        public override T Evaluate<T>(double x, double y)
        {
            return (T) Evaluate(new Coordinate(x, y));
        }

        public IVariable Index1
        {
            get { return index1; }
            set { index1 = value; }
        }

        public IVariable Index2
        {
            get { return index2; }
            set { index2 = value; }
        }

        public virtual IVariable<double> X
        {
            get { return x; }
            set { x = value; }
        }

        public virtual IVariable<double> Y
        {
            get { return y; }
            set { y = value; }
        }

        public int Size1
        {
            get { return Index1 != null ? Index1.Values.Count : -1; }
        }

        public int Size2
        {
            get { return Index2 != null ? Index2.Values.Count : -1; }
        }
    }
}