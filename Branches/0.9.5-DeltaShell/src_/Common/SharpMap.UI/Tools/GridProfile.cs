using System;
using System.Windows.Forms;
using DelftTools.Functions;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Layers;

namespace SharpMap.UI.Tools
{
    /// <summary>
    /// Feature implementation to support a profile on a grid coverage
    /// </summary>
    public class GridProfile : UserControl, IFeature
    {
        public virtual long Id { get; set; }
        public IFeatureAttributeCollection Attributes { get; set; }
        public IRegularGridCoverageLayer GridLayer { get; set;}
 
        private IGeometry geometry;
        public IGeometry Geometry 
        { 
            get { return geometry; } 
            set 
            { 
                if (!(value is ILineString))
                {
                    throw new ArgumentException("GridProfile only supports LineString geometries.");
                }
                geometry = value;
                if (null != profileData)
                {
                    RegularGridCoverageHelper.UpdateGridValues(profileData, GridLayer.Grid, geometry as ILineString);
                }
            }
 
        }

        private Function profileData;
        public Function ProfileData 
        {
            get
            {
                if (null == GridLayer)
                {
                    return null;
                }
                if (null == Geometry)
                {
                    return null;
                }
                if (null == profileData)
                {
                    profileData = RegularGridCoverageHelper.GetGridValues(GridLayer.Grid, geometry as ILineString);
                }
                else
                {
                    //RegularGridCoverageHelper.UpdateGridValues(profileData, GridLayer.Grid, geometry as ILineString);
                }
                return profileData; 
            }
            set 
            { 
                profileData = value; 
            }
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // GridProfile
            // 
            this.Name = "GridProfile";
            this.Size = new System.Drawing.Size(266, 150);
            this.ResumeLayout(false);

        }
    }
}