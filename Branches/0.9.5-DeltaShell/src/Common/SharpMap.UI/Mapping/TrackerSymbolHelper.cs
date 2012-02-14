using System.Drawing;

namespace SharpMap.UI.Mapping
{
    public class TrackerSymbolHelper
    {
        /// <summary>
        /// GenerateSimpleTrackerImage creates a rectangular image. Please note
        /// the offset of 2 pixels to counter a mismath in sharpmap?
        /// </summary>
        /// <param name="pen"></param>
        /// <param name="brush"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Bitmap GenerateSimple(Pen pen, Brush brush, int width, int height)
        {
            Bitmap bm = new Bitmap(width + 2, height + 2);
            Graphics graphics = Graphics.FromImage(bm);
            graphics.FillRectangle(brush, new Rectangle(2, 2, width, height));
            graphics.DrawRectangle(pen, new Rectangle(2, 2, width - 1, height - 1));
            graphics.Dispose();
            return bm;
        }
        public static Bitmap GenerateComposite(Pen pen, Brush brush, int totalWidth, int totaHeight, 
                                int width, int height)
        {
            Bitmap bm = new Bitmap(totalWidth + 2, totaHeight + 2);
            Graphics graphics = Graphics.FromImage(bm);

            graphics.DrawRectangle(pen, new Rectangle(2, 2, totalWidth - 1, totaHeight - 1));

            graphics.FillRectangle(brush, new Rectangle(2, 2, width, height));
            graphics.DrawRectangle(pen, new Rectangle(2, 2, width - 1, height - 1));

            graphics.FillRectangle(brush, new Rectangle(totalWidth - width + 2, 2, width, height));
            graphics.DrawRectangle(pen, new Rectangle(totalWidth - width + 2, 2, width - 1, height - 1));

            graphics.FillRectangle(brush, new Rectangle(totalWidth - width + 2, totaHeight - height + 2, width, height));
            graphics.DrawRectangle(pen, new Rectangle(totalWidth - width + 2, totaHeight - height + 2, width - 1, height - 1));

            graphics.FillRectangle(brush, new Rectangle(2, totaHeight - height + 2, width, height));
            graphics.DrawRectangle(pen, new Rectangle(2, totaHeight - height + 2, width - 1, height - 1));

            graphics.Dispose();
            return bm;
        }
    }
}
