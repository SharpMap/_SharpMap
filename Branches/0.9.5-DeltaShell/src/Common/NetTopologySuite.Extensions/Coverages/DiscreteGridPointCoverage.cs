using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.Index;
using GisSharpBlog.NetTopologySuite.Index.Quadtree;
using NetTopologySuite.Extensions.Features;

namespace NetTopologySuite.Extensions.Coverages
{
    /// <summary>
    /// Rename to CurvilinearGridCoverage?
    /// </summary>
    public class DiscreteGridPointCoverage : Coverage, IDiscreteGridPointCoverage
    {
        public new const string DefaultName = "new grid point coverage";

        private IVariable<double> x;
        private IVariable<double> y;
        private IVariable index1;
        private IVariable index2;
        private IMultiDimensionalArray<IGridFace> faces;

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

        public DiscreteGridPointCoverage(int size1, int size2, IEnumerable<double> x, IEnumerable<double> y)
        {
            Name = DefaultName;

            Resize(size1, size2, x, y);
        }

        public virtual void Resize(int size1, int size2, IEnumerable<double> xCoordinates, IEnumerable<double> yCoordinates)
        {
            // TODO: extend to non-memory stores

            if (Arguments.Count == 0)
            {
                Components.Add(new Variable<double>("value"));

                // created argument variables 
                index1 = new Variable<int>("index1") {FixedSize = size1, GenerateUniqueValueForDefaultValue = true};
                index2 = new Variable<int>("index2") {FixedSize = size2, GenerateUniqueValueForDefaultValue = true};

                // add arguments
                Arguments.Add(index1);
                Arguments.Add(index2);
            }

            index1.FixedSize = size1;
            index1.Values.Resize(size1);
            index2.FixedSize = size2;
            index2.Values.Resize(size2);

            if (x == null)
            {
                // setup points as a 2d variable
                x = new Variable<double>("x") {Arguments = {index1, index2}, FixedSize = size1*size2};

                // actually x, y are components: F = (value, x, y)(i, j) or even as IVariable<IPoint>
                y = new Variable<double>("y") {Arguments = {index1, index2}, FixedSize = size1*size2};
            }

            var newShape = new[] { size1, size2 };
            
            x.FixedSize = size1*size2;
            x.Values.Resize(newShape);

            y.FixedSize = size1 * size2;
            y.Values.Resize(newShape);

            foreach(var v in Components)
            {
                if (IsTimeDependent)
                {
                    v.Values.Resize(new [] {0, size1, size2});
                }
                else
                {
                    v.Values.Resize(newShape);
                }
            }
            
            if(xCoordinates != null && yCoordinates != null)
            {
                x.SetValues(xCoordinates);
                y.SetValues(yCoordinates);
            }

            faces = null; // reset
        }

        public class Face : Feature, IGridFace
        {
            public int I;
            public int J;
            public int Index;

            public bool Equals(Face other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return base.Equals(other) && other.Index == Index;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return Equals(obj as Face);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (base.GetHashCode()*397) ^ Index;
                }
            }

            public ICoverage Grid { get; set; }
        }

        public class Vertex : Feature, IGridVertex
        {
            public ICoverage Grid
            {
                get; set;
            }
        }

        public virtual IEnumerable<IFeature> GetFeatures(double xCoordinate, double yCoordinate, double searchDistance)
        {
            var point = new Point(xCoordinate, yCoordinate);

            var pointBuffer = point.Buffer(searchDistance);

            var xValues = x.Values;
            var yValues = y.Values;

            // find vertices
            for (var i = 0; i < Size1; i++)
            {
                for (var j = 0; j < Size2; j++)
                {
                    var pt = new Point(xValues[i, j], yValues[i, j]);
                    if (pointBuffer.Contains(pt))
                    {
                        yield return new Vertex { Geometry = pt, Grid = this };
                    }
                }
            }

            // find faces
            foreach (var face in Faces)
            {
                if(face.Geometry.Contains(point))
                {
                    yield return face;
                }
            }
        }

