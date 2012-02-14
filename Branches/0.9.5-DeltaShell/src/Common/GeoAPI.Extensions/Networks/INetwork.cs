using System;
using System.Collections.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Feature;
using QuickGraph;

namespace GeoAPI.Extensions.Networks
{
    /// <summary>
    /// TODO: change views to IEnumerable
    /// </summary>
    public interface INetwork : IFeature, INameable, IUndirectedGraph<INode, IBranch>, IEditableObject
    {
        IEventedList<IBranch> Branches { get; set; }
        IEventedList<INode> Nodes { get; set; }

        IEnumerable<IBranchFeature> BranchFeatures { get; }
        
        IEnumerable<INodeFeature> NodeFeatures { get; }

        IEnumerable<IBranch> GetShortestPath(INode source, INode target, Func<IBranch, double> weights);

        //IEnumerable<INode> BoundaryNodes { get; set; }
    }
}