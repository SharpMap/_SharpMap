using System;

namespace GeoAPI.Extensions.Networks
{
    public interface IBranchFeature : INetworkFeature, IComparable<IBranchFeature>, IComparable
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