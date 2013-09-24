using System.Drawing;
using GeoAPI.Geometries;
using SharpMap.Api;
using SharpMap.Data.Providers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

namespace SharpMap.Layers
{
    //TODO : remove this class...get this logic into the container or something...too many entities
    public class NetworkCoverageLocationLayer : NetworkCoverageBaseLayer
    {
        private readonly NetworkCoverageLocationRenderer renderer;

        public NetworkCoverageLocationLayer()
        {
            Name = "Locations";
            
            //Coverage = coverage;
            renderer = new NetworkCoverageLocationRenderer();
            var networkCoverageFeatureCollection = new NetworkCoverageFeatureCollection {NetworkCoverageFeatureType= NetworkCoverageFeatureType.Locations};
            DataSource = networkCoverageFeatureCollection;
            
            CustomRenderers.Add(renderer);
        }

        protected override void OnInitializeDefaultStyle()
        {
            Style = new VectorStyle { GeometryType = typeof(IPoint) };
        }

        public override void OnRender(Graphics g, IMap map)
        {
            renderer.Render(null, g, this);
        }
        
        protected override void CreateDefaultTheme()
        {
            // If there was no theme attached to the layer yet, generate a default theme
            if (Theme != null )
            {
                return;
            }

            //looks like min/max should suffice
            var minMaxValue = GetMinMaxValue();
            if (minMaxValue != null && minMaxValue.First != double.MaxValue && minMaxValue.Second != double.MaxValue)
            {
                const int numberOfClasses = 12;
                Theme = ThemeFactory.CreateGradientTheme(Coverage.Components[0].Name, Style,
                                                                   ColorBlend.Rainbow7,
                                                                   (float) minMaxValue.First, (float) minMaxValue.Second,
                                                                   12, 25, false, false, numberOfClasses);
            
            }
            AutoUpdateThemeOnDataSourceChanged = true;
        }

        
    }
}
