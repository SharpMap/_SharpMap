using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Collections;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using SharpMap.Utilities;
using ColorBlend = System.Drawing.Drawing2D.ColorBlend;

namespace SharpMap.Rendering
{
    public class NetworkCoverageSegmentRenderer : NetworkCoverageRenderer
    {
        public override bool Render(IFeature feature, Graphics g, ILayer layer)
        {
            var segmentsLayer = (NetworkCoverageSegmentLayer)layer;
            var coverage = ((NetworkCoverageFeatureCollection)layer.DataSource).RenderedCoverage;

            if (coverage == null || coverage.Network == null)
            {
                return true;
            }

            var interpolationType = coverage.Parent == null
                                        ? coverage.Locations.InterpolationType
                                        : ((NetworkCoverage) coverage.Parent).Locations.InterpolationType;

            var segmentGenerationMethod = coverage.Parent == null
                                        ? coverage.SegmentGenerationMethod
                                        : ((NetworkCoverage)coverage.Parent).SegmentGenerationMethod;

           
            var theme = segmentsLayer.Theme;
            var defaultStyle = (theme != null)
                                   ? (VectorStyle)theme.GetStyle(coverage.DefaultValue)
                                   : segmentsLayer.Style;

            var mapExtents = layer.Map.Envelope;
            var drawnBranches = new HashSet<IBranch>();

            if ((interpolationType == InterpolationType.None) && (coverage.Locations.ExtrapolationType == ExtrapolationType.None))
            {
                // no interpolation and extrapolation; only locations are visible
                RenderSegmentFreeBranches(layer.Map, coverage, g, defaultStyle, mapExtents, drawnBranches, theme);
                return true;
            }

            if (segmentGenerationMethod == SegmentGenerationMethod.RouteBetweenLocations)
            {
                RenderConstant(g, layer);
                RenderMissingSegments(g, layer);
                return true;
            }

            if (segmentGenerationMethod == SegmentGenerationMethod.SegmentBetweenLocations || 
                segmentGenerationMethod == SegmentGenerationMethod.SegmentBetweenLocationsFullyCovered)
            {
                if (interpolationType == InterpolationType.None)
                {
                    return true;
                }

                if (interpolationType == InterpolationType.Linear)
                {
                    throw new NotSupportedException("Linear interpolation for segments between locations is not yet supported");
                }

                RenderConstantBetweenLocations(g, layer, segmentGenerationMethod == SegmentGenerationMethod.SegmentBetweenLocationsFullyCovered);
                RenderMissingSegments(g, layer);
                return true;
            }

            if (coverage.Locations.Values.Count != coverage.Segments.Values.Count)
            {
                return true;
            }

            //don't render timedependent coverages with no times...evaluate will cause an exception (which is correct?)
            if (!coverage.IsTimeDependent || coverage.Time.AllValues.Count != 0)
            {
                var branchesToDraw = coverage.Network.Branches.Where(b => b.Geometry.EnvelopeInternal.Intersects(mapExtents));

                var locationsToSegmentDictonary = coverage.Locations.Values.Zip(coverage.Segments.Values).ToDictionary(t => t.First, t => t.Second);

                foreach (IBranch branch in branchesToDraw)
                {
                    if (RenderBranchSegments(branch, (NetworkCoverageSegmentLayer)layer, g, coverage, locationsToSegmentDictonary))
                    {
                        drawnBranches.Add(branch);
                    }
                }
            }

            // draw branches that are visible but without segment and thus not yet drawn.
            RenderSegmentFreeBranches(layer.Map, coverage, g, defaultStyle, mapExtents, drawnBranches, theme);

            return true;
        }

        // Evaluate value at start and end of segment and interpolate the colors based on colors from theme.
        // The 4 dictinctive cases below should be properly handled by coverage.Evaluate
        //A    0         8                            0
        // [       ][                  ][                  ]
        // interpolate - linear
        // extrapolate - constant 
        // 0000012334566788877776665555444333322211110000000

        //B    0         8                            0
        // [       ][                  ][                  ]
        // interpolate - linear
        // extrapolate - linear
        // -10001233456678887777666555544433332221111000-1-1

