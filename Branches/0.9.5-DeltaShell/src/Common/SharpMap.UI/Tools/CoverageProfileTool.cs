using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DelftTools.Functions;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Styles;
using SharpMap.UI.Forms;

namespace SharpMap.UI.Tools
{
    /// <summary>
    /// The profile tool draws a profile 'on' a grid layer and gathers the data of the gridcells of the profile
    /// </summary>
    public class CoverageProfileTool: MapTool
    {
        private readonly NewLineTool newLineTool;

        private List<IFeature> GridProfiles { get; set; }

        private VectorLayer profileLayer;

        public CoverageProfileTool()
        {

            GridProfiles = new List<IFeature>();

            profileLayer = new VectorLayer("Profile Layer")
                                           {
                                               DataSource = new FeatureCollection(GridProfiles, typeof(CoverageProfile)),
                                               Visible = true,
                                               Style = new VectorStyle
                                                           {
                                                               Fill = new SolidBrush (Color.Tomato),
                                                               Symbol = null,
                                                               Line = new Pen(Color.Tomato, 2),
                                                               Outline = new Pen(Color.FromArgb(50, Color.Tomato), 2)
                                                           },
                                               ShowInTreeView = true,
                                               FeatureEditor = new CoverageProfileEditor()
                                           };
            
            LayerFilter = l => l.Equals(profileLayer);

            newLineTool = new NewLineTool(l => l.Equals(profileLayer), "new coverage profile")
                              {
                                  AutoCurve = false,
                                  MinDistance = 0,
                                  IsActive = false
                              };
        }

        public override IMapControl MapControl
        {
            get { return base.MapControl; } 
            set 
            { 
                base.MapControl = value;
                newLineTool.MapControl = MapControl;
                profileLayer.Map = MapControl.Map;
            }
        }

        public override bool IsActive
        {
            get
            {
                return base.IsActive;
            }
            set
            {
                newLineTool.IsActive = value;
                base.IsActive = value;
            }
        }

        /// <summary>
        /// Grid data of profile as function offset-value;
        /// returns null if there's no data available
        /// </summary>
        public Function ProfileData
        {
            get
            {
                var coverageLayer = CoverageLayer;

                if (coverageLayer != null)
                {
                    if (profileLayer.DataSource.Features.Count > 0)
                    {
                        var feature = (IFeature)profileLayer.DataSource.Features[0];
                        var profile = (CoverageProfile)feature;

                        profile.CoverageLayer = coverageLayer;
                        profile.ProfileData.Name = coverageLayer.Name;
                        
                        return profile.ProfileData;
                    }
                }
                return null;
            }
        }

        public bool HasProfile()
        {
            return profileLayer.DataSource.Features.Count > 0;
        }

        /// <summary>
        /// Grid/Raster to rip the data off
        /// </summary>
        public ICoverageLayer CoverageLayer
        {
            get
            {
                return Map.GetAllVisibleLayers(true).OfType<ICoverageLayer>().FirstOrDefault();
            }
        }

        /// <summary>
        /// Clear profile
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < profileLayer.DataSource.Features.Count; i++)
            {
                profileLayer.DataSource.Features.RemoveAt(0);
            }
            profileLayer.RenderRequired = true;
            MapControl.Refresh();
        }

        /// <summary>
        /// Flush: from geometry to profile feature
        /// </summary>
        public void Flush()
        {
            newLineTool.Flush();
            MapControl.Refresh();
        }

        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            IsNewProfile();
            newLineTool.OnMouseDown(worldPosition, e);
        }

        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            newLineTool.OnMouseUp(worldPosition, e);
        }

        public override void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            newLineTool.OnMouseDoubleClick(sender, e);
            MapControl.SelectTool.Clear();
            MapControl.ActivateTool(MapControl.SelectTool);
            MapControl.Refresh();
        }

        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            newLineTool.OnMouseMove(worldPosition, e);
        }

        public override void Render(Graphics graphics, Map mapBox)
        {
            newLineTool.Render(graphics, mapBox);
        }

        private void IsNewProfile()
        {
            if(profileLayer.DataSource.Features.Count>0)
            {
                Clear();
            }
            if(!Map.Layers.Contains(profileLayer))
            {
                Map.Layers.Insert(0, profileLayer);
            }
        }

        public override void Cancel()
        {
            Clear();
            newLineTool.Cancel();
            base.Cancel();
        }

    }
}
