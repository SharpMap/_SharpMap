using System;
using GeoAPI.Extensions.Feature;

namespace GeoAPI.Extensions.Coverages
{
    public interface IFeatureLocation : IComparable<IFeatureLocation>, IComparable
    {
        IFeature Feature { get; set; }

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