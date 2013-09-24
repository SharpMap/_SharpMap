using System;
using GeoAPI.Extensions.Feature;

namespace GeoAPI.Extensions.Coverages
{
    public interface IFeatureLocation : IComparable<IFeatureLocation>, IComparable
    {
        IFeature Feature { get; set; }

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