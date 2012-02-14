using System;
using log4net;
using SharpMap.Extensions.Data.Providers;
using SharpMap.Rendering;

namespace SharpMap.Extensions.Layers
{
    /// <summary>
    /// Variation of GdalRasterLayer that uses by default the RegularGridCoverageRenderer.
    /// 
    /// Injecting the renderer as original done by the LayerFacvtory is effectively broken
    /// by the hibernate mapping that directly accesses the fields of the layer.
    /// </summary>
    public class GdalRegularGridRasterLayer : GdalRasterLayer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GdalRegularGridRasterLayer));
        
        public GdalRegularGridRasterLayer()
            : this("image layer1", null)
        {
        }

        public GdalRegularGridRasterLayer(string filename)
            : this("image layer1", filename)
        {
        }

        /// <summary>
        /// initialize a Gdal based raster layer
        /// </summary>
        /// <param name="name">Name of layer</param>
        /// <param name="path">location of image</param>
        public GdalRegularGridRasterLayer(string name, string path) : base(name, path)
        {
            CustomRenderers.Clear();
            CustomRenderers.Add(new RegularGridCoverageRenderer(this)); // add optimized custom gdal renderer
        }
    }
}