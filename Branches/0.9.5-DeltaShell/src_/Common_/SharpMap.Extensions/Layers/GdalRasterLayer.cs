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
        public GdalRasterLayer()
            : this("image layer1", null)
        {
        }

        public GdalRasterLayer(string filename)
            : this("image layer1", filename)
        {
        }

        /// <summary>
        /// initialize a Gdal based raster layer
        /// </summary>
        /// <param name="name">Name of layer</param>
        /// <param name="path">location of image</param>
        public GdalRasterLayer(string name, string path)
        {
            base.Name = name;

            var rasterFeatureProvider = new GdalFeatureProvider();

            if (path != null)
            {
                rasterFeatureProvider.Open(path);
            }
            CustomRenderers.Clear();

            CustomRenderers.Add(new GdalRenderer(this)); // add optimized custom gdal renderer

            DataSource = rasterFeatureProvider;
        }
    }
}
