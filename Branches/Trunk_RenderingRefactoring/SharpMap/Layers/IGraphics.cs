using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SharpMap.Layers
{
#pragma warning disable 1591
    public interface IGraphics : IDisposable
    {
        Smoothing SmoothingMode { get; set; }
        PixelOffset PixelOffsetMode { get; set; }
        Interpolation InterpolationMode { get; set; }
        TextRendering TextRenderingHint { get; set; }
        Compositing CompositingMode { get; set; }

        GraphicsUnitType PageUnit { get; set; }
        float PageScale { get; set; }

        RectangleF Clip { get; set; }
        RectangleF ClipBounds { get; }
        RectangleF VisibleClipBounds { get; }

        Point RenderingOrigin { get; set; }

        float DpiX { get; }
        float DpiY { get; }

        Matrix Transform { get; set; }
        
        void TranslateTransform(float dx, float dy);
        void RotateTransform(float angle);

        void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, PointF[] pts);

        void Clear(Color color);

        void DrawImage(Bitmap img, int x, int y);
        void DrawImage(Image img, int x, int y, int w, int h);
        void DrawImage(Image img,
            int dstX, int dstY, int dstW, int dstH,
            int srcX, int srcY, int srcW, int srcH,
            GraphicsUnitType unit);
        void DrawImage(Image img,
            int dstX, int dstY, int dstW, int dstH,
            int srcX, int srcY, int srcW, int srcH,
            GraphicsUnitType unit, ImageAttributes ia);
        void DrawImage(Image img,
            Point[] destPoints,
            int srcX, int srcY, int srcW, int srcH,
            GraphicsUnitType unit, ImageAttributes ia);
        void DrawImageUnscaled(Image img, int x, int y);

        void DrawLines(Pen pen, PointF[] pts);

        void DrawPath(Pen pen, GraphicsPath path);
        void FillPath(Brush brush, GraphicsPath path);

        void DrawArc(Pen pen, RectangleF rect, float x, float y);

        void DrawPolygon(Pen pen, PointF[] pts);
        void FillPolygon(Brush brush, PointF[] pts);

        void DrawRectangle(Pen pen, int x, int y, int w, int h);
        void FillRectangle(Brush brush, int x, int y, int w, int h);
        void FillRectangles(Brush brush, RectangleF[] rects);

        void DrawEllipse(Pen pen, int x, int y, int w, int h);
        void FillEllipse(Brush brush, int x, int y, int w, int h);

        void DrawPie(Pen pen, Rectangle rect, int startAngle, int sweepAngle);
        void FillPie(Brush brush, Rectangle rect, int startAngle, int sweepAngle);

        void DrawString(string text, Font font, Brush brush, int x, int y);
        void DrawString(string text, Font font, Brush brush, int x, int y, StringFormat format);        
        void DrawString(string text, Font font, Brush brush, RectangleF rect);
        void DrawString(string text, Font font, Brush brush, RectangleF rect, StringFormat format);

        SizeF MeasureString(string text, Font font);
        SizeF MeasureString(string text, Font font, int letterSpacePercentage);        

        RectangleF MeasureCharacterRanges(string text, Font font, RectangleF rect, StringFormat format);

        IntPtr GetHdc();
        void ReleaseHdc(IntPtr hdc);

        void Flush();
    }

#pragma warning restore 1591
}