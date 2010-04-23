using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpMap.Styles;
using System.IO;

namespace SharpMap.Rendering
{
    public static class GdiStyle
    {
        public static System.Drawing.Color Convert(this Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static System.Drawing.Pen Convert(this Pen pen)
        {
            return new System.Drawing.Pen(pen.Color.Convert(), pen.Width);
        }

        public static System.Drawing.Brush Convert(this Brush brush)
        {
            return new System.Drawing.SolidBrush(brush.Fill.Convert());
        }

        public static System.Drawing.Bitmap Convert(this Bitmap bitmap)
        {
            return new System.Drawing.Bitmap(bitmap.data);
        }

        public static System.Drawing.PointF Convert(this Offset offset)
        {
            return new System.Drawing.PointF(offset.X, offset.Y);
        }
    }
}
