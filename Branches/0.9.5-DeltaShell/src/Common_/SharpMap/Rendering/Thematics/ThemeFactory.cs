using System;
using System.Collections.Generic;
using System.Drawing;
using DelftTools.Utils.Drawing;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Index.Bintree;
using SharpMap.Styles;

namespace SharpMap.Rendering.Thematics
{
    public abstract class ThemeFactory
    {
        public static GradientTheme CreateGradientTheme(string attribute, VectorStyle defaultStyle, ColorBlend blend, 
            float minValue, float maxValue, int sizeMin, int sizeMax, bool skipColors, bool skipSizes)
        {
            return CreateGradientTheme(attribute, defaultStyle, blend, minValue, maxValue, sizeMin, sizeMax, skipColors,
                                       skipSizes, 8);
        }

        public static GradientTheme CreateGradientTheme(string attribute, VectorStyle defaultStyle, ColorBlend blend, 
            float minValue, float maxValue, int sizeMin, int sizeMax, bool skipColors, bool skipSizes, int numberOfClasses)
        {
            if(defaultStyle == null)
            {
                defaultStyle = new VectorStyle();
                defaultStyle.GeometryType = typeof(IPolygon);
            }

            Color minColor = (skipColors)? ((SolidBrush) defaultStyle.Fill).Color : blend.GetColor(0);
            Color maxColor = (skipColors) ? ((SolidBrush)defaultStyle.Fill).Color : blend.GetColor(1);

            var deltaWith = (defaultStyle.Outline.Width - defaultStyle.Line.Width);

            float minOutlineSize = deltaWith + sizeMin;
            float maxOutlineSize = deltaWith + sizeMax;

            // Use default styles if not working with VectorLayers (i.e. RegularGridCoverageLayers)
            var minStyle = (VectorStyle) defaultStyle.Clone();
            var maxStyle = (VectorStyle) defaultStyle.Clone();

            minStyle.GeometryType = defaultStyle.GeometryType;
            maxStyle.GeometryType = defaultStyle.GeometryType;

            if (defaultStyle.GeometryType == typeof(IPoint))
            {
                minStyle.Fill = new SolidBrush(minColor);
                maxStyle.Fill = new SolidBrush(maxColor);
                minStyle.Shape = defaultStyle.Shape;
                maxStyle.Shape = defaultStyle.Shape;
                if (!skipSizes)
                {
                    minStyle.Line.Width = sizeMin;
                    maxStyle.Line.Width = sizeMax;
                    minStyle.ShapeSize = sizeMin;
                    maxStyle.ShapeSize = sizeMax;
                }
            }
            else if ((defaultStyle.GeometryType == typeof(IPolygon)) || (defaultStyle.GeometryType == typeof(IMultiPolygon)))
            {
                minStyle.Fill = new SolidBrush(minColor);
                maxStyle.Fill = new SolidBrush(maxColor);
                minStyle.Outline = new Pen(defaultStyle.Outline.Color, minOutlineSize);
                maxStyle.Outline = new Pen(defaultStyle.Outline.Color, maxOutlineSize);
            }
            else if ((defaultStyle.GeometryType == typeof(ILineString)) || (defaultStyle.GeometryType == typeof(IMultiLineString)))
            {
                minStyle.Line = new Pen(minColor, sizeMin);
                maxStyle.Line = new Pen(maxColor, sizeMax);
                minStyle.Outline = new Pen(defaultStyle.Outline.Color, minOutlineSize);
                maxStyle.Outline = new Pen(defaultStyle.Outline.Color, maxOutlineSize);
            }
            else
            {
                minStyle.Fill = new SolidBrush(minColor);
                maxStyle.Fill = new SolidBrush(maxColor);
                minStyle.Outline = new Pen(minColor, minOutlineSize);
                maxStyle.Outline = new Pen(maxColor, maxOutlineSize);
            }

            var gradientTheme = new GradientTheme(attribute, minValue, maxValue, minStyle, maxStyle, blend, blend, null, numberOfClasses);
            return gradientTheme;
       }

        public static CategorialTheme CreateCategorialTheme(string attribute, VectorStyle defaultStyle, ColorBlend blend, 
            int numberOfClasses, IList<IComparable> values, List<string> categories)
        {
            if (defaultStyle == null)
            {
                defaultStyle = new VectorStyle
                                   {
                                       GeometryType = typeof (IPolygon)
                                   };
            }

            var categorialTheme = new CategorialTheme(attribute, defaultStyle);

            for (int i = 0; i < numberOfClasses; i++)
            {
                string label = (categories != null)
                                   ? categories[i]
                                   : values[i].ToString();

                Color color = (numberOfClasses > 1)
                                  ? blend.GetColor((float) i/(numberOfClasses - 1))
                                  : ((SolidBrush) defaultStyle.Fill).Color;
                
                var vectorStyle = (VectorStyle) defaultStyle.Clone();

                if (defaultStyle.GeometryType == typeof(IPoint))
                {
                    vectorStyle.Fill = new SolidBrush(color);
                    vectorStyle.Line.Width = 16;
                    vectorStyle.Shape = defaultStyle.Shape;
                }
                else if ((defaultStyle.GeometryType == typeof(IPolygon)) || (defaultStyle.GeometryType == typeof(IMultiPolygon)))
                {
                    vectorStyle.Fill = new SolidBrush(color);
                }
                else if ((defaultStyle.GeometryType == typeof(ILineString)) || (defaultStyle.GeometryType == typeof(IMultiLineString)))
                {
                    vectorStyle.Line = new Pen(color, defaultStyle.Line.Width);
                }
                else
                {
                    vectorStyle.Fill = new SolidBrush(color);
                }

                CategorialThemeItem categorialThemeItem = (values[i] != null)
                                                              ? new CategorialThemeItem(label, vectorStyle, vectorStyle.LegendSymbol, values[i])
                                                              : new CategorialThemeItem(label, vectorStyle, vectorStyle.LegendSymbol);

                
                categorialTheme.AddThemeItem(categorialThemeItem);
            }

            return categorialTheme;
        }

