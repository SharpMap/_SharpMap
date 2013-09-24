using System;
using DelftTools.Functions;
using DelftTools.Utils.Aop;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    /// <summary>
    /// Class to convert Networklocations to NetCdf formats.Id now needed for NHibernate :(
    /// </summary>
    public class NetworkLocationTypeConverter : TypeConverterBase<INetworkLocation>
    {
        public virtual INetwork Network { get; set; }

        public override Type[] StoreTypes
        {
            get
            {
                return new[] { typeof(int), typeof(double), typeof(char[]), typeof(double), typeof(double) };
            }
        }

        public override string[] VariableNames
        {
            get { return new[] { "branch_index", "branch_chainage", "branch_name", "x", "y" }; }
        }

        public override string[] VariableStandardNames
        {
            get { return new[] { "branch_index", "branch_chainage", "branch_name", FunctionAttributes.StandardNames.Longitude_X, FunctionAttributes.StandardNames.Latitude_Y }; }
        }

        public override string[] VariableUnits
        {
            get { return new[] { "-", FunctionAttributes.StandardUnits.Meters, "-", FunctionAttributes.StandardUnits.Long_X_Degr, FunctionAttributes.StandardUnits.Lat_Y_Degr }; }
        }

        /// <summary>
        /// Network coverage associated with this network location (if any).
        /// TODO: check if we can't incapsulate it in the NetCDF class, or as a NetworkCoverageTypeConverter, otherwise rename this class to NetworkCoverageLocationTypeConverter
        /// </summary>
        [Aggregation]
        public virtual INetworkCoverage Coverage { get; set; }

        public override INetworkLocation ConvertFromStore(object source)
        {
            var tuple = (object[]) source;
            
            var branchIdx = Convert.ToInt32(tuple[0]);
            var chainage = Convert.ToDouble(tuple[1]);
            var branch = Network.Branches[branchIdx];// First(b => b.Id == branchId);
            
            var location = new NetworkLocation(branch, chainage);
            
            return location;
        }

        public override object[] ConvertToStore(INetworkLocation source)
        {
            //a DelftTools.Utils.Tuple of branch name and chainage. Long is not supported by netcdf!?
            var idx = source.Network.Branches.IndexOf(source.Branch);
            var branchName = source.Branch.Name;
            var x = 0.0;
            var y = 0.0;
            
            if(source.Geometry != null)
            {
                var centroid = source.Geometry.Centroid;
                x = centroid.X;
                y = centroid.Y;
            }

            return new object[] { idx, source.Chainage, ConvertToCharArray(branchName), x, y };
        }

        public NetworkLocationTypeConverter()
        {
            
        }

        public NetworkLocationTypeConverter(INetwork network)
        {
            Network = network;
        }
    }
}
