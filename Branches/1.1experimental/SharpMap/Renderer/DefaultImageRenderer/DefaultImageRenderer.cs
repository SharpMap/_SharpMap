/*
 *	This file is part of SharpMap
 *  SharpMap is free software. This file © 2008 Newgrove Consultants Limited, 
 *  http://www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 *  
 *  Portions based on earlier work.
 * 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using SharpMap.Layers;
using SharpMap.Renderer.DefaultImage;

namespace SharpMap.Renderer
{
    public class DefaultImageRenderer
        : IMapRenderer<Image>, IAsyncMapRenderer<Image>
    {


        private static ImageCodecInfo _defaultCodec;
        private static ImageCodecInfo GetDefaultCodec()
        {
            if (_defaultCodec == null)
            {
                _defaultCodec = FindCodec("image/png");
            }
            return _defaultCodec;
        }

        public static ImageCodecInfo FindCodec(string mimeType)
        {
            foreach (ImageCodecInfo i in ImageCodecInfo.GetImageEncoders())
            {
                if (i.MimeType == mimeType)
                {
                    return i;
                }
            }

            return null;
        }


        private ImageCodecInfo _imageCodecInfo;
        public ImageCodecInfo ImageCodec
        {
            get
            {
                if (_imageCodecInfo == null)
                    _imageCodecInfo = GetDefaultCodec();

                return _imageCodecInfo;
            }
            set
            {
                _imageCodecInfo = value;
            }
        }

        public ImageFormat ImageFormat
        {
            get
            {
                return new ImageFormat(_imageCodecInfo.FormatID);
            }
            set
            {
                foreach (ImageCodecInfo i in ImageCodecInfo.GetImageEncoders())
                {
                    if (i.FormatID == value.Guid)
                    {
                        _imageCodecInfo = i;
                        break;
                    }
                }
            }
        }

        private EncoderParameters _encoderParams;
        public EncoderParameters EncoderParams
        {
            get
            {
                return _encoderParams;
            }
            set
            {
                _encoderParams = value;
            }
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

        public Image Render(Map map, out string mimeType)
        {
            Debug.WriteLine(string.Format("Render Thread is {0}", Thread.CurrentThread.ManagedThreadId));

            if (map.Layers == null || map.Layers.Count == 0)
                throw new InvalidOperationException("No layers to render");

            Image img = new Bitmap(map.Size.Width, map.Size.Height);
            using (Graphics g = Graphics.FromImage(img))
            {
                g.Transform = map.MapTransform;
                g.Clear(map.BackColor);
                g.PageUnit = GraphicsUnit.Pixel;
                //int SRID = (map.Layers.Count > 0 ? map.Layers[0].SRID : -1); //Get the SRID of the first layer
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
            mimeType = "image/bitmap";
            return img;
        }

        Stream IMapRenderer.Render(Map map, out string mimeType)
        {
            Debug.WriteLine(string.Format("Render Thread is {0}", Thread.CurrentThread.ManagedThreadId));



            MemoryStream ms = new MemoryStream();
            Image im = this.Render(map, out mimeType);
            im.Save(ms, ImageCodec, EncoderParams);
            mimeType = ImageCodec.MimeType;

            ms.Position = 0;
            return ms;
        }

        #endregion

        #region IAsyncMapRenderer<Image> Members

        public IAsyncResult Render(Map map, AsyncRenderCallbackDelegate<Image> callback)
        {
            Debug.WriteLine(string.Format("Calling Thread is {0}", Thread.CurrentThread.ManagedThreadId));

            InternalAsyncRenderDelegate<Image> dlgt = new InternalAsyncRenderDelegate<Image>(
                delegate(Map m, AsyncRenderCallbackDelegate<Image> call)
                {
                    string mime;
                    Image im = this.Render(map, out mime);
                    callback(im, mime);

                });
            return dlgt.BeginInvoke(map, callback, null, null);
        }

        #endregion

        #region IAsyncMapRenderer Members

        public IAsyncResult Render(Map map, AsyncRenderCallbackDelegate callback)
        {
            Debug.WriteLine(string.Format("Calling Thread is {0}", Thread.CurrentThread.ManagedThreadId));

            InternalAsyncRenderDelegate dlgt = new InternalAsyncRenderDelegate(
                delegate(Map m, AsyncRenderCallbackDelegate call)
                {
                    string mime;
                    Stream s = ((IMapRenderer)this).Render(map, out mime);
                    callback(s, mime);

                });
            return dlgt.BeginInvoke(map, callback, null, null);

        }

        #endregion



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

        /// <summary>
        /// Poor mans IoC Container - renderers are stored against the Layer type that they serve. 
        /// This is a static object so only needs to be configured at app start up.
        /// In future this may need to be looked at as some inplementations may require changing a given renderer on a render by render basis.
        /// 
        /// Also: It may make more sense to introduce a dependency on another project rather than recreate it here! 
        /// possibly Unity which is being referenced by some 'higher level' sharpmap projects?
        /// </summary>
        public static class LayerRendererTypeMap
        {
            private static readonly Dictionary<Type, Delegate> _layerRendererTypeMap;
            delegate DefaultImageRenderer.ILayerRenderer LayerRendererFactory();

            static LayerRendererTypeMap()
            {
                _layerRendererTypeMap = new Dictionary<Type, Delegate>();
                //add some default renderers
                RegisterLayerRenderer<IVectorLayer>(typeof(DefaultVectorRenderer));
                RegisterLayerRenderer<ILabelLayer>(typeof(DefaultLabelRenderer));
                RegisterLayerRenderer<IGdiRasterLayer>(typeof(DefaultGdiRasterRenderer));
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
                {
                    foreach (Type t2 in t.GetInterfaces())
                    {
                        if (_layerRendererTypeMap.ContainsKey(t2))
                        {
                            lock (_layerRendererTypeMap)
                                _layerRendererTypeMap.Add(t, _layerRendererTypeMap[t2]);
                            break;
                        }
                    }
                    if (!_layerRendererTypeMap.ContainsKey(t) && t.BaseType != null)
                    {
                        Type t3 = t.BaseType;
                        while (t3 != null)
                        {
                            if (_layerRendererTypeMap.ContainsKey(t3))
                            {
                                lock (_layerRendererTypeMap)
                                    _layerRendererTypeMap.Add(t, _layerRendererTypeMap[t3]);
                                break;
                            }
                            else
                            {
                                t3 = t3.BaseType;
                            }
                        }
                    }
                    if (!_layerRendererTypeMap.ContainsKey(t))
                        throw new InvalidOperationException(string.Format("No renderer is configured for layer type {0}", t));
                }


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
    }
}
