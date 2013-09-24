using System;
using System.Collections.Generic;

namespace GeoAPI.Extensions.Networks
{
    [Obsolete("Use EditAction/IsEditing of the aggregate (parent composition object) instead of this")]
    public static class INetworkFeatureExtensions
    {
        private static readonly IList<INetworkFeature> networkFeaturesBeingMoved = new List<INetworkFeature>();

        public static bool IsBeingMoved(this INetworkFeature networkFeature)
        {
            return (networkFeaturesBeingMoved.Contains(networkFeature));
        }

        public static void SetBeingMoved(this INetworkFeature networkFeature, bool beingMoved)
        {
            if (beingMoved)
            {
                if (!networkFeaturesBeingMoved.Contains(networkFeature))
                {
                    networkFeaturesBeingMoved.Add(networkFeature);
                }
            }
            else
            {
                if (networkFeaturesBeingMoved.Contains(networkFeature))
                {
                    networkFeaturesBeingMoved.Remove(networkFeature);
                }
            }
        }
    }
}
