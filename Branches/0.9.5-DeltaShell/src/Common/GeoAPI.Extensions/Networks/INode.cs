using DelftTools.Utils.Collections.Generic;

namespace GeoAPI.Extensions.Networks
{
    public interface INode : INetworkFeature
    {
        /// <summary>
        /// The incoming branches of the node
        /// </summary>
        IEventedList<IBranch> IncomingBranches { get; set; }

        /// <summary>
        /// The outgoing branches of the node
        /// </summary>
        IEventedList<IBranch> OutgoingBranches { get; set; }

        /// <summary>
        /// The features of the node
        /// </summary>
        IEventedList<INodeFeature> NodeFeatures { get; set; }

        /// <summary>
        /// Whether the node has more than one incoming and/or outgoing branches
        /// </summary>
        bool IsConnectedToMultipleBranches { get; }

        /// <summary>
        /// Whether the node has exactly one incoming or outgoing branch
        /// </summary>
        bool IsOnSingleBranch { get; }
    }
}