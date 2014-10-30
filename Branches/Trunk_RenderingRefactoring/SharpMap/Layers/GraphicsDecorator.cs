using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace SharpMap.Layers
{
    internal class GraphicsDecorator : IGraphics
    {
        private readonly Graphics _g;

        internal GraphicsDecorator(Graphics g)
        {
            if (g == null)
                throw new ArgumentNullException("g");

            _g = g;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                _g.Dispose();
        }

        public SmoothingMode SmoothingMode
        {
            get { return _g.SmoothingMode; }
            set { _g.SmoothingMode = value; }
        }

        public PixelOffsetMode PixelOffsetMode
        {
            get { return _g.PixelOffsetMode; }
            set { _g.PixelOffsetMode = value; }
        }

        public InterpolationMode InterpolationMode
        {
            get { return _g.InterpolationMode; }
            set { _g.InterpolationMode = value; }
        }

        public TextRenderingHint TextRenderingHint
        {
            get { return _g.TextRenderingHint; }
            set { _g.TextRenderingHint = value; }
        }

        public GraphicsUnit PageUnit
        {
            get { return _g.PageUnit; }
            set { _g.PageUnit = value; }
        }

        public float PageScale
        {
            get { return _g.PageScale; }
            set { _g.PageScale = value; }
        }

        public Region Clip
        {
            get { return _g.Clip; }
            set { _g.Clip = value; }
        }

        public RectangleF ClipBounds
        {
            get { return _g.ClipBounds; }
        }

        public RectangleF VisibleClipBounds
        {
            get { return _g.VisibleClipBounds; }
        }

        public Point RenderingOrigin
        {
            get { return _g.RenderingOrigin; }
            set { _g.RenderingOrigin = value; }
        }

        public float DpiX
        {
            get { return _g.DpiX; }
        }

        public float DpiY
        {
            get { return _g.DpiY; }
        }

        public Matrix Transform
        {
            get { return _g.Transform; }
            set { _g.Transform = value; }
        }

        public void TranslateTransform(float dx, float dy)
        {
            _g.TranslateTransform(dx, dy);
        }

        public void RotateTransform(float angle)
        {
            _g.RotateTransform(angle);
        }

        public void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, PointF[] pts)
        {
            _g.TransformPoints(destSpace, srcSpace, pts);
        }

        public void Clear(Color color)
        {
            _g.Clear(color);
        }

        public void FillRegion(Brush brush, Region region)
        {
            _g.FillRegion(brush, region);
        }

        public void DrawImage(Image img, Rectangle rect)
        {
            DrawImage(img, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawImage(Bitmap img, int x, int y)
        {
            _g.DrawImage(img, x, y);
        }

        public void DrawImage(Image img, int x, int y, int w, int h)
        {
            _g.DrawImage(img, x, y, w, h);
        }

        public void DrawImage(Image img, 
            int dstX, int dstY, int dstW, int dstH, 
            int srcX, int srcY, int srcW, int srcH, 
            GraphicsUnit unit, ImageAttributes attribs)
        {
            Rectangle destRect = new Rectangle(dstX, dstY, dstW, dstH);
            _g.DrawImage(img, destRect, srcX, srcY, srcW, srcH, unit, attribs);
        }

        public void DrawImageUnscaled(Image img, int x, int y)
        {
            _g.DrawImageUnscaled(img, x, y);
        }

        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
        {
            _g.DrawLine(pen, x1, y1, x2, y2);
        }

        public void DrawLines(Pen pen, PointF[] pts)
        {
            _g.DrawLines(pen, pts);
        }

        public void DrawPath(Pen pen, GraphicsPath path)
        {
            _g.DrawPath(pen, path);
        }

        public void FillPath(Brush brush, GraphicsPath path)
        {
            _g.FillPath(brush, path);
        }

        public void DrawArc(Pen pen, RectangleF rect, int x, int y)
        {
            _g.DrawArc(pen, rect, x, y);
        }

        public void DrawPolygon(Pen pen, PointF[] pts)
        {
            _g.DrawPolygon(pen, pts);
        }

        public void FillPolygon(Brush brush, PointF[] pts)
        {
            _g.FillPolygon(brush, pts);
        }

        public void DrawRectangle(Pen pen, int x, int y, int w, int h)
        {
            _g.DrawRectangle(pen, x, y, w, h);
        }

        public void FillRectangle(Brush brush, int x, int y, int w, int h)
        {
            _g.FillRectangle(brush, x, y, w, h);
        }

        public void FillRectangles(Brush brush, RectangleF[] rects)
        {
            _g.FillRectangles(brush, rects);
        }

        public void DrawEllipse(Pen pen, int x, int y, int w, int h)
        {
            _g.DrawEllipse(pen, x, y, w, h);
        }

        public void FillEllipse(Brush brush, int x, int y, int w, int h)
        {
            _g.FillEllipse(brush, x, y, w, h);
        }
        public void DrawString(string text, Font font, Brush brush, int x, int y)
        {
            _g.DrawString(text, font, brush, x, y);
        }

        public void DrawString(string text, Font font, Brush brush, int x, int y, StringFormat format)
        {
            _g.DrawString(text, font, brush, x, y, format);
        }

        public void DrawString(string text, Font font, Brush brush, PointF pt, StringFormat format)
        {
            _g.DrawString(text, font, brush, pt, format);
        }

        public void DrawString(string text, Font font, Brush brush, RectangleF rect)
        {
            _g.DrawString(text, font, brush, rect);
        }

        public SizeF MeasureString(string text, Font font)
        {
            return _g.MeasureString(text, font);
        }

        public SizeF MeasureString(string text, Font font, int letterSpacePercentage)
        {
            return _g.MeasureString(text, font, letterSpacePercentage);
        }

        public RectangleF MeasureCharacterRanges(string text, Font font, RectangleF rect, StringFormat format)
        {
            Region[] regions = _g.MeasureCharacterRanges(text, font, rect, format);
            return regions[0].GetBounds(_g);
        }

        public IntPtr GetHdc()
        {
            return _g.GetHdc();
        }

        public void ReleaseHdc(IntPtr hdc)
        {
            _g.ReleaseHdc(hdc);
        }        
    }
}