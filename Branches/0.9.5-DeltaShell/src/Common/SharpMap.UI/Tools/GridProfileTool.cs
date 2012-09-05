using System.Collections.Generic;
using System.Drawing;
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
    public class GridProfileTool: MapTool
    {
        private IRegularGridCoverageLayer gridLayer;
        private readonly NewLineTool newLineTool;

        private List<IFeature> GridProfiles { get; set; }

        public GridProfileTool(MapControl mapControl)
            : base(mapControl)
        {

            GridProfiles = new List<IFeature>();

            VectorLayer profileLayer = new VectorLayer("Profile Layer")
                                           {
                                               DataSource = new FeatureCollection(GridProfiles, typeof(GridProfile)),
                                               Enabled = true,
                                               Style = new VectorStyle
                                                           {
                                                               Fill = new SolidBrush (Color.Tomato),
                                                               Symbol = null,
                                                               Line = new Pen(Color.SteelBlue, 1),
                                                               Outline = new Pen(Color.FromArgb(50, Color.LightGray), 3)
                                                           },
                                               Map = mapControl.Map,
                                               ShowInTreeView = true
                                           };
            Layer = profileLayer;

            newLineTool = new NewLineTool(profileLayer)
                              {
                                  MapControl = mapControl,
                                  Name = "ProfileLine",
                                  AutoCurve = false,
                                  MinDistance = 0,
                                  IsActive = false
                              };
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
                if (gridLayer != null)
                {
                    if (Layer.DataSource.Features.Count > 0)
                    {
                        IFeature feature = (IFeature)Layer.DataSource.Features[0];
                        GridProfile gridProfile = (GridProfile)feature;
                        gridLayer.Grid.Name = gridLayer.Name; //Hack: sometimes a different name?
                        gridProfile.GridLayer = gridLayer;
                        return gridProfile.ProfileData;
                    }
                }
                return null;
            }
        }

        public bool HasProfile()
        {
            return Layer.DataSource.Features.Count > 0;
        }

        /// <summary>
        /// Grid/Raster to rip the data off
        /// </summary>
        public IRegularGridCoverageLayer GridLayer
        {
            get
            {
                return gridLayer;
            }
            set
            {
                gridLayer = value;
                if (Layer.Map == null)
                {
                    Layer.Map = gridLayer.Map;
                }
            }
        }

        /// <summary>
        /// Clear profile
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < Layer.DataSource.Features.Count; i++)
            {
                Layer.DataSource.Features.RemoveAt(0);
            }
            Layer.RenderRequired = true;
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
            if(Layer.DataSource.Features.Count>0)
            {
                Clear();
            }
            if(!Map.Layers.Contains(Layer))
            {
                Map.Layers.Insert(0,Layer);
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
