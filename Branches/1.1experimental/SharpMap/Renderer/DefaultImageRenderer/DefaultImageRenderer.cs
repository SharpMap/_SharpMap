using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SharpMap.Layers;
using System.Reflection;
using System.Reflection.Emit;
using SharpMap.Renderer.DefaultImage;

namespace SharpMap.Renderer
{
    public class DefaultImageRenderer
        : IMapRenderer<Image>
    {
        /// <remarks>Implementors should provide a default constructor</remarks> 
        public interface ILayerRenderer
        {
            void RenderLayer(ILayer layer, Map map, Graphics g);
        }


        /// <remarks>Implementors should provide a default constructor</remarks> 
        public interface ILayerRenderer<TLayerType>
            : ILayerRenderer
            where TLayerType : ILayer
        {
            void RenderLayer(TLayerType layer, Map map, Graphics g);
        }

        public event EventHandler RenderDone;
        public event EventHandler<LayerRenderedEventArgs> LayerRendered;

        private void OnLayerRendered(ILayer lyr)
        {
            if (this.LayerRendered != null)
                this.LayerRendered(this, new LayerRenderedEventArgs(lyr));
        }

        private void OnRenderDone()
        {
            if (this.RenderDone != null)
                this.RenderDone(this, EventArgs.Empty);
        }

        #region IMapRenderer<Image> Members

        public Image Render(Map map)
        {
            if (map.Layers == null || map.Layers.Count == 0)
                throw new InvalidOperationException("No layers to render");

            Image img = new Bitmap(map.Size.Width, map.Size.Height);
            using (Graphics g = Graphics.FromImage(img))
            {
                g.Transform = map.MapTransform;
                g.Clear(map.BackColor);
                g.PageUnit = GraphicsUnit.Pixel;
                int SRID = (map.Layers.Count > 0 ? map.Layers[0].SRID : -1); //Get the SRID of the first layer
                for (int i = 0; i < map.Layers.Count; i++)
                {
                    ILayer lyr = map.Layers[i];
                    if (lyr.Enabled
                        && lyr.MaxVisible >= map.Zoom
                        && lyr.MinVisible < map.Zoom)
                        RenderLayer(lyr, map, g);
                }

                OnRenderDone();
            }

            return img;
        }

        private void RenderLayer(ILayer lyr, Map m, Graphics g)
        {
            if (lyr is LayerGroup)
            {
                foreach (ILayer l in ((LayerGroup)lyr).Layers)
                    RenderLayer(lyr, m, g);
            }
            else
            {
                DefaultImageRenderer.ILayerRenderer helper =
                    DefaultImageRenderer
                    .LayerRendererTypeMap
                    .GetRenderer(lyr.GetType());

                helper.RenderLayer(lyr, m, g);
            }
            OnLayerRendered(lyr);
        }


        /// <summary>
        /// Poor mans IoC Container - renderers are stored against the Layer type that they serve. 
        /// This is a static object so only needs to be configured at app start up.
        /// In future this may need to be looked at as some inplementations may require changing a given renderer on a render by render basis.
        /// </summary>
        public static class LayerRendererTypeMap
        {
            private static readonly Dictionary<Type, Delegate> _layerRendererTypeMap;
            delegate DefaultImageRenderer.ILayerRenderer LayerRendererFactory();

            static LayerRendererTypeMap()
            {
                _layerRendererTypeMap = new Dictionary<Type, Delegate>();
                //add some default renderers
                RegisterLayerRenderer<VectorLayer>(typeof(DefaultVectorRenderer));
                RegisterLayerRenderer<LabelLayer>(typeof(DefaultLabelRenderer));
                RegisterLayerRenderer<WmsLayer>(typeof(DefaultWmsRenderer));
                RegisterLayerRenderer<TiledWmsLayer>(typeof(DefaultTiledWmsRenderer));

            }
            /// <summary>
            /// registers <paramref name="helper"/> as the renderer for <typeparamref name="TLayer"/>
            /// </summary>
            /// <typeparam name="TLayer">the type of Layer to be rendered</typeparam>
            /// <param name="helper">The type to delegate rendering to</param>
            public static void RegisterLayerRenderer<TLayer>(Type rendererType) where TLayer : ILayer
            {
                lock (_layerRendererTypeMap)
                {
                    if (!_layerRendererTypeMap.ContainsKey(typeof(TLayer)))
                        _layerRendererTypeMap.Add(typeof(TLayer), CreateDynamicConstructor<TLayer>(rendererType));
                    else
                    {
                        _layerRendererTypeMap[typeof(TLayer)] = CreateDynamicConstructor<TLayer>(rendererType);
                    }
                }
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="t">The type of the Layer being rendered</param>
            /// <returns></returns>
            public static DefaultImageRenderer.ILayerRenderer GetRenderer(Type t)
            {
                if (!_layerRendererTypeMap.ContainsKey(t))
                    throw new InvalidOperationException(string.Format("No renderer is configured for layer type {0}", t));

                return ((LayerRendererFactory)_layerRendererTypeMap[t])();
            }

            public static DefaultImageRenderer.ILayerRenderer<TLayer> GetRenderer<TLayer>()
                where TLayer : ILayer
            {
                return (DefaultImageRenderer.ILayerRenderer<TLayer>)GetRenderer(typeof(TLayer));
            }

            static LayerRendererFactory CreateDynamicConstructor<TLayer>(Type t)
                where TLayer : ILayer
            {
                ConstructorInfo ci = t.GetConstructor(Type.EmptyTypes);
                DynamicMethod dm = new DynamicMethod(string.Format("create {0}", t.FullName), t, null, true);
                ILGenerator gen = dm.GetILGenerator();
                gen.Emit(OpCodes.Newobj, ci);
                gen.Emit(OpCodes.Ret);
                return (LayerRendererFactory)dm.CreateDelegate(typeof(LayerRendererFactory), null);
            }
        }


        #endregion

        #region IMapRenderer Members


        object IMapRenderer.Render(Map map)
        {
            return this.Render(map);
        }

        #endregion
    }
}
