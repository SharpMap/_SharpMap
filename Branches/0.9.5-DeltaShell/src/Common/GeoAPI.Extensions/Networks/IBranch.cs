using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Utils.Collections.Generic;
using QuickGraph;

namespace GeoAPI.Extensions.Networks
{
    public interface IBranch : INetworkFeature, IComparable<IBranch>, IEdge<INode>
    {
        new INode Source { get; set; }
        new INode Target { get; set; }

        double Length { get; set; }
        
        /// <summary>
        /// True is length is not computed from Geometry.
        /// </summary>
        bool IsLengthCustom { get; set; }

        IEventedList<IBranchFeature> BranchFeatures { get; set; }
    }
}