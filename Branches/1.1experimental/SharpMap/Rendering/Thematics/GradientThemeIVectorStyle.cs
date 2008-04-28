using System;
using SharpMap.Styles;

namespace SharpMap.Rendering.Thematics
{
    public class GradientThemeIVectorStyle
        : GradientThemeGdiBase<IVectorStyle>
    {




        public GradientThemeIVectorStyle(string columnName, double minValue, double maxValue, IVectorStyle minStyle, IVectorStyle maxStyle)
            : base(columnName, minValue, maxValue, minStyle, maxStyle) { }

        public override IVectorStyle GetStyle(SharpMap.Data.FeatureDataRow row)
        {
            double attr = 0;
            try { attr = Convert.ToDouble(row[this.ColumnName]); }
            catch { throw new ApplicationException("Invalid Attribute type in Gradient Theme - Couldn't parse attribute (must be numerical)"); }

            return CalculateStyle(MinStyle, MaxStyle, attr);
        }

        private IVectorStyle CalculateStyle(IVectorStyle min, IVectorStyle max, double value)
        {
            IVectorStyle style = new VectorStyle();
            double dFrac = Fraction(value);
            float fFrac = Convert.ToSingle(dFrac);
            style.Enabled = (dFrac > 0.5 ? min.Enabled : max.Enabled);
            style.EnableOutline = (dFrac > 0.5 ? min.EnableOutline : max.EnableOutline);
            if (FillColorBlend != null)
                style.Fill = new System.Drawing.SolidBrush(FillColorBlend.GetColor(fFrac));
            else if (min.Fill != null && max.Fill != null)
                style.Fill = InterpolateBrush(min.Fill, max.Fill, value);

            if (min.Line != null && max.Line != null)
                style.Line = InterpolatePen(min.Line, max.Line, value);
            if (LineColorBlend != null)
                style.Line.Color = LineColorBlend.GetColor(fFrac);

            if (min.Outline != null && max.Outline != null)
                style.Outline = InterpolatePen(min.Outline, max.Outline, value);
            style.MinVisible = InterpolateDouble(min.MinVisible, max.MinVisible, value);
            style.MaxVisible = InterpolateDouble(min.MaxVisible, max.MaxVisible, value);
            style.Symbol = (dFrac > 0.5 ? min.Symbol : max.Symbol);
            style.SymbolOffset = (dFrac > 0.5 ? min.SymbolOffset : max.SymbolOffset); //We don't interpolate the offset but let it follow the symbol instead
            style.SymbolScale = InterpolateFloat(min.SymbolScale, max.SymbolScale, value);
            return style;
        }


    }
}