        //C    0         8                            0
        // [       ][                  ][                  ]
        // interpolate - linear
        // extrapolate - none
        // ddd00123345667888777766655554443333222111100ddddd where d is default value for coverage

        //D0             8                                 0
        // [      ][                       ][              ]
        // interpolate - linear
        // extrapolate - n.a.
        // 0011233455667888777766565555444433332222111110000

        // no interpolation; only locations are visible
        //     0         8                            0
        // [       ][                  ][                  ]
        // interpolate - constant
        // 0000000008888888888888888888800000000000000000000

        // 0             8                                 0
        // [      ][                       ][              ]
        // interpolate - constant
        // 0000000088888888888888888888888880000000000000000
        private static bool RenderBranchSegments(IBranch branch, VectorLayer segmentsLayer, Graphics graphics, INetworkCoverage coverage, Dictionary<INetworkLocation, INetworkSegment> locationsToSegmentDictonary)
        {
            var knownBranchLocations = coverage.GetLocationsForBranch(branch);

            if (knownBranchLocations.Count == 0) //nothing to draw
                return false;

            var theme = segmentsLayer.Theme;
            var defaultStyle = (theme != null) ? (VectorStyle)theme.GetStyle(coverage.DefaultValue) : segmentsLayer.Style;
            
            var first = true;
            var allBranchLocations = new List<INetworkLocation>();
            var branchSegments = new List<INetworkSegment>();

            foreach (var location in knownBranchLocations)
            {
                if (!locationsToSegmentDictonary.Keys.Contains(location))
                {
                    continue;
                }

                var segment = locationsToSegmentDictonary[location];
                branchSegments.Add(segment);

                if (first)
                {
                    allBranchLocations.Add(new NetworkLocation(segment.Branch, segment.Offset));
                }

                allBranchLocations.Add(location);

                allBranchLocations.Add(new NetworkLocation(segment.Branch, segment.Offset + segment.Length));

                first = false;
            }

            var allBranchLocationValues = coverage.EvaluateWithinBranch(allBranchLocations);

            for (var i = 0; i < branchSegments.Count; i++)
            {
                var firstSegment = (i == 0);
                var lastSegment = (i == branchSegments.Count - 1);

                var segment = branchSegments[i];

                var offset = knownBranchLocations[i].Offset;

                DrawSegment(segmentsLayer, coverage, theme, i, allBranchLocationValues, firstSegment, lastSegment, graphics, segment, defaultStyle, offset);
            }

            return true;
        }

