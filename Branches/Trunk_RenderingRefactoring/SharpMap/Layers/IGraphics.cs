using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace SharpMap.Layers
{
#pragma warning disable 1591
    public interface IGraphics : IDisposable
    {
        SmoothingMode SmoothingMode { get; set; }
        PixelOffsetMode PixelOffsetMode { get; set; }
        InterpolationMode InterpolationMode { get; set; }
        TextRenderingHint TextRenderingHint { get; set; }

        GraphicsUnit PageUnit { get; set; }
        float PageScale { get; set; }

        Region Clip { get; set; }
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

        void FillRegion(Brush brush, Region region);

        void DrawImage(Bitmap img, int x, int y);
        void DrawImage(Image img, int x, int y, int w, int h);
        void DrawImage(Image img, 
            int dstX, int dstY, int dstW, int dstH, 
            int srcX, int srcY, int srcW, int srcH,
            GraphicsUnit unit, ImageAttributes attribs);
        void DrawImageUnscaled(Image img, int x, int y);

        void DrawLine(Pen pen, int x1, int y1, int x2, int y2);
        void DrawLines(Pen pen, PointF[] pts);

        void DrawPath(Pen pen, GraphicsPath path);
        void FillPath(Brush brush, GraphicsPath path);

        void DrawArc(Pen pen, RectangleF rect, int x, int y);

        void DrawPolygon(Pen pen, PointF[] pts);
        void FillPolygon(Brush brush, PointF[] pts);

        void DrawRectangle(Pen pen, int x, int y, int w, int h);
        void FillRectangle(Brush brush, int x, int y, int w, int h);
        void FillRectangles(Brush brush, RectangleF[] rects);

        void DrawEllipse(Pen pen, int x, int y, int w, int h);
        void FillEllipse(Brush brush, int x, int y, int w, int h);

        void DrawString(string text, Font font, Brush brush, int x, int y);
        void DrawString(string text, Font font, Brush brush, int x, int y, StringFormat format);
        void DrawString(string text, Font font, Brush brush, PointF pt, StringFormat format);
        void DrawString(string text, Font font, Brush brush, RectangleF rect);

        SizeF MeasureString(string text, Font font);
        SizeF MeasureString(string text, Font font, int letterSpacePercentage);        

        RectangleF MeasureCharacterRanges(string text, Font font, RectangleF rect, StringFormat format);

        IntPtr GetHdc();
        void ReleaseHdc(IntPtr hdc);
    }
#pragma warning restore 1591
}