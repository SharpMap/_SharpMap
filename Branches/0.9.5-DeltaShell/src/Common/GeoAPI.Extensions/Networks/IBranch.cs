using System;
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
        /// Custom length is not set to derived objects.
        /// eg. when the length of a branch is mnodifie from 100 to custom length 200
        /// internal offset are unchanged. It is UI of model resposability to handle this
        /// </summary>
        bool IsLengthCustom { get; set; }

        IEventedList<IBranchFeature> BranchFeatures { get; set; }
    }
}