        private static void DrawSegment(VectorLayer segmentsLayer, INetworkCoverage coverage, ITheme theme, int segmentNumber, IList<double> allBranchLocationValues, bool firstSegment, bool lastSegment, Graphics graphics, INetworkSegment segment, VectorStyle defaultStyle, double offset)
        {
            var valueAtStart = allBranchLocationValues[segmentNumber * 2];
            var value = allBranchLocationValues[segmentNumber * 2 + 1];
            var valueAtEnd = allBranchLocationValues[segmentNumber * 2 + 2];

            // extract based on valueAtStart and valueAtEnd the colors from the 
            var styleStart = (theme != null) ? (VectorStyle)theme.GetStyle(valueAtStart) : segmentsLayer.Style;
            var themeStyle = (theme != null) ? (VectorStyle)theme.GetStyle(value) : segmentsLayer.Style;
            var styleEnd = (theme != null) ? (VectorStyle)theme.GetStyle(valueAtEnd) : segmentsLayer.Style;

            if (firstSegment && lastSegment)
            {
                // 1 segment; render segement based on coverage.Locations.InterpolationType
                if (coverage.Locations.ExtrapolationType == ExtrapolationType.None)
                {
                    VectorRenderingHelper.RenderGeometry(graphics, segmentsLayer.Map, segment.Geometry, defaultStyle, null,
                                                         true);
                    return;
                }
                if (coverage.Locations.ExtrapolationType == ExtrapolationType.Linear)
                {
                    VectorRenderingHelper.RenderGeometry(graphics, segmentsLayer.Map, segment.Geometry, themeStyle, null, true);
                    return;
                }
                // todo use proper colors/styles from Theme; now 'via' styles are ignored.
                var kcolors = new[]
                                  {
                                      ((SolidBrush) styleStart.Fill).Color, ((SolidBrush) themeStyle.Fill).Color,
                                      ((SolidBrush) styleEnd.Fill).Color
                                  };
                var kpositions = new[] { 0.0F, (float)((offset - segment.Offset) / segment.Length), 1.0F };
                DrawStrokesLinear(graphics, Transform.TransformToImage((ILineString)segment.Geometry, segmentsLayer.Map),
                                  (int)themeStyle.Line.Width, kcolors, kpositions);
                return;
            }

            var positions = new[]
                                {
                                    0.0F, (float) ((offset - segment.Offset)/segment.Length),
                                    (float) ((offset - segment.Offset)/segment.Length), 1.0F
                                };

            var colors = CreateBeginEndColors(coverage, firstSegment, lastSegment, GetStyleColor(themeStyle), GetStyleColor(styleStart), GetStyleColor(styleEnd), GetStyleColor(defaultStyle));

            // todo use proper colors/styles from Theme; now 'via' styles are ignored.
            if (!segment.Geometry.IsEmpty)
                DrawStrokesLinear(graphics, Transform.TransformToImage((ILineString)segment.Geometry, segmentsLayer.Map),
                                  (int)themeStyle.Line.Width, colors, positions);
        }

        private static Color GetStyleColor(VectorStyle style)
        {
            return ((SolidBrush)style.Fill).Color;
        }

        private static Color[] CreateBeginEndColors(INetworkCoverage coverage, bool firstSegment, bool lastSegment, Color themeColor, Color startColor, Color endColor, Color defaultColor)
        {
            var colors = new Color[4];

            var extrapolationType = coverage.Locations.ExtrapolationType;
            var interpolationType = coverage.Locations.InterpolationType;

            if (firstSegment)
            {
                switch (extrapolationType)
                {
                    case ExtrapolationType.None:
                        colors[0] = defaultColor;
                        colors[1] = defaultColor;
                        break;
                    case ExtrapolationType.Constant:
                        colors[0] = themeColor;
                        colors[1] = themeColor;
                        break;
                    default:
                        colors[0] = startColor;
                        colors[1] = themeColor;
                        break;
                }
            }
            else
            {
                switch (interpolationType)
                {
                    case InterpolationType.None:
                        colors[0] = defaultColor;
                        colors[1] = defaultColor;
                        break;
                    case InterpolationType.Constant:
                        colors[0] = themeColor;
                        colors[1] = themeColor;
                        break;
                    default:
                        colors[0] = startColor;
                        colors[1] = themeColor;
                        break;
                }
            }

            if (lastSegment)
            {
                switch (extrapolationType)
                {
                    case ExtrapolationType.None:
                        colors[2] = defaultColor;
                        colors[3] = defaultColor;
                        break;
                    case ExtrapolationType.Constant:
                        colors[2] = themeColor;
                        colors[3] = themeColor;
                        break;
                    default:
                        colors[2] = themeColor;
                        colors[3] = endColor;
                        break;
                }
            }
            else
            {
                switch (interpolationType)
                {
                    case InterpolationType.None:
                        colors[2] = defaultColor;
                        colors[3] = defaultColor;
                        break;
                    case InterpolationType.Constant:
                        colors[2] = themeColor;
                        colors[3] = themeColor;
                        break;
                    default:
                        colors[2] = themeColor;
                        colors[3] = endColor;
                        break;
                }
            }

            return colors;
        }

        //Why ask the renderer when you know already?
        public override IList GetFeatures(IEnvelope box, ILayer layer)
        {
            var coverage = (INetworkCoverage)((NetworkCoverageSegmentLayer)layer).Coverage;
            return coverage.Segments.Values.Where(networkLocation => networkLocation.Geometry.EnvelopeInternal.Intersects(box)).ToList();
        }

