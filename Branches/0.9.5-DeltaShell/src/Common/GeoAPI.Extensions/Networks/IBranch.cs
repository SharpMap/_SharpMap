using System;
using DelftTools.Utils.Collections.Generic;
using QuickGraph;

namespace GeoAPI.Extensions.Networks
{
    public interface IBranch : INetworkFeature, IComparable<IBranch>, IEdge<INode>
    {
        new INode Source { get; set; }
        new INode Target { get; set; }

        /// <summary>
        /// Returns the length of the Geometry when <see cref="IsLengthCustom"/> is false, or
        /// returns the user set length when <see cref="IsLengthCustom"/> is true.
        /// </summary>
        /// <remarks>
        /// User specified length for <see cref="IsLengthCustom"/> set to true is buffered, such that toggling
        /// <see cref="IsLengthCustom"/> 2x results in the user specified length is kept.
        /// </remarks>
        double Length { get; set; }
        
        /// <summary>
        /// True: length is not computed from Geometry.
        /// Custom length is not set to derived objects.
        /// e.g. when the length of a branch is modified from 100 to custom length 200
        /// internal chainages are unchanged. It is UI of model responsibility to handle this
        /// </summary>
        bool IsLengthCustom { get; set; }

        /// <summary>
        /// Order number will be used for interpolation over the branches. 
        /// A chain of branches with the same order number will be treated as one.
        /// </summary>
        int OrderNumber { get; set; }

        IEventedList<IBranchFeature> BranchFeatures { get; set; }
    }
}