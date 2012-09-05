using System.Drawing;

namespace DelftTools.Utils.Drawing
{
    public static class ImageExtensions // TODO: move to helpers / utils
    {
        public static bool PixelsEqual(this Image image1, Image image2)
        {
            if ((image1 == null && image2 != null) || (image1 != null && image2 == null))
            {
                return false;
            }

            if(image1 == image2)
            {
                return true;
            }

            if(!(image1 is Bitmap) || !(image2 is Bitmap))
            {
                return false;
            }

            var bitmap1 = (Bitmap)image1;
            var bitmap2 = (Bitmap)image2;

            if(bitmap1.Width != bitmap2.Width || bitmap1.Height != bitmap2.Height)
            {
                return false;
            }

            for (var i = 0; i < bitmap1.Width; i++)
            {
                for (var j = 0; j < bitmap1.Height; j++)
                {
                    if(!bitmap1.GetPixel(i, j).Equals(bitmap2.GetPixel(i, j)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}