// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Drawing;
using GeoAPI.Extensions.Feature;
using NetTopologySuite.Extensions.Features;
using SharpMap.Styles;

namespace SharpMap.Rendering.Thematics
{
    /// <summary>
    /// The GradientTheme class defines a gradient color thematic rendering of features based by a numeric attribute.
    /// </summary>
    [NotifyPropertyChanged(EnableLogging = false)]
    public class GradientTheme : Theme
    {
        private double maxValue;
        private double minValue;

        private readonly GradientThemeItem maxItem;
        private readonly GradientThemeItem minItem;
        private Color minColor;
        private Color maxColor;
        private ColorBlend fillColorBlend;
        private ColorBlend lineColorBlend;
        private ColorBlend textColorBlend;

        private string attributeName;

        /// <summary>
        /// Initializes a new instance of the GradientTheme class
        /// </summary>
        /// <remarks>
        /// <para>The gradient theme interpolates linearly between two styles based on a numerical attribute in the datasource.
        /// This is useful for scaling symbols, line widths, line and fill colors from numerical attributes.</para>
        /// <para>Colors are interpolated between two colors, but if you want to interpolate through more colors (fx. a rainbow),
        /// set the <see cref="TextColorBlend"/>, <see cref="LineColorBlend"/> and <see cref="FillColorBlend"/> properties
        /// to a custom <see cref="ColorBlend"/>.
        /// </para>
        /// <para>The following properties are scaled (properties not mentioned here are not interpolated):
        /// <list type="table">
        ///		<listheader><term>Property</term><description>Remarks</description></listheader>
        ///		<item><term><see cref="System.Drawing.Color"/></term><description>Red, Green, Blue and Alpha values are linearly interpolated.</description></item>
        ///		<item><term><see cref="System.Drawing.Pen"/></term><description>The color, width, color of pens are interpolated. MiterLimit,StartCap,EndCap,LineJoin,DashStyle,DashPattern,DashOffset,DashCap,CompoundArray, and Alignment are switched in the middle of the min/max values.</description></item>
        ///		<item><term><see cref="System.Drawing.SolidBrush"/></term><description>SolidBrush color are interpolated. Other brushes are not supported.</description></item>
        ///		<item><term><see cref="SharpMap.Styles.VectorStyle"/></term><description>MaxVisible, MinVisible, Line, Outline, Fill and SymbolScale are scaled linearly. Symbol, EnableOutline and Enabled switch in the middle of the min/max values.</description></item>
        ///		<item><term><see cref="SharpMap.Styles.LabelStyle"/></term><description>FontSize, BackColor, ForeColor, MaxVisible, MinVisible, Offset are scaled linearly. All other properties use min-style.</description></item>
        /// </list>
        /// </para>
        /// <example>
        /// Creating a rainbow colorblend showing colors from red, through yellow, green and blue depicting 
        /// the population density of a country.
        /// <code lang="C#">
        /// //Create two vector styles to interpolate between
        /// SharpMap.Styles.VectorStyle min = new SharpMap.Styles.VectorStyle();
        /// SharpMap.Styles.VectorStyle max = new SharpMap.Styles.VectorStyle();
        /// min.Outline.Width = 1f; //Outline width of the minimum value
        /// max.Outline.Width = 3f; //Outline width of the maximum value
        /// //Create a theme interpolating population density between 0 and 400
        /// SharpMap.Rendering.Thematics.GradientTheme popdens = new SharpMap.Rendering.Thematics.GradientTheme("PopDens", 0, 400, min, max);
        /// //Set the fill-style colors to be a rainbow blend from red to blue.
        /// popdens.FillColorBlend = SharpMap.Rendering.Thematics.ColorBlend.Rainbow5;
        /// myVectorLayer.Theme = popdens;
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="columnName">Name of column to extract the attribute</param>
        /// <param name="minValue">Minimum value</param>
        /// <param name="maxValue">Maximum value</param>
        /// <param name="minStyle">Color for minimum value</param>
        /// <param name="maxStyle">Color for maximum value</param>
        /// <param name="fillColorBlend"></param>
        /// <param name="lineColorBlend"></param>
        /// <param name="textColorBlend"></param>
        /// <param name="numberOfClasses"></param>
        public GradientTheme(string columnName, double minValue, double maxValue, IStyle minStyle, IStyle maxStyle,
                    ColorBlend fillColorBlend, ColorBlend lineColorBlend, ColorBlend textColorBlend) : this(columnName, minValue, maxValue, minStyle, maxStyle,
                    fillColorBlend, lineColorBlend, textColorBlend, 8)
        {
            
        }
       
