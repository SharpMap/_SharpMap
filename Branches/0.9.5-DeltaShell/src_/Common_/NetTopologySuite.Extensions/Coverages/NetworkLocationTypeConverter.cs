using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using ValidationAspects;

namespace NetTopologySuite.Extensions.Coverages
{
    /// <summary>
    /// Class to convert Networklocations to NetCdf formats.Id now needed for NHibernate :(
    /// </summary>
    //TODO : get rid of this ID and map this class as a component of the store. 
    // Note this might conflict with the Network reference
    public class NetworkLocationTypeConverter : TypeConverterBase<INetworkLocation>
    {
        public virtual INetwork Network { get; set; }

        public override Type[] StoreTypes
        {
            get
            {
                return new[] {typeof (int), typeof (double), typeof (double), typeof (double)};
            }
        }

        public override string[] VariableNames
        {
            get { return new[] { "branch_id", "branch_offset", "x", "y" }; }
        } 

        /// <summary>
        /// Network coverage associated with this network location (if any).
        /// TODO: check if we can't incapsulate it in the NetCDF class, or as a NetworkCoverageTypeConverter, otherwise rename this class to NetworkCoverageLocationTypeConverter
        /// </summary>
        public virtual INetworkCoverage Coverage { get; set; }

        public override INetworkLocation ConvertFromStore(object source)
        {
            var sourceTuple = (object[]) source;
            
            var branchId = Convert.ToInt32(sourceTuple[0]);
            var offset = Convert.ToDouble(sourceTuple[1]);
            var branch = Network.Branches.First(b => b.Id == branchId);
            
            var location = new NetworkLocation(branch, offset);
            //location.Attributes = new NetworkLocationAttributeAccessor(location, Coverage);
            
            return location;
        }

        public override object[] ConvertToStore(INetworkLocation source)
        {
            CheckIfBranchIdsAreUnique();

            //a tuple of brand ID and offset. Long is not supported by netcdf!?
            return new object[] {Convert.ToInt32(source.Branch.Id), source.Offset, source.Geometry.Centroid.X, source.Geometry.Centroid.Y };
        }

        public NetworkLocationTypeConverter()
        {
            
        }

        public NetworkLocationTypeConverter(INetwork network)
        {
            Network = network;
        }

        private IEnumerable<long> UniqueBranchIds
        {
            get
            {
                if(Network == null)
                {
                    return Enumerable.Empty<long>();
                }

                return Network.Branches.Select(b => b.Id).Distinct();
            }
        }

        private void CheckIfBranchIdsAreUnique()
        {
            if(UniqueBranchIds.Count() != Network.Branches.Count)
            {
                throw new InvalidOperationException("Branch ids in the network are not unique, can't convert network location");
            }
        }
    }
}
