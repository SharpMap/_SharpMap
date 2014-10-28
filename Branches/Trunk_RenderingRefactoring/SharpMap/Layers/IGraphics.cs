using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using BruTile.Web.Wms;
using DotSpatial.Projections.Transforms;

namespace SharpMap.Layers
{
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

        void DrawImage(Image img, Rectangle rect);
        void DrawImage(Bitmap img, Rectangle rect);
        void DrawImage(Bitmap img, Point rect);
        void DrawImage(Bitmap img, float x, float y);
        void DrawImage(Image img, float x, float y, float w, float h);
        void DrawImage(Image img, Rectangle rect, float x, float y, float w, float h, GraphicsUnit unit, ImageAttributes attribs);
        void DrawImageUnscaled(Image img, int x, int y);

        void DrawLine(Pen pen, float x1, float y1, float x2, float y2);
        void DrawLines(Pen pen, PointF[] pts);

        void DrawPath(Pen pen, GraphicsPath path);
        void FillPath(Brush brush, GraphicsPath path);

        void DrawArc(Pen pen, RectangleF rect, float x, float y);

        void DrawPolygon(Pen pen, PointF[] pts);
        void FillPolygon(Brush brush, PointF[] pts);

        void DrawRectangle(Pen pen, float x, float y, float w, float h);
        void FillRectangle(Brush brush, float x, float y, float w, float h);
        void FillRectangles(Brush brush, RectangleF[] rects);

        void DrawEllipse(Pen pen, float x, float y, float w, float h);
        void FillEllipse(Brush brush, float x, float y, float w, float h);

        void DrawString(string text, Font font, Brush brush, float x, float y);
        void DrawString(string text, Font font, Brush brush, float x, float y, StringFormat format);
        void DrawString(string text, Font font, Brush brush, PointF pt, StringFormat format);
        void DrawString(string text, Font font, Brush brush, RectangleF rect);

        SizeF MeasureString(string text, Font font);
        SizeF MeasureString(string text, Font font, int letterSpacePercentage);

        Region[] MeasureCharacterRanges(string text, Font font, RectangleF rect, StringFormat format);

        IntPtr GetHdc();
        void ReleaseHdc(IntPtr hdc);

        Graphics GetNativeObject();        
    }
}