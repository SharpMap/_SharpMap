using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Drawing;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.Index;
using GisSharpBlog.NetTopologySuite.Index.Strtree;
using log4net;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

namespace SharpMap.Layers
{
    public class DiscreteGridPointCoverageLayer : VectorLayer, ICoverageLayer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DiscreteGridPointCoverageLayer)); 
        
        private IDiscreteGridPointCoverage coverage;
        private IVariable<double> valuesVariable;

        private IList<double> x;
        private IList<double> y;
        private IList<double> values;
        private IList<int> valueIndices;
        private DateTime currentTime;

        private IEnvelope envelope;

        private static Pen pen;
        
        private double? noDataValue;

        public DiscreteGridPointCoverageLayer()
        {
            Name = "Grid Points";
            Enabled = true;
            Style = new VectorStyle { GeometryType = typeof(IPoint), Shape = ShapeType.Ellipse, ShapeSize = 4 };

            pen = Pens.LightGreen;
        }

        public virtual ICoverage Coverage
        {
            get { return coverage; }
            set
            {
                coverage = (IDiscreteGridPointCoverage) value;
                
                RefreshValuesAndCoordinates();
                
                if (coverage.IsTimeDependent)
                {
                    currentTime = coverage.Time.Values[0];
                }
            }
        }

        private void RefreshValuesAndCoordinates()
        {
            log.Debug("Cashing values ...");
            valuesVariable = ((IVariable<double>)coverage.Components[0]);

            if (valuesVariable.NoDataValues.Count > 0)
            {
                noDataValue = valuesVariable.NoDataValues[0];
            }

            valueIndices = GetIndicesWhereValuesExist().ToList(); // cache indices
            values = GetVariableValuesByIndex(valuesVariable, valueIndices).ToList(); // cache values

            log.Debug("Caching coordinates ...");
            envelope = new Envelope();
            x = GetVariableValuesByIndex(coverage.X, valueIndices).ToList();
            y = GetVariableValuesByIndex(coverage.Y, valueIndices).ToList();
            for (var i = 0; i < x.Count; i++)
            {
                envelope.ExpandToInclude(x[i], y[i]);
            }

            log.Debug("Creating default theme ...");
            GenerateTheme();
        }

        private IEnumerable<T> GetVariableValuesByIndex<T>(IVariable<T> variable, IEnumerable<int> indices) where T : IComparable
        {
            var variableValues = variable.Values;
            foreach (var index in indices)
            {
                yield return variableValues[index];
            }
        }

        private IEnumerable<int> GetIndicesWhereValuesExist()
        {
            var variableValues = ((IVariable<double>)coverage.Components[0]).Values;
            for (int i = 0; i < variableValues.Count; i++)
            {
                if (noDataValue != null && variableValues[i] == noDataValue.Value)
                {
                    continue;
                }

                yield return i;
            }
        }

        private void GenerateTheme()
        {
            var minVectorStyle = new VectorStyle();
            var maxVectorStyle = new VectorStyle();
            minVectorStyle.Fill = new SolidBrush(Color.Red);
            maxVectorStyle.Fill = new SolidBrush(Color.Blue);

            Theme = new GradientTheme("Red to Blue", values.Min(), values.Max(), minVectorStyle, maxVectorStyle, null, null, null);
        }

        public override object Clone()
        {
            var clone = (DiscreteGridPointCoverageLayer) base.Clone();
            clone.Coverage = Coverage;
            return clone;
        }

        public override IEnvelope Envelope
        {
            get { return envelope; }
        }

        public override void OnRender(Graphics g, Map map)
        {
            if (map.Center == null)
            {
                throw (new ApplicationException("Cannot render map. View center not specified"));
            }

            if (g == null)
            {
                return;
            }

            // configure graphics to maximum speed
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;

            // refresh cached values if grid is time dependent
            if(coverage.IsTimeDependent)
            {
                var filterTime = coverage.Filters.OfType<VariableValueFilter<DateTime>>().First().Values[0];
                if (currentTime != filterTime)
                {
                    currentTime = filterTime;
                    RefreshValuesAndCoordinates();
                }
            }

            RenderPointsAndValues(g, envelope, map);
        }

        private void RenderPointsAndValues(Graphics g, IEnvelope envelope, Map map)
        {
            var t = DateTime.Now;

            for (int i = 0; i < values.Count; i++)
            {
                var localCoordinate = Utilities.Transform.WorldtoMap(new Coordinate(x[i], y[i]), map);

                using (var brush = new SolidBrush(Theme.GetFillColor(values[i])))
                {
                    g.FillEllipse(brush, localCoordinate.X, localCoordinate.Y, 4, 4);
                }
            }

            SetRenderingTimeParameters(1, valueIndices.Count(), DateTime.Now.Subtract(t).TotalMilliseconds);
        }

        public override int SRID { get; set; }
    }
}























// get geometry

/*
                var point = pointsExceptNoData[i];
                var currentGeometry = CoordinateTransformation != null
                                                ? GeometryTransform.TransformGeometry(point, CoordinateTransformation.MathTransform)
                                                : point;

                var currentVectorStyle = isThemeUsed
                                ? Theme.GetStyle(valuesExceptNoData[i]) as VectorStyle
                                : Style;

                VectorRenderingHelper.RenderGeometry(graphics, map, currentGeometry, currentVectorStyle, DefaultPointSymbol, ClippingEnabled);
*/





/*
 * // spatial index
                var index1D = 0;
                for (var i = 0; i < points.Shape[0]; i++)
                {
                    for (var j = 0; j < points.Shape[1]; j++)
                    {
                        index.Insert(points[i, j].EnvelopeInternal, new PointAndIndex { Point = points[i, j], Index1D = index1D});
                        index1D++;
                    }
                }
*/
/*
private class PointAndIndex
{
    public IPoint Point { get; set; }
    public int Index1D { get; set; }
}
        private ISpatialIndex index;
        

            index = new STRtree();
*/
