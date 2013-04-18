using System;
using System.Drawing;

namespace SharpMap.UI.Helpers
{
    public class GraphicsHelper
    {
        /// <summary>
        /// Draws selection rectangle, can be used for zoom, query
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="color"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        public static void DrawSelectionRectangle(Graphics graphics, Color color, PointF point1, PointF point2)
        {
            Rectangle rectangle = new Rectangle((int)Math.Min(point1.X, point2.X), (int)Math.Min(point1.Y, point2.Y),
                                                (int)Math.Abs(point1.X - point2.X), (int)Math.Abs(point1.Y - point2.Y));
            Pen pen = new Pen(color);
            graphics.DrawRectangle(pen, rectangle);
            pen.Dispose();

            Brush brush = new SolidBrush(Color.FromArgb(30, color));
            graphics.FillRectangle(brush, rectangle);
            brush.Dispose();
        }

        /// <summary>
        /// Draw a polyline using an array of points and fills the interior
        /// cfr Lasso Select in Paint.Net
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="color"></param>
        /// <param name="points"></param>
        public static void DrawSelectionLasso(Graphics graphics, Color color, PointF[] points)
        {
            if (points.Length < 2)
            {
                return;
            }
            Pen pen = new Pen(color);
            graphics.DrawCurve(pen, points);
            pen.Dispose();

            Brush brush = new SolidBrush(Color.FromArgb(30, color));
            graphics.FillClosedCurve(brush, points);
            brush.Dispose();
        }

        public static Bitmap CreateRectangleImage(Pen pen, Brush brush, int width, int height)
        {
            Bitmap bm = new Bitmap(width + 2, height + 2);
            Graphics graphics = Graphics.FromImage(bm);
            graphics.FillRectangle(brush, new Rectangle(2, 2, width, height));
            graphics.DrawRectangle(pen, new Rectangle(2, 2, width - 1, height - 1));
            graphics.Dispose();
            return bm;
        }
    }
}