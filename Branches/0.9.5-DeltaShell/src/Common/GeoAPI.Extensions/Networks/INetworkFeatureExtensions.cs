using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeoAPI.Extensions.Networks
{
    public static class INetworkFeatureExtensions
    {
        private static IList<INetworkFeature> networkFeaturesBeingMoved = new List<INetworkFeature>();

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
