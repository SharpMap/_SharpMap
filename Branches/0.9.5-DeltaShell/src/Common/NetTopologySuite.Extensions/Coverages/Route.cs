using System;
using GeoAPI.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Coverages
{
    public class Route : NetworkCoverage
    {
        public Route(): base("route", false, "Chainage", "m")
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

            if (segmentsInitialized)
            {
                if (RouteSegmentsUpdated != null)
                {
                    RouteSegmentsUpdated(this, new EventArgs());
                }
            }
        }
    }
}