        public GradientTheme(string columnName, double minValue, double maxValue, IStyle minStyle, IStyle maxStyle,
                    ColorBlend fillColorBlend, ColorBlend lineColorBlend, ColorBlend textColorBlend, int numberOfClasses)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;

            this.fillColorBlend = fillColorBlend;
            this.lineColorBlend = lineColorBlend;
            this.textColorBlend = textColorBlend;

            attributeName = columnName;

            //create themeitems only for the extremes. Other values are interpolated.
            minItem = new GradientThemeItem(minStyle, string.Format("{0:g4}", minValue),string.Format("{0:g4}", minValue));
            themeItems.Add(minItem);

            maxItem = new GradientThemeItem(maxStyle, string.Format("{0:g4}", maxValue),
                                            string.Format("{0:g4}", maxValue));

            double step = (maxValue - minValue) / numberOfClasses;
            for (int i = 1; i <= numberOfClasses -2; i++)
            {
                double value = minValue + i * step;
                IStyle style = GetStyle(value);
                var gradientThemeItem = new GradientThemeItem(style, string.Format("{0:g4}", value), string.Format("{0:g4}", value));
                themeItems.Add(gradientThemeItem);
            }

            themeItems.Add(maxItem);

            minColor = ThemeHelper.ExtractFillColorFromThemeItem(minItem);
            minItem.Symbol = ((VectorStyle) minStyle).LegendSymbol;
            maxColor = ThemeHelper.ExtractFillColorFromThemeItem(maxItem);
            maxItem.Symbol = ((VectorStyle) maxStyle).LegendSymbol;
        }


        /// <summary>
        /// Gets or sets the column name from where to get the attribute value
        /// </summary>
        public string ColumnName
        {
            get { return attributeName; }
            set { attributeName = value; }
        }

