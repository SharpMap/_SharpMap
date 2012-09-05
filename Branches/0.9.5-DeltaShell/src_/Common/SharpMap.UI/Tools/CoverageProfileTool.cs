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
    public class CoverageProfileTool: MapTool
    {
        private ICoverageLayer coverageLayer;
        private readonly NewLineTool newLineTool;

        private List<IFeature> GridProfiles { get; set; }

        public CoverageProfileTool(MapControl mapControl)
            : base(mapControl)
        {

            GridProfiles = new List<IFeature>();

            VectorLayer profileLayer = new VectorLayer("Profile Layer")
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
                if (coverageLayer != null)
                {
                    if (Layer.DataSource.Features.Count > 0)
                    {
                        IFeature feature = (IFeature)Layer.DataSource.Features[0];
                        CoverageProfile profile = (CoverageProfile)feature;


                        coverageLayer.Coverage.Name = coverageLayer.Name; //Hack: sometimes a different name?
                        profile.CoverageLayer = coverageLayer;
                        
                        
                        return profile.ProfileData;
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
        public ICoverageLayer CoverageLayer
        {
            get
            {
                return coverageLayer;
            }
            set
            {
                coverageLayer = value;
                if (Layer.Map == null)
                {
                    Layer.Map = coverageLayer.Map;
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
