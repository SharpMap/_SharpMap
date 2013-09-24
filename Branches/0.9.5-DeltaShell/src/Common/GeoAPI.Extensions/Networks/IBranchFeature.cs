using System;
using DelftTools.Utils;

namespace GeoAPI.Extensions.Networks
{
    public interface IBranchFeature : INetworkFeature, IComparable<IBranchFeature>, ICopyFrom, IComparable
    {
        IBranch Branch { get; set; }
        
        double Chainage { get; set; }

        /// <summary>
        /// Length of the branch feature starting from chainage:
        /// 
        /// [] - branch feature
        /// 
        ///                    <----length--->
        /// *------------------[-------------]-------------*
        /// <------------------>
        ///        chainage
        ///  
        /// </summary>
        double Length { get; set; }


    }
}