        /// <summary>
        /// Gets or sets the minimum value of the gradient
        /// </summary>
        public double Min
        {
            get { return minValue; }
            set
            {
                minValue = value;
                minItem.Label = minValue.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the maximum value of the gradient
        /// </summary>
        public double Max
        {
            get { return maxValue; }
            set
            {
                maxValue = value;
                maxItem.Label = maxValue.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="SharpMap.Styles.IStyle">style</see> for the minimum value
        /// </summary>
        public IStyle MinStyle
        {
            get { return minItem.Style; } //minStyle; }
            set { minItem.Style = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="SharpMap.Styles.IStyle">style</see> for the maximum value
        /// </summary>
        public IStyle MaxStyle
        {
            get { return maxItem.Style; } //maxStyle; }
            set { maxItem.Style = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="SharpMap.Rendering.Thematics.ColorBlend"/> used on labels
        /// </summary>
        public ColorBlend TextColorBlend
        {
            get { return textColorBlend; }
            set { textColorBlend = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="SharpMap.Rendering.Thematics.ColorBlend"/> used on lines
        /// </summary>
        public ColorBlend LineColorBlend
        {
            get { return lineColorBlend; }
            set { lineColorBlend = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="SharpMap.Rendering.Thematics.ColorBlend"/> used as Fill
        /// </summary>
        public ColorBlend FillColorBlend
        {
            get { return fillColorBlend; }
            set { fillColorBlend = value; }
        }

        /// <summary>
        /// Returns the style based on a numeric DataColumn, where style
        /// properties are linearly interpolated between max and min values.
        /// </summary>
        /// <param name="feature">Feature</param>
        /// <returns><see cref="SharpMap.Styles.IStyle">Style</see> calculated by a linear interpolation between the min/max styles</returns>
        public override IStyle GetStyle(IFeature feature)
        {
            double attr;
            try
            {
                attr = FeatureAttributeAccessorHelper.GetAttributeValue<double>(feature, attributeName);
            }
            catch
            {
                throw new ApplicationException(
                    "Invalid Attribute type in Gradient Theme - Couldn't parse attribute (must be numerical)");
            }
            if (MinStyle.GetType() != MaxStyle.GetType())
                throw new ArgumentException("MinStyle and MaxStyle must be of the same type");
            switch (MinStyle.GetType().FullName)
            {
                case "SharpMap.Styles.VectorStyle":
                    return CalculateVectorStyle(MinStyle as VectorStyle, MaxStyle as VectorStyle, attr);
                case "SharpMap.Styles.LabelStyle":
                    return CalculateLabelStyle(MinStyle as LabelStyle, MaxStyle as LabelStyle, attr);
                default:
                    throw new ArgumentException(
                        "Only SharpMap.Styles.VectorStyle and SharpMap.Styles.LabelStyle are supported for the gradient theme");
            }
        }

        public override IStyle GetStyle<T>(T value)
        {
            // Assumes this value is a double, float or int and gets the vector style for this numeric value
            if (!(value is double || value is float || value is int))
            {
                throw new NotSupportedException(
                    "Gradient theme only supports numeric value types (double, float or int).");
            }

            if (value is double && double.IsNaN(Convert.ToDouble(value)))
            {
                var transparentBrush = new SolidBrush(Color.Transparent);
                return new VectorStyle
                {
                    Fill = transparentBrush,
                    Line = new Pen(transparentBrush)
                };
            }

            return CalculateVectorStyle((VectorStyle) MinStyle, (VectorStyle) MaxStyle, Convert.ToDouble(value));
        }

        public IStyle GetStyle(double attributeValue)
        {
            return CalculateVectorStyle((VectorStyle) MinStyle, (VectorStyle) MaxStyle, attributeValue);
        }

        /// <summary>
        /// Calculates the style for the gradient Theme. Use the constructor when all values are known because
        /// it will also update the symbol (bitmap).
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected VectorStyle CalculateVectorStyle(VectorStyle min, VectorStyle max, double value)
        {
            //var style = new VectorStyle();
            double dFrac = Fraction(value);
            float fFrac = Convert.ToSingle(dFrac);
            //bool enabled = (dFrac > 0.5 ? min.Enabled : max.Enabled);
            bool enableOutline = (dFrac > 0.5 ? min.EnableOutline : max.EnableOutline);

            Brush fillStyle = null;
            if (fillColorBlend != null)
                fillStyle = new SolidBrush(fillColorBlend.GetColor(fFrac));
            else if (min.Fill != null && max.Fill != null)
                fillStyle = InterpolateBrush(min.Fill, max.Fill, value);

            Pen lineStyle;
            if (lineColorBlend != null)
            {
                lineStyle = new Pen(lineColorBlend.GetColor(fFrac), InterpolateFloat(min.Line.Width, max.Line.Width, value));
            }
            else
            {
                lineStyle = InterpolatePen(min.Line, max.Line, value);
            }
            // assume line and outline same for gradient theme

            Pen outLineStyle = null;
            if (min.Outline != null && max.Outline != null)
                outLineStyle = InterpolatePen(min.Outline, max.Outline, value);

            ShapeType shapeType = min.Shape;
            float symbolScale = InterpolateFloat(min.SymbolScale, max.SymbolScale, value);
            Type geometryType = min.GeometryType;
            int shapeSize = InterpolateInt(min.ShapeSize, max.ShapeSize, value);
            var style = new VectorStyle(fillStyle, outLineStyle, enableOutline, lineStyle, symbolScale, geometryType, shapeType, shapeSize)
                            {
                                MinVisible = InterpolateDouble(min.MinVisible, max.MinVisible, value),
                                MaxVisible = InterpolateDouble(min.MaxVisible, max.MaxVisible, value),
                                Enabled = (dFrac > 0.5 ? min.Enabled : max.Enabled)
                            };
            return style;
        }

        protected LabelStyle CalculateLabelStyle(LabelStyle min, LabelStyle max, double value)
        {
            var style = new LabelStyle();
            style.CollisionDetection = min.CollisionDetection;
            style.Enabled = InterpolateBool(min.Enabled, max.Enabled, value);
            float FontSize = InterpolateFloat(min.Font.Size, max.Font.Size, value);
            style.Font = new Font(min.Font.FontFamily, FontSize, min.Font.Style);
            if (min.BackColor != null && max.BackColor != null)
            {
                style.BackColor = InterpolateBrush(min.BackColor, max.BackColor, value);
            }

            if (textColorBlend != null)
            {
                style.ForeColor = lineColorBlend.GetColor(Convert.ToSingle(Fraction(value)));
            }
            else
            {
                style.ForeColor = InterpolateColor(min.ForeColor, max.ForeColor, value);
            }
            if (min.Halo != null && max.Halo != null)
            {
                style.Halo = InterpolatePen(min.Halo, max.Halo, value);
            }

            style.MinVisible = InterpolateDouble(min.MinVisible, max.MinVisible, value);
            style.MaxVisible = InterpolateDouble(min.MaxVisible, max.MaxVisible, value);
            style.Offset =
                new PointF(InterpolateFloat(min.Offset.X, max.Offset.X, value),
                           InterpolateFloat(min.Offset.Y, max.Offset.Y, value));
            return style;
        }

        private double Fraction(double attr)
        {
            if (attr < minValue) return 0;
            if (attr > maxValue) return 1;
            double delta = Math.Abs(maxValue - minValue);
            if (delta < 1e-8)
                return 0;
            return (attr - minValue) / (delta);
        }

        private bool InterpolateBool(bool min, bool max, double attr)
        {
            double frac = Fraction(attr);
            if (frac > 0.5) return max;
            else return min;
        }

        private float InterpolateFloat(float min, float max, double attr)
        {
            return Convert.ToSingle((max - min)*Fraction(attr) + min);
        }

        private double InterpolateDouble(double min, double max, double attr)
        {
            return (max - min) * Fraction(attr) + min;
        }

        private int InterpolateInt(int min, int max, double attr)
        {
            return (int) ((max - min) * Fraction(attr) + min);
        }

        private SolidBrush InterpolateBrush(Brush min, Brush max, double attr)
        {
            if (min.GetType() != typeof (SolidBrush) || max.GetType() != typeof (SolidBrush))
                throw (new ArgumentException("Only SolidBrush brushes are supported in GradientTheme"));
            return new SolidBrush(InterpolateColor((min as SolidBrush).Color, (max as SolidBrush).Color, attr));
        }

        private Pen InterpolatePen(Pen min, Pen max, double attr)
        {
            if (min.PenType != PenType.SolidColor || max.PenType != PenType.SolidColor)
                throw (new ArgumentException("Only SolidColor pens are supported in GradientTheme"));
            Pen pen =
                new Pen(InterpolateColor(min.Color, max.Color, attr), InterpolateFloat(min.Width, max.Width, attr));
            double frac = Fraction(attr);
            pen.MiterLimit = InterpolateFloat(min.MiterLimit, max.MiterLimit, attr);
            pen.StartCap = (frac > 0.5 ? max.StartCap : min.StartCap);
            pen.EndCap = (frac > 0.5 ? max.EndCap : min.EndCap);
            pen.LineJoin = (frac > 0.5 ? max.LineJoin : min.LineJoin);
            pen.DashStyle = (frac > 0.5 ? max.DashStyle : min.DashStyle);
            if (min.DashStyle == DashStyle.Custom && max.DashStyle == DashStyle.Custom)
                pen.DashPattern = (frac > 0.5 ? max.DashPattern : min.DashPattern);
            pen.DashOffset = (frac > 0.5 ? max.DashOffset : min.DashOffset);
            pen.DashCap = (frac > 0.5 ? max.DashCap : min.DashCap);
            if (min.CompoundArray.Length > 0 && max.CompoundArray.Length > 0)
                pen.CompoundArray = (frac > 0.5 ? max.CompoundArray : min.CompoundArray);
            pen.Alignment = (frac > 0.5 ? max.Alignment : min.Alignment);
            //pen.CustomStartCap = (frac > 0.5 ? max.CustomStartCap : min.CustomStartCap);  //Throws ArgumentException
            //pen.CustomEndCap = (frac > 0.5 ? max.CustomEndCap : min.CustomEndCap);  //Throws ArgumentException
            return pen;
        }

        private Color InterpolateColor(Color minCol, Color maxCol, double attr)
        {
            double frac = Fraction(attr);
            if (frac == 1)
            {
                return maxCol;
            }
            if ((frac == 0) || (double.IsNaN(frac)))
            {
                return minCol;
            }
            double r = (maxCol.R - minCol.R)*frac + minCol.R;
            double g = (maxCol.G - minCol.G)*frac + minCol.G;
            double b = (maxCol.B - minCol.B)*frac + minCol.B;
            double a = (maxCol.A - minCol.A)*frac + minCol.A;
            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (b > 255) b = 255;
            if (a > 255) a = 255;
            if (a < 0) a = 0;
            return Color.FromArgb((int) a, (int) r, (int) g, (int) b);
        }

        public override object Clone()
        {
            var gradientTheme = new GradientTheme(attributeName, minValue, maxValue, (IStyle) MinStyle.Clone(),
                                  (IStyle) MaxStyle.Clone(),
                                  (null != FillColorBlend) ? (ColorBlend)FillColorBlend.Clone() : null,
                                  (null != LineColorBlend) ? (ColorBlend)LineColorBlend.Clone() : null,
                                  (null != TextColorBlend) ? (ColorBlend)TextColorBlend.Clone() : null);
            
            gradientTheme.ThemeItems.Clear();
            
            foreach (var themeItem in ThemeItems)
            {
                gradientTheme.ThemeItems.Add((IThemeItem) ((GradientThemeItem) themeItem).Clone());
            }

            if (NoDataValues != null)
            {
                gradientTheme.NoDataValues = NoDataValues.Cast<object>().ToArray();
            }

            return gradientTheme;
        }

        public override Color GetFillColor<T>(T value)
        {
            // hack: get value using field because field getter is less performand due to aop aspect.
            if (noDataValues != null && noDataValues.Contains(value))
            {
                return NoDataColor;
            }

            if (fillColorBlend != null)
            {
                double fraction = Fraction(Convert.ToDouble(value));
                return fillColorBlend.GetColor((float) fraction);
            }

            return InterpolateColor(minColor, maxColor, Convert.ToDouble(value));
        }

        /// <summary>
        /// Fills array of colors based on current configuration of theme.
        /// This function is optimized for performance. Keep large loop as simple as possible.
        /// note method is unsafe to allow direct manipulation of colors in bitmap
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="colors">pointer to colors in bitmap</param>
        /// <param name="length">the number of colors</param>
        /// <param name="values">array with values to convert to colors. Length should equal lenght</param>
        public override unsafe void GetFillColors<T>(int* colors, int length, T[] values)
        {
            //update color due to changes in the min or max theme.
            minColor = ThemeHelper.ExtractFillColorFromThemeItem(minItem);
            maxColor = ThemeHelper.ExtractFillColorFromThemeItem(maxItem);

            if (length != values.Length)
            {
                throw new ArgumentException("GetFillColors: length of targer array should match number of source values", "length");
            }
            if (noDataValues != null)
            {
                if (fillColorBlend != null)
                {
                    for (int i = 0; i < length; i++)
                    {
                        if (noDataValues.Contains(values[i]))
                        {
                            colors[i] = NoDataColor.ToArgb();
                            continue;
                        }
                        double fraction = Fraction(Convert.ToDouble(values[i]));
                        colors[i] = fillColorBlend.GetColor((float)fraction).ToArgb();
                    }
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        if (noDataValues.Contains(values[i]))
                        {
                            colors[i] = NoDataColor.ToArgb();
                            continue;
                        }
                        colors[i] = InterpolateColor(minColor, maxColor, Convert.ToDouble(values[i])).ToArgb();
                    }
                }
            }
            else
            {
                if (fillColorBlend != null)
                {
                    for (int i = 0; i < length; i++)
                    {
                        double fraction = Fraction(Convert.ToDouble(values[i]));
                        colors[i] = fillColorBlend.GetColor((float)fraction).ToArgb();
                    }
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        colors[i] = InterpolateColor(minColor, maxColor, Convert.ToDouble(values[i])).ToArgb();
                    }
                }
            }
        }
    }
}