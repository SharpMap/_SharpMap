using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.Extensions.Coverages;

namespace SharpMap.Layers
{
    public class RouteGroupLayer : NetworkCoverageGroupLayer
    {
        public virtual Route Route
        {
            get { return (Route)NetworkCoverage; }
            set { NetworkCoverage = value; }
        }
    }
}
