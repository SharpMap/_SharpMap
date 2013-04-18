using System;
using System.Collections;
using System.Linq;
using System.Windows.Forms;
using DelftTools.Functions.Filters;
using DelftTools.Utils;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Layers;
using SharpMap.UI.Forms;

namespace SharpMap.UI.Tools
{
    public class QueryTool : MapTool
    {
        private readonly ToolTip tooltipControl = new ToolTip()
                                             {
                                                 AutomaticDelay = 1000,
                                                 ToolTipIcon = ToolTipIcon.Info
                                             };

        public QueryTool(MapControl mapControl): base(mapControl)
        {
            Name = "Query";
        }

        /// <summary>
        /// Use this property to enable or disable tool. When the measure tool is deactivated, it cleans up old measurements.
        /// </summary>
        public override bool IsActive
        {
            get { return base.IsActive; }
            set
            {
                base.IsActive = value;

                if (!IsActive)
                    Clear();
            }
        }

        private void Clear()
        {
            tooltipControl.SetToolTip((Control) MapControl, "");
        }

        private string GetCoverageValues(ICoordinate worldPosition)
        {
            if (!IsActive || worldPosition == null)
                return "";

            var layerValues = "";
            ICoordinate coordinate = new Coordinate(worldPosition.X, worldPosition.Y);

            var allLayers = Map.GetAllLayers(true).Where(l => l.Visible);
            foreach (var layer in allLayers)
            {
                if (!(layer is ICoverageLayer))
                    continue;

                var envelope = (IEnvelope)layer.Envelope.Clone();
                envelope.ExpandBy(layer.Envelope.Width*0.1); //10% margin

                if (!envelope.Contains(coordinate)) 
                    continue;

                var coverage = ((ICoverageLayer)layer).Coverage;

                if (coverage.IsTimeDependent)
                {
                    if (layer is ITimeNavigatable)
                    {
                        var timeNav = (layer as ITimeNavigatable);
                        if (timeNav.TimeSelectionStart.HasValue)
                        {
                            var time = timeNav.TimeSelectionStart.Value;
                            coverage = coverage.FilterTime(time);
                        }
                    }
                    else
                    {
                        continue; //nothing to filter on
                    }
                }

                SetEvaluationTolerance(coverage);

                var value = coverage.Evaluate(coordinate);
                layerValues += layer.Name + " : " + GetValueString(value) + "\n";
            }

            return layerValues;
        }

        private static string GetValueString(object value)
        {
            var valueString = "";

            if (value is IEnumerable)
            {
                foreach (var valueItem in value as IEnumerable)
                {
                    valueString += valueItem + ",";
                }

                valueString = (valueString == "") ? "<empty>" : valueString.TrimEnd(',');
            }
            else
            {
                valueString = (value?? "<empty>").ToString();
            }

            return valueString;
        }

        private void SetEvaluationTolerance(ICoverage coverage)
        {

            if (coverage is FeatureCoverage)
            {
                var tolerance = Map.PixelSize * 5;
                var featureCoverage = ((FeatureCoverage) coverage);
                if (featureCoverage.EvaluateTolerance != tolerance)
                {
                    featureCoverage.EvaluateTolerance = tolerance;
                }
            }
            if (coverage is NetworkCoverage)
            {
                var tolerance = Map.PixelSize * 20;
                var networkCoverage = (NetworkCoverage) coverage;
                if (networkCoverage.EvaluateTolerance != tolerance)
                {
                    networkCoverage.EvaluateTolerance = tolerance;
                }
            }
        }

        private System.Drawing.Point previousLocation;
        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (previousLocation.X == e.Location.X && previousLocation.Y == e.Location.Y)
            {
                return;
            }
            
            previousLocation = e.Location;
            tooltipControl.SetToolTip((Control)MapControl, GetCoverageValues(worldPosition));
        }
    }
}