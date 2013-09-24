using System;
using System.Collections.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Editing;

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

        /// <summary>
        /// Finds the shortest path from <paramref name="source"/> to <paramref name="target"/>.
        /// </summary>
        /// <param name="source">Source node in network</param>
        /// <param name="target">target node in network</param>
        /// <param name="weights">Cost function for determining shortest path</param>
        /// <returns>Returns shortest path basde on the costfunction.</returns>
        IEnumerable<IBranch> GetShortestPath(INode source, INode target, Func<IBranch, double> weights);

        //IEnumerable<INode> BoundaryNodes { get; set; }

        INode NewNode();
    }
}