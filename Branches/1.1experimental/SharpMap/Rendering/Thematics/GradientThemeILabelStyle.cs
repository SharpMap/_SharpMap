using System;
using SharpMap.Styles;

namespace SharpMap.Rendering.Thematics
{
    public class GradientThemeILabelStyle
        : GradientThemeGdiBase<ILabelStyle>
    {
        public GradientThemeILabelStyle(string columnName, double minValue, double maxValue, ILabelStyle minStyle, ILabelStyle maxStyle)
            : base(columnName, minValue, maxValue, minStyle, maxStyle) { }

        public override ILabelStyle GetStyle(SharpMap.Data.FeatureDataRow row)
        {
            double attr = 0;
            try { attr = Convert.ToDouble(row[this.ColumnName]); }
            catch { throw new ApplicationException("Invalid Attribute type in Gradient Theme - Couldn't parse attribute (must be numerical)"); }

            return CalculateStyle(MinStyle, MaxStyle, attr);
        }

        private ColorBlend _TextColorBlend;

        /// <summary>
        /// Gets or sets the <see cref="SharpMap.Rendering.Thematics.ColorBlend"/> used on labels
        /// </summary>
        public ColorBlend TextColorBlend
        {
            get { return _TextColorBlend; }
            set { _TextColorBlend = value; }
        }


        private LabelStyle CalculateStyle(ILabelStyle min, ILabelStyle max, double value)
        {
            LabelStyle style = new LabelStyle();
            style.CollisionDetection = min.CollisionDetection;
            style.Enabled = InterpolateBool(min.Enabled, max.Enabled, value);
            float FontSize = InterpolateFloat(min.Font.Size, max.Font.Size, value);
            style.Font = new System.Drawing.Font(min.Font.FontFamily, FontSize, min.Font.Style);
            if (min.BackColor != null && max.BackColor != null)
                style.BackColor = InterpolateBrush(min.BackColor, max.BackColor, value);

            if (_TextColorBlend != null)
                style.ForeColor = LineColorBlend.GetColor(Convert.ToSingle(Fraction(value)));
            else
                style.ForeColor = InterpolateColor(min.ForeColor, max.ForeColor, value);
            if (min.Halo != null && max.Halo != null)
                style.Halo = InterpolatePen(min.Halo, max.Halo, value);

            style.MinVisible = InterpolateDouble(min.MinVisible, max.MinVisible, value);
            style.MaxVisible = InterpolateDouble(min.MaxVisible, max.MaxVisible, value);
            style.Offset = new System.Drawing.PointF(InterpolateFloat(min.Offset.X, max.Offset.X, value), InterpolateFloat(min.Offset.Y, max.Offset.Y, value));
            return style;
        }
    }
}
