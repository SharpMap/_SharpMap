using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using SharpMap.Styles;

namespace SharpMap.Rendering.Thematics
{
    /// <summary>
    /// I'm trying to abstract out the GDI reliance to reuse the base classes/interfaces for other non gdi rendering options
    /// its getting a bit fishy though... mmm watch this space. jd
    /// </summary>
    /// <typeparam name="TStyle"></typeparam>
    public abstract class GradientThemeGdiBase<TStyle>
        : GradientThemeBase<TStyle>, IGradientThemeGdi<TStyle>
        where TStyle : IStyle
    {
        public GradientThemeGdiBase(string columnName, double minValue, double maxValue, TStyle minStyle, TStyle maxStyle)
            : base(columnName, minValue, maxValue, minStyle, maxStyle) { }

        protected SolidBrush InterpolateBrush(System.Drawing.Brush min, System.Drawing.Brush max, double attr)
        {
            if (min.GetType() != typeof(System.Drawing.SolidBrush) || max.GetType() != typeof(System.Drawing.SolidBrush))
                throw (new ArgumentException("Only SolidBrush brushes are supported in GradientTheme"));
            return new System.Drawing.SolidBrush(InterpolateColor((min as System.Drawing.SolidBrush).Color, (max as System.Drawing.SolidBrush).Color, attr));
        }

        protected Pen InterpolatePen(Pen min, Pen max, double attr)
        {
            if (min.PenType != PenType.SolidColor || max.PenType != PenType.SolidColor)
                throw (new ArgumentException("Only SolidColor pens are supported in GradientTheme"));
            System.Drawing.Pen pen = new System.Drawing.Pen(InterpolateColor(min.Color, max.Color, attr), InterpolateFloat(min.Width, max.Width, attr));
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

        protected Color InterpolateColor(Color minCol, System.Drawing.Color maxCol, double attr)
        {
            double frac = Fraction(attr);
            if (frac == 1)
                return maxCol;
            else if (frac == 0)
                return minCol;
            else
            {
                double r = (maxCol.R - minCol.R) * frac + minCol.R;
                double g = (maxCol.G - minCol.G) * frac + minCol.G;
                double b = (maxCol.B - minCol.B) * frac + minCol.B;
                double a = (maxCol.A - minCol.A) * frac + minCol.A;
                if (r > 255) r = 255;
                if (g > 255) g = 255;
                if (b > 255) b = 255;
                if (a > 255) a = 255;
                return System.Drawing.Color.FromArgb((int)a, (int)r, (int)g, (int)b);
            }
        }

        private ColorBlend _LineColorBlend;
        /// <summary>
        /// Gets or sets the <see cref="SharpMap.Rendering.Thematics.ColorBlend"/> used on lines
        /// </summary>
        public ColorBlend LineColorBlend
        {
            get { return _LineColorBlend; }
            set { _LineColorBlend = value; }
        }

        private ColorBlend _FillColorBlend;

        /// <summary>
        /// Gets or sets the <see cref="SharpMap.Rendering.Thematics.ColorBlend"/> used as Fill
        /// </summary>
        public ColorBlend FillColorBlend
        {
            get { return _FillColorBlend; }
            set { _FillColorBlend = value; }
        }

    }
}
