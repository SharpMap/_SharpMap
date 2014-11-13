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

        public Smoothing SmoothingMode
        {
            get { return (Smoothing)_g.SmoothingMode; }
            set { _g.SmoothingMode = (SmoothingMode)value; }
        }

        public PixelOffset PixelOffsetMode
        {
            get { return (PixelOffset)_g.PixelOffsetMode; }
            set { _g.PixelOffsetMode = (PixelOffsetMode)value; }
        }

        public Interpolation InterpolationMode
        {
            get { return (Interpolation)_g.InterpolationMode; }
            set { _g.InterpolationMode = (InterpolationMode)value; }
        }

        public TextRendering TextRenderingHint
        {
            get { return (TextRendering)_g.TextRenderingHint; }
            set { _g.TextRenderingHint = (TextRenderingHint)value; }
        }

        public Compositing CompositingMode
        {
            get { return (Compositing)_g.CompositingMode; }
            set { _g.CompositingMode = (CompositingMode)value; }
        }

        public GraphicsUnitType PageUnit
        {
            get { return (GraphicsUnitType)_g.PageUnit; }
            set { _g.PageUnit = (GraphicsUnit)value; }
        }

        public float PageScale
        {
            get { return _g.PageScale; }
            set { _g.PageScale = value; }
        }

        public RectangleF Clip
        {
            get { return _g.Clip.GetBounds(_g); }
            set { _g.Clip = new Region(value); }
        }

        public RectangleF ClipBounds
        {
            get { return _g.ClipBounds; }
        }

        public RectangleF VisibleClipBounds
        {
            get { return _g.VisibleClipBounds; }
        }

        public PointStruct RenderingOrigin
        {
            get
            {
                Point p = _g.RenderingOrigin;
                return new PointStruct(p.X, p.Y);
            }
            set
            {
                int x = Convert.ToInt32(value.X);
                int y = Convert.ToInt32(value.Y);
                _g.RenderingOrigin = new Point(x, y);
            }
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

        public void TransformPoints(CoordinateSpaceType destSpace, CoordinateSpaceType srcSpace, PointF[] pts)
        {
            _g.TransformPoints((CoordinateSpace) destSpace, (CoordinateSpace) srcSpace, pts);
        }

        public void Clear(Color color)
        {
            _g.Clear(color);
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
            GraphicsUnitType unit)
        {
            Rectangle destRect = new Rectangle(dstX, dstY, dstW, dstH);
            _g.DrawImage(img,
                destRect,
                srcX, srcY, srcW, srcH,
                (GraphicsUnit)unit);
        }

        public void DrawImage(Image img,
            int dstX, int dstY, int dstW, int dstH,
            int srcX, int srcY, int srcW, int srcH,
            GraphicsUnitType unit, ImageAttributes ia)
        {
            Rectangle destRect = new Rectangle(dstX, dstY, dstW, dstH);
            _g.DrawImage(img,
                destRect,
                srcX, srcY, srcW, srcH,
                (GraphicsUnit)unit, ia);
        }

        public void DrawImage(Image img,
            PointF[] destPoints,
            int srcX, int srcY, int srcW, int srcH,
            GraphicsUnitType unit, ImageAttributes ia)
        {
            Rectangle srcRect = new Rectangle(srcX, srcY, srcW, srcH);
            _g.DrawImage(img, destPoints, srcRect, (GraphicsUnit)unit, ia);
        }

        public void DrawImageUnscaled(Image img, int x, int y)
        {
            _g.DrawImageUnscaled(img, x, y);
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

        public void DrawArc(Pen pen, RectangleF rect, float x, float y)
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

        public void DrawPie(Pen pen, RectangleF rect, int startAngle, int sweepAngle)
        {
            _g.DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void FillPie(Brush brush, RectangleF rect, int startAngle, int sweepAngle)
        {
            _g.FillPie(brush, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void DrawString(string text, Font font, Brush brush, int x, int y)
        {
            _g.DrawString(text, font, brush, x, y);
        }

        public void DrawString(string text, Font font, Brush brush, int x, int y, StringFormat format)
        {
            _g.DrawString(text, font, brush, x, y, format);
        }

        public void DrawString(string text, Font font, Brush brush, RectangleF rect)
        {
            _g.DrawString(text, font, brush, rect);
        }

        public void DrawString(string text, Font font, Brush brush, RectangleF rect, StringFormat format)
        {
            _g.DrawString(text, font, brush, rect, format);
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

        public void Flush()
        {
            _g.Flush();
        }
    }
}