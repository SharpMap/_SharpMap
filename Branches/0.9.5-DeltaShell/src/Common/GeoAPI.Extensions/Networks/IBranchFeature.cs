using System;
using DelftTools.Utils;

namespace GeoAPI.Extensions.Networks
{
    public interface IBranchFeature : INetworkFeature, IComparable<IBranchFeature>, ICopyFrom, IComparable
    {
        IBranch Branch { get; set; }
        
        double Offset { get; set; }

        /// <summary>
        /// Length of the branch feature starting from offset:
        /// 
        /// [] - branch feature
        /// 
        ///                    <----length--->
        /// *------------------[-------------]-------------*
        /// <------------------>
        ///        offset
        ///  
        /// </summary>
        double Length { get; set; }


    }
}