using GeoAPI.Extensions.Feature;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Networks
{
    public abstract class NodeFeature : NetworkFeature, INodeFeature
    {
        [FeatureAttribute] public virtual INode Node { get; set; }

        public override INetwork Network
        {
            get { return Node == null ? null : Node.Network; }
        }
    }
}