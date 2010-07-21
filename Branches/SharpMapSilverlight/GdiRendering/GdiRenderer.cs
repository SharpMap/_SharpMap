using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using GdiRendering;
using SharpMap.Layers;

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

        public void Render(IView view, Map map)
        {
            foreach (var layer in map.Layers)
            {
                if (layer.Enabled &&
                    layer.MinVisible <= view.Resolution &&
                    layer.MaxVisible >= view.Resolution)
                {
                    if (layer is Layer) RenderLayer(view, layer as Layer);
                    //!!!else if (layer is LabelLayer) RenderLabelLayer(view, layer);
                }
            }        
        }

        private void RenderLayer(IView view, Layer layer)
        {
            if (graphics == null) throw new Exception("Graphics was not initialized");
            RendererHelper.RenderLayer(graphics, layer.DataSource, CreateStyleMethod(layer.Style, layer.Theme), layer.CoordinateTransformation, view);
        }

        private static Func<IFeature, IStyle> CreateStyleMethod(IStyle style, ITheme theme)
        {
            if (theme == null) return (row) => style;

            return (row) => theme.GetStyle(row);
        }

        private void Initialize(IView view)
        {
            if ((view.Width <= 0) || (view.Height <= 0)) return;
            image = new System.Drawing.Bitmap((int)view.Width, (int)view.Height);
            graphics = System.Drawing.Graphics.FromImage(image);
        }

        public Image GetMapAsImage(IView view, Map map)
        {
            Initialize(view); //TODO: only initilize when needed
            graphics.Clear(map.BackColor.Convert());
            graphics.PageUnit = System.Drawing.GraphicsUnit.Pixel;
            Render(view, map);
            return image;
        }

        public byte[] GetMapAsByteArray(IView view, Map map)
        {
            Image image = GetMapAsImage(view, map);
            MemoryStream memoryStream = new MemoryStream();
            image.Save(memoryStream, ImageFormat.Bmp);
            return memoryStream.ToArray();
        }

        #region IRenderer Members


        private void RenderLabelLayer(IView view, IProvider dataSource, SharpMap.Layers.LabelLayer labelLayer)
        {
            if (graphics == null) throw new Exception("Graphics was not initialized");
            LabelRenderer.Render(graphics, view, dataSource, labelLayer);
        }

        #endregion
    }
}