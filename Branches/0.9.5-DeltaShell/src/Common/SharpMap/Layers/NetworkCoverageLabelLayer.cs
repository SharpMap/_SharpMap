using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using SharpMap.Data.Providers;

namespace SharpMap.Layers
{
    public class NetworkCoverageLabelLayer:LabelLayer
    {
        protected override string GetText(IFeature feature)
        {
            var loc = (INetworkLocation)feature;
            //return locationname..segment name or value of the feature
            if (LabelColumn == "Branch")
            {
                return loc.Branch.Name;
            }
            if (LabelColumn == "Offset")
            {
                return loc.Offset.ToString();
            }

            return Coverage.Evaluate(loc).ToString();
        }

      
        private INetworkCoverage Coverage
        {
            get
            {
                return ((NetworkCoverageFeatureCollection) DataSource).RenderedCoverage;
            }
        }

        public override IFeatureProvider DataSource
        {
            get
            {
                return base.DataSource;
            }
            set
            {
                base.DataSource = value;
            }
        }
    }
}
