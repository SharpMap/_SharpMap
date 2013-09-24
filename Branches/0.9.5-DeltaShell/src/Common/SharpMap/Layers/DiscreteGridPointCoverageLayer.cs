using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using SharpMap.Api;
using SharpMap.Editors;
using SharpMap.Rendering;
using log4net;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

namespace SharpMap.Layers
{
    public class DiscreteGridPointCoverageLayer : Layer, ICoverageLayer, ITimeNavigatable
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
            showLines = true;

            Name = "Grid Points";
            Visible = true;
            
            pen = Pens.LightGreen;

            FeatureEditor = new FeatureEditor(); // default feature editor
        }

        private bool showFaces;
        public virtual bool ShowFaces 
        { 
            get
            {
                return showFaces;
            } 
            set 
            { 
                showFaces = value;
                RenderRequired = true;
            } 
        }

        private bool showVertices;
        public virtual bool ShowVertices
        {
            get
            {
                return showVertices;
            }
            set 
            { 
                showVertices = value;
                RenderRequired = true;
            }
        }

        private bool showLines;
        public virtual bool ShowLines
        {
            get
            {
                return showLines;
            }
            set 
            { 
                showLines = value;
                RenderRequired = true;
            }
        }

        private bool updateTheme;

        public virtual ICoverage Coverage
        {
            get { return coverage; }
            set
            {
                coverage = (IDiscreteGridPointCoverage) value;

                if (coverage.IsTimeDependent && coverage.Time.Values.Count > 0)
                {
                    currentTime = coverage.Time.Values[0];
                }

                if (coverage.Size1 != 0 && coverage.Size2 != 0 && (coverage.Size1 < 3 || coverage.Size2 < 3))
                {
                    showVertices = true;
                    showLines = false;
                }

                if (coverage.Size1 > 1 && coverage.Size2 > 1 && coverage.Size1 * coverage.Size2 < 50000)
                {
                    showFaces = true;
                }
                
                RefreshValuesAndCoordinates();

                coverage.ValuesChanged += coverage_ValuesChanged;

                RenderRequired = true;
            }
        }

        public virtual IComparable MinValue
        {
            get
            {
                if (coverage != null && coverage.Components[0].Values.Count > 0)
                {
                    return (IComparable) coverage.Components[0].MinValue;
                }
                return null;
            }
        }

        public virtual IComparable MaxValue
        {
            get
            {
                if (coverage != null && coverage.Components[0].Values.Count > 0)
                {
                    return (IComparable)coverage.Components[0].MaxValue;
                }
                return null;
            }
        }

        void coverage_ValuesChanged(object sender, DelftTools.Functions.FunctionValuesChangingEventArgs e)
        {
            updateTheme = true;
            RenderRequired = true;
        }

        private void RefreshValuesAndCoordinates()
        {
            log.Debug("Cashing values ...");

            valuesVariable = GetCoverageValuesVariable();

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

        private IVariable<double> GetCoverageValuesVariable()
        {
            if (coverage.IsTimeDependent && coverage.Time.Values.Count > 0)
            {
                return (IVariable<double>) coverage.Components[0].Filter(new VariableValueFilter<DateTime>(coverage.Time, currentTime), new VariableReduceFilter(coverage.Time));
            }
            else
            {
                return ((IVariable<double>) coverage.Components[0]);
            }
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
            var variableValues = GetCoverageValuesVariable().Values;

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
            if(Theme != null)
            {
                if(Theme is GradientTheme)
                {
                    var gradientTheme = theme as GradientTheme;

                    if (gradientTheme != null)
                    {
                        var variable = Coverage.Components[0];
                        if (variable != null)
                        {
                            gradientTheme.Min = (double) variable.MinValue;
                            gradientTheme.Max = (double) variable.MaxValue;
                            gradientTheme.UpdateThemeItems();
                        }
                    }
                }

                return;
            }
            var minVectorStyle = new VectorStyle();
            var maxVectorStyle = new VectorStyle();
            minVectorStyle.Fill = new SolidBrush(Color.MediumSeaGreen);
            maxVectorStyle.Fill = new SolidBrush(Color.DarkBlue);

            if(values.Count == 0)
            {
                return;
            }

            AutoUpdateThemeOnDataSourceChanged = true;

            Theme = new GradientTheme("Green to Blue", values.Min(), values.Max(), minVectorStyle, maxVectorStyle, null, null, null);
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

        public override void OnRender(Graphics g, IMap map)
        {
            // TODO: render is called twice after time selection change - find out why
            /*
            if(!RenderRequired)
            {
                return;
            }
            */

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

            if(updateTheme)
            {
                RefreshValuesAndCoordinates();
                updateTheme = false;
            }
            
            if(coverage.IsTimeDependent)
            {
                if (curentTimeChanged)
                {
                    RefreshValuesAndCoordinates();
                    curentTimeChanged = false;
                }
            }

            if (ShowFaces)
            {
                Pen pen = null;

                if(ShowLines)
                {
                    pen = new Pen(Color.DarkGray, 0.05f);
                }

                var brush = new SolidBrush(Color.White);
                var values2D = GetCoverageValuesVariable().GetValues<double>();
                for (var i = 0; i < coverage.Size1 - 1; i++)
                {
                    for (var j = 0; j < coverage.Size2 - 1; j++)
                    {
                        brush.Color = Theme.GetFillColor(values2D[i, j]);
                        var face = coverage.Faces[i, j];
                        VectorRenderingHelper.DrawPolygon(g, (IPolygon) face.Geometry, brush, pen, false, map);
                    }
                }
                brush.Dispose();

                if(pen != null)
                {
                    pen.Dispose();
                }
            }
            else if (ShowLines)
            {
                var xValues = coverage.X.Values;
                var yValues = coverage.Y.Values;
                
                using (pen = new Pen(Color.DarkGray, 0.05f))
                {
                    for (var i = 0; i < coverage.Size1; i++)
                    {
                        for (var j = 0; j < coverage.Size2; j++)
                        {
                            if (i != coverage.Size1 - 1)
                            {
                                var line = new LineString(new ICoordinate[]
                                {
                                    new Coordinate(xValues[i, j], yValues[i, j]),
                                    new Coordinate(xValues[i + 1, j], yValues[i + 1, j]),
                                });
                                VectorRenderingHelper.DrawLineString(g, line, pen, map);
                            }

                            if (j != coverage.Size2 - 1)
                            {
                                var line = new LineString(new ICoordinate[] 
                                {
                                    new Coordinate(xValues[i, j], yValues[i, j]),
                                    new Coordinate(xValues[i, j + 1], yValues[i, j + 1]),
                                });
                                VectorRenderingHelper.DrawLineString(g, line, pen, map);
                            }
                        }
                    }
                }
            }
            
            if (ShowVertices)
            {
                RenderPointsAndValues(g, envelope, map);
            }
        }

        private bool curentTimeChanged;

        private void RenderPointsAndValues(Graphics g, IEnvelope envelope, IMap map)
        {
            var t = DateTime.Now;

            for (int i = 0; i < values.Count; i++)
            {
                var localCoordinate = Utilities.Transform.WorldtoMap(new Coordinate(x[i], y[i]), map);

                using (var brush = new SolidBrush(Theme.GetFillColor(values[i])))
                {
                    g.FillEllipse(brush, localCoordinate.X - 6, localCoordinate.Y - 6, 12, 12);
                    //g.DrawString(i.ToString(), new Font(FontFamily.GenericMonospace, 10), Brushes.Black, localCoordinate.X, localCoordinate.Y);
                }
            }
        }

        public virtual DateTime? TimeSelectionStart
        {
            get; internal protected set;
        }

        public virtual DateTime? TimeSelectionEnd
        {
            get; internal protected set;
        }

        public virtual TimeNavigatableLabelFormatProvider CustomDateTimeFormatProvider
        {
            get { return null; }
        }

        public virtual void SetCurrentTimeSelection(DateTime? start, DateTime? end)
        {
            TimeSelectionStart = start;
            TimeSelectionEnd = end;

            currentTime = start.Value;
            curentTimeChanged = true;

            if(CurrentTimeSelectionChanged != null)
            {
                CurrentTimeSelectionChanged();
            }

            RenderRequired = true;
        }

        public virtual event Action CurrentTimeSelectionChanged;

        public virtual IEnumerable<DateTime> Times
        {
            get { return coverage.IsTimeDependent ? coverage.Time.Values : Enumerable.Empty<DateTime>(); }
        }

        public virtual event Action TimesChanged;

        public virtual TimeSelectionMode SelectionMode
        {
            get { return TimeSelectionMode.Single; }
        }

        public virtual SnappingMode SnappingMode
        {
            get { return SnappingMode.Nearest; }
        }
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
