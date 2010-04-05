using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Styles;

namespace SharpMap.Rendering
{
    public class GdiRenderer : IRenderer
    {
        Graphics graphics;
        Image image;

        public Graphics Graphics
        {
            set { graphics = value; }
        }

        public GdiRenderer()
        {
        }

        public void Render(IProvider DataSource, Func<FeatureDataRow, IStyle> getStyle, ICoordinateTransformation CoordinateTransformation, IMapTransform mapTansform)
        {
            if (graphics == null) throw new ApplicationException("Graphics was not initialized");
            RendererHelper.Render(graphics, DataSource, getStyle, CoordinateTransformation, mapTansform);
        }
        
        private void Initialize(IMapTransform transform)
        {
            image = new System.Drawing.Bitmap((int)transform.Width, (int)transform.Height);
            graphics = System.Drawing.Graphics.FromImage(image);
        }

        public Image GetMapAsImage(IMapTransform transform, Map map)
        {
            Initialize(transform); //TODO: only initilize when needed
            graphics.Clear(map.BackColor.Convert());
            graphics.PageUnit = System.Drawing.GraphicsUnit.Pixel;
            map.Render(this, transform);
            return image;
        }

        public byte[] GetMapAsByteArray(IMapTransform transform, Map map)
        {
            Image image = GetMapAsImage(transform, map);
            MemoryStream memoryStream = new MemoryStream();
            image.Save(memoryStream, ImageFormat.Bmp);
            return memoryStream.ToArray();
        }
    }
}
