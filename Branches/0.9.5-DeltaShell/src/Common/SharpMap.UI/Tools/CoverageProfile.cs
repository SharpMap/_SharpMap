using System;
using System.Windows.Forms;
using DelftTools.Functions;
using DelftTools.Utils;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Layers;

namespace SharpMap.UI.Tools
{
    /// <summary>
    /// Feature implementation to support a profile on a grid coverage
    /// </summary>
    public class CoverageProfile : UserControl, IFeature
    {
        public long Id { get; set; }

        public Type GetEntityType()
        {
            return GetEntityType();
        }

        public IFeatureAttributeCollection Attributes { get; set; }

        private ICoverageLayer coverageLayer;

        public ICoverageLayer CoverageLayer
        {
            get { return coverageLayer; } 
            set
            {
                coverageLayer = value;
                
                if(coverageLayer is ITimeNavigatable)
                {
                    ((ITimeNavigatable) coverageLayer).CurrentTimeSelectionChanged += () => Geometry = geometry; // updates grid profile using current layer time
                }
            }
        }
 
        private IGeometry geometry;
        public IGeometry Geometry 
        { 
            get { return geometry; } 
            set 
            { 
                if (!(value is ILineString))
                {
                    throw new ArgumentException("CoverageProfile only supports LineString geometries.");
                }
                geometry = value;
                if (null != profileData)
                {
                    var time = GetCurrentTime();
                    CoverageHelper.UpdateCoverageValues(profileData, CoverageLayer.Coverage, geometry as ILineString, time);
                }
            }
 
        }

        private Function profileData;
        public Function ProfileData 
        {
            get
            {
                if (null == CoverageLayer)
                {
                    return null;
                }
                if (null == Geometry)
                {
                    return null;
                }
                if (null == profileData)
                {
                    DateTime? time = GetCurrentTime();
                    profileData = CoverageHelper.GetCoverageValues(CoverageLayer.Coverage, geometry as ILineString, time);
                    
                }
                else
                {
                    //CoverageHelper.UpdateCoverageValues(profileData, CoverageLayer.Grid, geometry as ILineString);
                }
                return profileData; 
            }
            set 
            { 
                profileData = value; 
            }
        }

        private DateTime? GetCurrentTime()
        {
            var coverage = CoverageLayer.Coverage;
            if (coverage.IsTimeDependent && CoverageLayer is ITimeNavigatable)
            {
                var timeNavigatable = CoverageLayer as ITimeNavigatable;
                return timeNavigatable.TimeSelectionStart;
            }
            
            return null;
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
            this.Name = "CoverageProfile";
            this.Size = new System.Drawing.Size(266, 150);
            this.ResumeLayout(false);

        }
    }
}