        public static int GetStartIndex(float[] positions, float position)
        {
            var i = 0;
            while ((position >= positions[i]) && (i < positions.Length - 1))
            {
                if (i == positions.Length - 1)
                {
                    return i;
                }
                i++;
            }
            if ((i == positions.Length - 1) && (position == positions[i]))
            {
                return i;
            }
            return i - 1;
        }

        public static int GetEndIndex(float[] positions, float position)
        {
            var i = 0;
            while (position > positions[i])
            {
                i++;
            }
            return i;
        }

        private static void RenderConstantBetweenLocations(Graphics g, ILayer layer, bool fullyCovered)
        {
            var segmentsLayer = (NetworkCoverageSegmentLayer)layer;
            var coverage = ((NetworkCoverageFeatureCollection)layer.DataSource).RenderedCoverage;
            var mapExtents = layer.Map.Envelope;
            var theme = layer.Theme;

            int currentCount = 0;
            IBranch currentBranch = null;
            List<INetworkLocation> locations = null;

            var timeFilter = coverage.Filters.OfType<VariableValueFilter<DateTime>>().FirstOrDefault();
            var hasTime = coverage.IsTimeDependent && timeFilter != null && timeFilter.Values.Count > 0;

            if (coverage.IsTimeDependent && !hasTime) return;

            foreach (INetworkSegment networkSegment in coverage.Segments.Values)
            {
                if (currentBranch != networkSegment.Branch)
                {
                    currentCount = 0;
                    currentBranch = networkSegment.Branch;
                    locations = coverage.Locations.Values.Where(l => l.Branch == currentBranch).ToList();

                    if (fullyCovered)
                    {
                        var first = locations.FirstOrDefault();

                        if (first == null || first.Offset != 0)
                        {
                            locations.Insert(0, FindNodePoint(currentBranch.Source, coverage));
                        }
                    }
                }

                if (!networkSegment.Geometry.EnvelopeInternal.Intersects(mapExtents))
                {
                    currentCount++;
                    continue;
                }

                var value = !hasTime
                                ? coverage[locations[currentCount]]
                                : coverage[timeFilter.Values[0], locations[currentCount]];
                
                var style = (theme != null) ? (VectorStyle) theme.GetStyle(value != null ? (double) value : 0) : segmentsLayer.Style;

                VectorRenderingHelper.RenderGeometry(g, layer.Map, networkSegment.Geometry, style, null, true);
                currentCount++;
            }
        }

        private static INetworkLocation FindNodePoint(INode node, INetworkCoverage coverage)
        {
            var incommingBranchPoints = coverage.Locations.Values.Where(l => l.Offset == l.Branch.Length && node.IncomingBranches.Contains(l.Branch));
            var outGoingBranchPoints = coverage.Locations.Values.Where(l => l.Offset == 0 && node.OutgoingBranches.Contains(l.Branch));

            return incommingBranchPoints.Concat(outGoingBranchPoints).FirstOrDefault();
        }

        private static void RenderConstant(Graphics g, ILayer layer)
        {
            var segmentsLayer = (NetworkCoverageSegmentLayer)layer;
            var coverage = ((NetworkCoverageFeatureCollection)layer.DataSource).RenderedCoverage;
            var mapExtents = layer.Map.Envelope;

            var sliceValues = coverage.GetValues();
            var theme = segmentsLayer.Theme;
            var drawnBranches = new HashSet<IBranch>();

            // 1 find the segments withing the current extend
            var segments = coverage.Segments.Values;

            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (!segment.Geometry.EnvelopeInternal.Intersects(mapExtents) || sliceValues.Count <= 0)
                {
                    continue;
                }
                drawnBranches.Add(segment.Branch);
                // 2 get the values for this segment
                // if SegmentGenerationMethod == SegmentGenerationMethod.RouteBetweenLocations the segments and 
                // location do not have to match; return default value
                var value = coverage.SegmentGenerationMethod == SegmentGenerationMethod.RouteBetweenLocations
                                   ? 0
                                   : (double)sliceValues[i];

                // 3 use the Theme of the layer to draw 
                var style = (theme != null) ? (VectorStyle)theme.GetStyle(value) : segmentsLayer.Style;

                VectorRenderingHelper.RenderGeometry(g, layer.Map, segment.Geometry, style, null, true);
            }