        public static QuantityTheme CreateQuantityTheme(string attribute, VectorStyle defaultStyle, ColorBlend blend, 
            int numberOfClasses, IList<Interval> intervals)
        {
            float minSize = defaultStyle.Line.Width;
            float maxSize = defaultStyle.Line.Width;

            return CreateQuantityTheme(attribute, defaultStyle, blend, numberOfClasses, intervals, minSize, maxSize, false, false);
        }

        public static QuantityTheme CreateQuantityTheme(string attribute, VectorStyle defaultStyle, ColorBlend blend, 
            int numberOfClasses, IList<Interval> intervals, float minSize, float maxSize, bool skipColors, bool skipSizes)
        {
            if (defaultStyle == null)
            {
                defaultStyle = new VectorStyle();
                defaultStyle.GeometryType = typeof(IPolygon);
            }

            var quantityTheme = new QuantityTheme(attribute, defaultStyle);
            
            var totalMinValue = (float) intervals[0].Min;
            var totalMaxValue = (float) intervals[intervals.Count - 1].Max;
            
            if (totalMinValue == totalMaxValue)
            {
                return null;
            }

            for (int i = 0; i < numberOfClasses; i++)
            {
                Color color = numberOfClasses > 1
                                  ? blend.GetColor(1 - (float) i/(numberOfClasses - 1))
                                  : ((SolidBrush) defaultStyle.Fill).Color;

                float size = defaultStyle.Line.Width;

                if (!skipSizes)
                {
                    var minValue = (float) intervals[i].Min;
                    var maxValue = (float) intervals[i].Max;
                    
                    float width = maxValue - minValue;
                    float mean = minValue + 0.5f * width;

                    float fraction = (mean - totalMinValue) / (totalMaxValue - totalMinValue);

                    size = minSize + fraction * (maxSize - minSize);
                }

                var vectorStyle = new VectorStyle
                                      {
                                          GeometryType = defaultStyle.GeometryType
                                      };

                if (defaultStyle.GeometryType == typeof(IPoint))
                {
                    if (skipColors)
                    {
                        color = ((SolidBrush)defaultStyle.Fill).Color;
                    }

                    vectorStyle.Fill = new SolidBrush(color);
                    vectorStyle.Shape = defaultStyle.Shape;

                    if (!skipSizes)
                    {
                        vectorStyle.ShapeSize = Convert.ToInt32(size);
                        vectorStyle.Line.Width = size;
                    }

                }
                else if ((defaultStyle.GeometryType == typeof(IPolygon)) || (defaultStyle.GeometryType == typeof(IMultiPolygon)))
                {
                    if (skipColors)
                    {
                        color = ((SolidBrush)defaultStyle.Fill).Color;
                    }
                    vectorStyle.Fill = new SolidBrush(color);
                    vectorStyle.Line = new Pen(color, size);
                    vectorStyle.Outline.Width = (defaultStyle.Outline.Width - defaultStyle.Line.Width) + size;
                }
                else if ((defaultStyle.GeometryType == typeof(ILineString)) || (defaultStyle.GeometryType == typeof(IMultiLineString)))
                {
                    if (skipColors)
                    {
                        color = defaultStyle.Line.Color;
                    }
                    vectorStyle.Line = new Pen(color, size);
                    vectorStyle.Outline.Width = (defaultStyle.Outline.Width - defaultStyle.Line.Width) + size;
                }
                else
                {
                    vectorStyle.Fill = new SolidBrush(color);
                }
              
                quantityTheme.AddStyle(vectorStyle, intervals[i]);
            }

            return quantityTheme;
        }

        public static CustomTheme CreateSingleFeatureTheme(Type geometryType, Color color, float width)
        {
            var vectorStyle = new VectorStyle {GeometryType = geometryType};

            if (geometryType == typeof(IPoint))
            {
                vectorStyle.Fill = new SolidBrush(color); // also used for updating symbol
                vectorStyle.Line = new Pen(color, width);
            }
            else if ((geometryType == typeof(IPolygon)) || (geometryType == typeof(IMultiPolygon)))
            {
                vectorStyle.Fill = new SolidBrush(color);
            }
            else if ((geometryType == typeof(ILineString)) || (geometryType == typeof(IMultiLineString)))
            {
                vectorStyle.Line = new Pen(color, width);
            }
            else
            {
                vectorStyle.Fill = new SolidBrush(color);
            }
            vectorStyle.Shape = ShapeType.Diamond;

            return new CustomTheme(null) { DefaultStyle = vectorStyle }; 
        }
    }
}
