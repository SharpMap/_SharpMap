using System;
using log4net;
using SharpMap.Data.Providers;
using SharpMap.Extensions.Data.Providers;
using SharpMap.Layers;

namespace SharpMap.Extensions.Layers
{
    /// <summary>
    /// Gdal raster image layer, helps to create RegularGridCoverageLayer with GdalFeatureProvider and GdalRenderer
    /// 
    /// see also GdalRegularGridRasterLayer
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="C#">
    /// myMap = new SharpMap.Map(new System.Drawing.Size(500,250);
    /// SharpMap.Layers.GdalRasterLayer layGdal = new SharpMap.Layers.GdalRasterLayer("Blue Marble", @"C:\data\srtm30plus.tif");
    /// myMap.Layers.Add(layGdal);
    /// myMap.ZoomToExtents();
    /// </code>
    /// </example>
    /// </remarks>

    public class GdalRasterLayer : RegularGridCoverageLayer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GdalRasterLayer));

        public GdalRasterLayer()
            : this("image layer1", null)
        {
        }

        public GdalRasterLayer(string filename)
            : this("image layer1", filename)
        {
        }

        public override IFeatureProvider DataSource
        {
            get { return base.DataSource ?? (base.DataSource = new GdalFeatureProvider()); }
            set { base.DataSource = value; }
        }

        /// <summary>
        /// initialize a Gdal based raster layer
        /// </summary>
        /// <param name="name">Name of layer</param>
        /// <param name="path">location of image</param>
        public GdalRasterLayer(string name, string path)
        {
            base.Name = name;

            CustomRenderers.Clear();
            CustomRenderers.Add(new GdalRenderer(this)); // add optimized custom gdal renderer

            if (path == null)
            {
                return;
            }

            var rasterFeatureProvider = new GdalFeatureProvider();

            try
            {
                rasterFeatureProvider.Open(path);
            }
            catch (Exception e)
            {
                log.Error("Can't open raster file, path: " + path, e);
            }

            DataSource = rasterFeatureProvider;
        }
    }
}