            //// draw branches that are visible but without segment and thus not yet drawn.
            //var defaultStyle = (theme != null) ? (VectorStyle)theme.GetStyle(coverage.DefaultValue) : segmentsLayer.Style;
            //RenderSegmentFreeBranches(layer.Map, coverage, g, defaultStyle, mapExtents, drawnBranches, theme);
        }

        private static void RenderMissingSegments(Graphics g, ILayer layer)
        {
            var coverage = ((NetworkCoverageFeatureCollection)layer.DataSource).RenderedCoverage;

            var locations = coverage.Locations.Values;

            for (int i = 0; i < locations.Count - 1; i++)
            {
                var loc1 = locations[i];
                var loc2 = locations[i + 1];

                var segments = NetworkHelper.GetShortestPathBetweenBranchFeaturesAsNetworkSegments(coverage.Network, loc1, loc2);

                if (segments.Count != 0)
                {
                    continue;
                }

                var line = new LineString(new[] { loc1.Geometry.Coordinate, loc2.Geometry.Coordinate });

                var linePen = new Pen(Color.Red, 5f) { DashStyle = DashStyle.Dot };

                var missingSegmentStyle = new VectorStyle(Brushes.Black, linePen, false, linePen, 1.0f,
                                                          typeof(ILineString),
                                                          DelftTools.Utils.Drawing.ShapeType.Rectangle, 3);

                VectorRenderingHelper.RenderGeometry(g, layer.Map, line, missingSegmentStyle, null, true);

                linePen.Dispose();
            }
        }

        /// <summary>
        /// Draw a branch with no segments; this will usually be drawn with the style for the default value.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="coverage"></param>
        /// <param name="g"></param>
        /// <param name="style"></param>
        /// <param name="mapExtents"></param>
        /// <param name="drawnBranches"></param>
        /// <param name="theme"></param>
        private static void RenderSegmentFreeBranches(Map map, INetworkCoverage coverage, Graphics g, VectorStyle style, IEnvelope mapExtents, HashSet<IBranch> drawnBranches, ITheme theme)
        {
            var visibleBranches = coverage.Network.Branches.Where(b => b.Geometry.EnvelopeInternal.Intersects(mapExtents)).ToList();
            visibleBranches.ForEach(vb =>
            {
                if (!drawnBranches.Contains(vb))
                {
                    VectorRenderingHelper.RenderGeometry(g, map, vb.Geometry, style, null, true);
                }
            });
        }

