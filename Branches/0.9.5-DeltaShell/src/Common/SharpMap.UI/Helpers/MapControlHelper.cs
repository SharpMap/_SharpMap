using System.Drawing;
using System.Drawing.Imaging;
using SharpMap.Styles;

namespace SharpMap.UI.Helpers
{
    public class MapControlHelper // TODO: it looks more like Map helper in SharpMap ? 
    {
        /// <summary>
        /// Modifies a Vectorstyle to "highlight" during operation (eg. moving features)
        /// </summary>
        /// <param name="vectorStyle"></param>
        /// <param name="good"></param>
        public static void PimpStyle(VectorStyle vectorStyle, bool good)
        {
            vectorStyle.Line.Color = Color.FromArgb(128, vectorStyle.Line.Color);
            SolidBrush solidBrush = vectorStyle.Fill as SolidBrush;
            if (null != solidBrush)
                vectorStyle.Fill = new SolidBrush(Color.FromArgb(127, solidBrush.Color));
            else // possibly a multicolor brush
                vectorStyle.Fill = new SolidBrush(Color.FromArgb(63, Color.DodgerBlue));
            if (null != vectorStyle.Symbol)
            {
                Bitmap bitmap = new Bitmap(vectorStyle.Symbol.Width, vectorStyle.Symbol.Height);
                Graphics graphics = Graphics.FromImage(bitmap);
                ColorMatrix colorMatrix;
                if (good)
                {
                    colorMatrix = new ColorMatrix(new float[][]
                    {
                        new float[] {1.0f, 0.0f, 0.0f, 0.0f, 0.0f}, // red scaling of 1
                        new float[] {0.0f, 1.0f, 0.0f, 0.0f, 0.0f}, // green scaling of 1
                        new float[] {0.0f, 0.0f, 1.0f, 0.0f, 0.0f}, // blue scaling of 1
                        new float[] {0.0f, 0.0f, 0.0f, 0.5f, 0.0f}, // alpha scaling of 0.5
                        new float[] {0.0f, 0.0f, 0.0f, 0.0f, 1.0f}
                    });
                }
                else
                {
                    colorMatrix = new ColorMatrix(new float[][]
                    {
                        new float[] {2.0f, 0.0f, 0.0f, 0.0f, 0.0f}, // red scaling of 2
                        new float[] {0.0f, 1.0f, 0.0f, 0.0f, 0.0f}, // green scaling of 1
                        new float[] {0.0f, 0.0f, 1.0f, 0.0f, 0.0f}, // blue scaling of 1
                        new float[] {0.0f, 0.0f, 0.0f, 0.5f, 0.0f}, // alpha scaling of 0.5
                        new float[] {1.0f, 0.0f, 0.0f, 0.0f, 1.0f}
                    });
                }

                ImageAttributes imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(colorMatrix);
                graphics.DrawImage(vectorStyle.Symbol,
                    new Rectangle(0, 0, vectorStyle.Symbol.Width, vectorStyle.Symbol.Height), 0, 0,
                    vectorStyle.Symbol.Width, vectorStyle.Symbol.Height, GraphicsUnit.Pixel, imageAttributes);
                graphics.Dispose();
                vectorStyle.Symbol = bitmap;
            }
        }
    }
}
