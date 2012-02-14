using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoAPI.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Coverages
{
    public class Route : NetworkCoverage
    {
        public Route(string name, bool isTimeDependend, string outputName, string outputUnit)
            : base(name, isTimeDependend, outputName, outputUnit)
        {
            InitializeAsRoute();
        }

        public Route()
        {
            InitializeAsRoute();
        }

        private void InitializeAsRoute()
        {
            SegmentGenerationMethod = SegmentGenerationMethod.RouteBetweenLocations;
            Locations.IsAutoSorted = false;
            Segments.IsAutoSorted = false;
        }

        public event EventHandler RouteSegmentsUpdated;

        protected override void UpdateSegments()
        {
            base.UpdateSegments();

            if (RouteSegmentsUpdated != null)
            {
                RouteSegmentsUpdated(this,new EventArgs());
            }
        }

        protected override void UpdateValuesForBranchSplit(Actions.BranchSplitAction currentEditAction)
        {
            //just ignore the split this does not affect routes.
        }
    }
}