        /// <summary>
        /// Draws a linestring using a gradient brush with colors
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="strokes">An array of coordinates to draw </param>
        /// <param name="width">with of the line to be drawn. There no support for multiple widths</param>
        /// <param name="colors">The colors to use in the brush</param>
        /// <param name="positions">The relative positions of the colors in the brush. Should be in range [0..1]</param>
        private static void DrawStrokesLinear(Graphics graphics, PointF[] strokes, int width, Color[] colors, float[] positions)
        {
            var totalLength = TotalDistance(strokes);
            double currentLength = 0;
            for (var i = 1; i < strokes.Length; i++)
            {
                // a LinearGradientBrush can only interpolate between two locations. To draw a line
                // with multliple colors we must split the linestring into straight segments and
                // create a brush for each segment.
                // strokes are in device coordinates; linearGradientBrush will fail if points overlap.
                if (Distance(strokes[i - 1], strokes[i]) > 2)
                {
                    var startOffset = (float)(currentLength / totalLength);
                    currentLength += Distance(strokes[i - 1], strokes[i]);
                    var endOffset = (float)(currentLength / totalLength);

                    var startColor = InterpolateColor(colors, positions, startOffset, false);
                    var endColor = InterpolateColor(colors, positions, endOffset, true);
                    var linearGradientBrush = new LinearGradientBrush(strokes[i - 1], strokes[i], startColor, endColor);
                    IList<Color> localColors = new List<Color>();
                    IList<float> localPositions = new List<float>();
                    localColors.Add(startColor);
                    localPositions.Add(0.0f);
                    for (var c = 0; c < positions.Length; c++)
                    {
                        if ((positions[c] <= startOffset) || (positions[c] >= endOffset))
                        {
                            continue;
                        }
                        localColors.Add(colors[c]);
                        localPositions.Add((positions[c] - startOffset) / (endOffset - startOffset));
                        if (localPositions[localPositions.Count - 1] == localPositions[localPositions.Count - 2])
                        {
                            localPositions[localPositions.Count - 1] = localPositions[localPositions.Count - 2] + 1.0e-6F;
                        }
                    }
                    localColors.Add(endColor);
                    localPositions.Add(1.0f);

                    var colorBlend = new ColorBlend { Colors = localColors.ToArray(), Positions = localPositions.ToArray() };

                    linearGradientBrush.InterpolationColors = colorBlend;
                    var pen = new Pen(linearGradientBrush, width);
                    if (i != (strokes.Length - 1))
                    {
                        using (var solidBrush = new SolidBrush(endColor))
                        {
                            graphics.FillEllipse(solidBrush, strokes[i].X - pen.Width / 2, strokes[i].Y - pen.Width / 2,
                                                 pen.Width, pen.Width);
                        }
                    }
                    graphics.DrawLine(pen, strokes[i - 1], strokes[i]);
                    pen.Dispose();
                    linearGradientBrush.Dispose();
                }
                else
                {
                    using (var solidBrush = new SolidBrush(colors[0]))
                    {
                        using (var pen = new Pen(solidBrush, width))
                        {
                            graphics.DrawLine(pen, strokes[i - 1], strokes[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the interpolated color
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="positions">Relative positions of the colors in colors. Duplicate position are allowed; see useFirst</param>
        /// <param name="position">Position relative to positions for which the color is requested</param>
        /// <param name="useFirst">When 2 or more positions are duplicate use the first when true else use the last</param>
        /// <returns></returns>
        private static Color InterpolateColor(Color[] colors, float[] positions, float position, bool useFirst)
        {
            // assert postions[0] < position < positions[positions.Length-1]
            if (positions[0] == position)
            {
                if (positions[0] != positions[1])
                    return colors[0];
            }
            var startIndex = GetStartIndex(positions, position);
            var endIndex = GetEndIndex(positions, position);
            if (startIndex < endIndex)
            {
                return InterpolateColor(colors[startIndex], colors[endIndex], positions[endIndex] - positions[startIndex],
                                    position - positions[startIndex]);
            }
            return useFirst ? colors[endIndex] : colors[startIndex];
        }

        private static Color InterpolateColor(Color minCol, Color maxCol, double totalLength, double length)
        {
            var frac = length / totalLength;
            if (frac == 1)
            {
                return maxCol;
            }
            if ((frac == 0) || (double.IsNaN(frac)))
            {
                return minCol;
            }
            var r = (maxCol.R - minCol.R) * frac + minCol.R;
            var g = (maxCol.G - minCol.G) * frac + minCol.G;
            var b = (maxCol.B - minCol.B) * frac + minCol.B;
            var a = (maxCol.A - minCol.A) * frac + minCol.A;
            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (b > 255) b = 255;
            if (a > 255) a = 255;
            if (a < 0) a = 0;
            return Color.FromArgb((int)a, (int)r, (int)g, (int)b);
        }

        private static double Distance(PointF first, PointF last)
        {
            return Math.Sqrt(((first.X - last.X) * (first.X - last.X)) + ((first.Y - last.Y) * (first.Y - last.Y)));
        }

        private static double TotalDistance(IList<PointF> strokes)
        {
            if (strokes.Count < 2)
            {
                return 0.0;
            }
            var distance = 0.0;
            for (var i = 1; i < strokes.Count; i++)
            {
                distance += Distance(strokes[i - 1], strokes[i]);
            }
            return distance;
        }
    }
}