        private void GenerateFaces()
        {
            var xValues = x.Values;
            var yValues = y.Values;

            faces = new MultiDimensionalArray<IGridFace>(new [] {Size1 - 1, Size2 - 1});

            // build quad tree
            facesIndex = new Quadtree();

            for (var i = 0; i < Size1 - 1; i++)
            {
                for (var j = 0; j < Size2 - 1; j++)
                {
                    var points = new Coordinate[5];
                    points[0] = new Coordinate(xValues[i, j], yValues[i, j]);
                    points[1] = new Coordinate(xValues[i, j + 1], yValues[i, j + 1]);
                    points[2] = new Coordinate(xValues[i + 1, j + 1], yValues[i + 1, j + 1]);
                    points[3] = new Coordinate(xValues[i + 1, j], yValues[i + 1, j]);
                    points[4] = points[0];

                    var face = new Face { Geometry = new Polygon(new LinearRing(points)), I = i, J = j, Index = MultiDimensionalArrayHelper.GetIndex1d(new[] {i, j}, xValues.Stride ), Grid = this };
                    Faces[i, j] = face;
                    facesIndex.Insert(face.Geometry.EnvelopeInternal, face);
                }
            }
        }

        public override IFunction GetTimeSeries(ICoordinate coordinate)
        {
            var face = GetFaceAtCoordinate(coordinate);

            if(face != null)
            {
                var timeSeries = new TimeSeries{Parent = this};
                var component = new Variable<double>
                                    {
                                        Name = Name + string.Format(" ({0}, {1})", face.I, face.J)
                                    };
                timeSeries.Components.Add(component);
                
                var times = (IMultiDimensionalArray<DateTime>)Time.Values.Clone();
                var values = new MultiDimensionalArrayView<double>(Components[0].Values);
                values.SelectedIndexes[1] = new[]{face.I};
                values.SelectedIndexes[2] = new[]{face.J};
                timeSeries.SetValues(values, new VariableValueFilter<DateTime>(timeSeries.Time, times));
                return timeSeries;
            }

            return null;
        }

        ISpatialIndex facesIndex = new Quadtree();

        public virtual IMultiDimensionalArray<IGridFace> Faces
        {
            get
            {
                if(faces == null)
                {
                    GenerateFaces();
                }

                return faces;
            }
        }

        public override object Evaluate(ICoordinate coordinate)
        {
            return Evaluate(coordinate, null);
        }

        public override object Evaluate(ICoordinate coordinate, DateTime? time)
        {
            // check value directly at the point
            var xValues = x.Values;
            var yValues = y.Values;

            for (int i = 0; i < xValues.Count; i++)
            {
                if(xValues[i] == coordinate.X && yValues[i] == coordinate.Y)
                {
                    var index = MultiDimensionalArrayHelper.GetIndex(i, xValues.Stride);
                    if (time != null)
                    {
                        var timeIndex = Time.Values.IndexOf(time);
                        return Components[0].Values[timeIndex, index[0], index[1]];
                    }
                    else
                    {
                        return Components[0].Values[i];
                    }
                }
            }

            // otherwise face
            var face = GetFaceAtCoordinate(coordinate);

            // does not work?
            // var face = facesIndex.Query(point.EnvelopeInternal).Cast<Face>().FirstOrDefault();

            if (face != null)
            {
                if (time != null)
                {
                    var timeIndex = Time.Values.IndexOf(time);
                    return Components[0].Values[timeIndex, face.I, face.J];
                }
                else
                {
                    return Components[0].Values[face.Index];
                }
            }

            return null;
        }

        private Face GetFaceAtCoordinate(ICoordinate coordinate)
        {
            var point = new Point(coordinate);

            if(faces == null)
            {
                return null;
            }

            var face = (Face) faces.FirstOrDefault(f => f.Geometry.EnvelopeInternal.Contains(point.Coordinate));
            return face;
        }

        public override T Evaluate<T>(ICoordinate coordinate)
        {
            return Evaluate<T>(coordinate.X, coordinate.Y);
        }

        public override T Evaluate<T>(double x, double y)
        {
            return (T) Evaluate(new Coordinate(x, y));
        }

        public virtual IVariable Index1
        {
            get { return index1; }
            set { index1 = value; }
        }

        public virtual IVariable Index2
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

        public virtual int Size1
        {
            get
            {
                if (Index1 == null)
                {
                    return 0;
                }

                if (Index1.FixedSize != 0)
                {
                    return Index1.FixedSize;
                }

                return Index1.Values.Count;
            }
        }

        public virtual int Size2
        {
            get
            {
                if(Index2 == null)
                {
                    return 0;
                }

                if (Index2.FixedSize != 0)
                {
                    return Index2.FixedSize;
                }

                return Index2.Values.Count;
            }
        }
    }
}