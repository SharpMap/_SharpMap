using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using GdiPoint = System.Drawing.Point;
using GdiSize = System.Drawing.Size;
using GdiRectangle = System.Drawing.Rectangle;
using GdiPen = System.Drawing.Pen;
using GdiBrush = System.Drawing.Brush;
using GdiBrushes = System.Drawing.Brushes;
using GdiFont = System.Drawing.Font;
using GdiFontFamily = System.Drawing.FontFamily;
using GdiFontStyle = System.Drawing.FontStyle;
using GdiColor = System.Drawing.Color;
using GdiGraphicsPath = System.Drawing.Drawing2D.GraphicsPath;
using GdiMatrix = System.Drawing.Drawing2D.Matrix;
using GdiColorMatrix = System.Drawing.Imaging.ColorMatrix;
using GdiSmoothingMode = System.Drawing.Drawing2D.SmoothingMode;
using GdiTextRenderingHint = System.Drawing.Text.TextRenderingHint;

using SharpMap.Rendering;
using SharpMap.Styles;

namespace SharpMap.Rendering.Gdi
{
    public static class ViewConverter
    {
        #region GdiToView
        public static ViewRectangle2D GdiToView(GdiRectangle rectangle)
        {
            return new ViewRectangle2D(rectangle.X, rectangle.Y,
                rectangle.Width, rectangle.Height);
        }

        public static ViewRectangle2D GdiToView(RectangleF rectangleF)
        {
            return new ViewRectangle2D(rectangleF.Left, rectangleF.Right, rectangleF.Top, rectangleF.Bottom);
        }

        public static ViewSize2D GdiToView(SizeF sizeF)
        {
            return new ViewSize2D(sizeF.Width, sizeF.Height);
        }

        public static ViewPoint2D GdiToView(GdiPoint point)
        {
            return new ViewPoint2D(point.X, point.Y);
        }
        #endregion

        #region ViewToGdi
        public static PointF[] ViewToGdi(IEnumerable<ViewPoint2D> viewPoints)
        {
            List<PointF> gdiPoints = new List<PointF>();
            foreach (ViewPoint2D viewPoint in viewPoints)
                gdiPoints.Add(ViewToGdi(viewPoint));

            return gdiPoints.ToArray();
        }

        public static GdiRectangle ViewToGdi(ViewRectangle2D rectangle)
        {
            return new GdiRectangle((int)rectangle.X, (int)rectangle.Y,
                (int)rectangle.Width, (int)rectangle.Height);
        }

        public static GdiFontStyle ViewToGdi(SharpMap.Styles.FontStyle fontStyle)
        {
            return (GdiFontStyle)(int)(fontStyle);
        }

        public static GdiMatrix ViewToGdi(ViewMatrix2D viewMatrix)
        {
            GdiMatrix gdiMatrix = new GdiMatrix(
                (float)viewMatrix.Elements[0, 0],
                (float)viewMatrix.Elements[0, 1],
                (float)viewMatrix.Elements[1, 0],
                (float)viewMatrix.Elements[1, 1],
                (float)viewMatrix.Elements[2, 0],
                (float)viewMatrix.Elements[2, 1]);

            return gdiMatrix;
        }

        public static GdiColorMatrix ViewToGdi(ColorMatrix colorMatrix)
        {
            GdiColorMatrix gdiColorMatrix = new GdiColorMatrix();
            gdiColorMatrix.Matrix00 = (float)colorMatrix.R;
            gdiColorMatrix.Matrix11 = (float)colorMatrix.G;
            gdiColorMatrix.Matrix22 = (float)colorMatrix.B;
            gdiColorMatrix.Matrix33 = (float)colorMatrix.A;
            gdiColorMatrix.Matrix40 = (float)colorMatrix.RedShift;
            gdiColorMatrix.Matrix41 = (float)colorMatrix.GreenShift;
            gdiColorMatrix.Matrix42 = (float)colorMatrix.BlueShift;
            gdiColorMatrix.Matrix43 = (float)colorMatrix.AlphaShift;
            gdiColorMatrix.Matrix44 = 1;
            return gdiColorMatrix;
        }

        public static GdiColor ViewToGdi(StyleColor color)
        {
            return GdiColor.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static GdiFontFamily ViewToGdi(SharpMap.Styles.FontFamily fontFamily)
        {
            if (fontFamily == null)
                return null;

            return new GdiFontFamily(fontFamily.Name);
        }

        public static GdiPen ViewToGdi(StylePen pen)
        {
            if (pen == null)
                return null;

            GdiPen gdiPen = new GdiPen(ViewConverter.ViewToGdi(pen.BackgroundBrush), (float)pen.Width);
            gdiPen.Alignment = (System.Drawing.Drawing2D.PenAlignment)(int)pen.Alignment;
            gdiPen.CompoundArray = pen.CompoundArray;
            //gdiPen.CustomEndCap = new System.Drawing.Drawing2D.CustomLineCap();
            //gdiPen.CustomStartCap = new System.Drawing.Drawing2D.CustomLineCap();
            gdiPen.DashCap = (System.Drawing.Drawing2D.DashCap)(int)pen.DashCap;
            gdiPen.DashOffset = pen.DashOffset;
            gdiPen.DashPattern = pen.DashPattern;
            gdiPen.DashStyle = (System.Drawing.Drawing2D.DashStyle)(int)pen.DashStyle;
            gdiPen.EndCap = (System.Drawing.Drawing2D.LineCap)(int)pen.EndCap;
            gdiPen.LineJoin = (System.Drawing.Drawing2D.LineJoin)(int)pen.LineJoin;
            gdiPen.MiterLimit = pen.MiterLimit;
            //gdiPen.PenType = System.Drawing.Drawing2D.PenType...
            gdiPen.StartCap = (System.Drawing.Drawing2D.LineCap)(int)pen.StartCap;
            gdiPen.Transform = ViewConverter.ViewToGdi(pen.Transform);

            return gdiPen;
        }

        public static GdiBrush ViewToGdi(StyleBrush brush)
        {
            if (brush == null)
                return null;

            // TODO: need to accomodate other types of view brushes
            SolidBrush gdiBrush = new SolidBrush(ViewConverter.ViewToGdi(brush.Color));
            return gdiBrush;
        }

        public static PointF ViewToGdi(ViewPoint2D viewPoint)
        {
            PointF gdiPoint = new PointF((float)viewPoint.X, (float)viewPoint.Y);
            return gdiPoint;
        }

        public static SizeF ViewToGdi(ViewSize2D viewSize)
        {
            SizeF gdiSize = new SizeF((float)viewSize.Width, (float)viewSize.Height);
            return gdiSize;
        }
        #endregion
    }
}
