using System;
using System.Collections;
using System.Linq;
using System.Windows.Forms;
using DelftTools.Utils;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Layers;

namespace SharpMap.UI.Tools
{
    public class QueryTool : MapTool
    {
        private readonly ToolTip tooltipControl = new ToolTip
            {
                AutomaticDelay = 1000,
                ToolTipIcon = ToolTipIcon.Info
            };

        public QueryTool()
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

            var allLayers = Map.GetAllVisibleLayers(true);
            foreach (var layer in allLayers)
            {
                if (!(layer is ICoverageLayer) || layer.Envelope == null)
                    continue;

                var envelope = (IEnvelope) layer.Envelope.Clone();
                    envelope.ExpandBy(layer.Envelope.Width*0.1); // 10% margin

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
                string valueString = GetValueString(value);
                if ((coverage is IRegularGridCoverage) && (coverage.Components.Count > 0))
                {
                    if (coverage.Components.Count > 1) throw new NotImplementedException("Query tool for multi-component grid-coverages not implemented");

                    if (coverage.Components[0].NoDataValues.Contains(value))
                    {
                        valueString = "No data";
                    }
                }
                layerValues += layer.Name + " : " + valueString + "\n";
            }

            return layerValues;
        }

        private static string GetValueString(object value)
        {
            var valueString = "";

            var enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                valueString = enumerable.Cast<object>().Aggregate(valueString, (current, valueItem) => current + (valueItem + ","));

                valueString = valueString == "" ? "<empty>" : valueString.TrimEnd(',');
            }
            else
            {
                valueString = (value ?? "<empty>").ToString();
            }

            return valueString;
        }

        private void SetEvaluationTolerance(ICoverage coverage)
        {
            var featureCoverage1 = coverage as FeatureCoverage;
            if (featureCoverage1 != null)
            {
                var tolerance = Map.PixelSize * 5;
                var featureCoverage = featureCoverage1;
                if (featureCoverage.EvaluateTolerance != tolerance)
                {
                    featureCoverage.EvaluateTolerance = tolerance;
                }
            }

            var networkCoverage = coverage as NetworkCoverage;
            if (networkCoverage != null)
            {
                var tolerance = Map.PixelSize * 20;
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