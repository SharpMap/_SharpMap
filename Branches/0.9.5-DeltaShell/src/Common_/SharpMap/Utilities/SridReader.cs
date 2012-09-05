using System.Collections.Generic;
using GeoAPI.CoordinateSystems;
using SharpMap.Properties;

namespace SharpMap.Utilities
{
    public class SridReader
    {
        public struct WKTstring
        {
            /// <summary>Well-known ID</summary>
            public int WKID;
            /// <summary>Well-known Text</summary>
            public string WKT;
        }

        /// <summary>Enumerates all SRID's in the SRID.csv file.</summary>
        /// <returns>Enumerator</returns>
        public static IEnumerable<WKTstring> GetSRIDs()
        {
            string[] srids = Resources.SRID.Split('\n');
            foreach (string srid in srids)
            {
                WKTstring wkt = new WKTstring();
                wkt.WKID = int.Parse(srid.Split(';')[0]);
                wkt.WKT = srid.Split(';')[1];
                yield return wkt;
            }
        }
        /// <summary>Gets a coordinate system from the SRID.csv file</summary>
        /// <param name="id">EPSG ID</param>
        /// <returns>Coordinate system, or null if SRID was not found.</returns>
        public static ICoordinateSystem GetCSbyID(int id)
        {
            foreach (SridReader.WKTstring wkt in SridReader.GetSRIDs())
            {
                if (wkt.WKID == id) //We found it!
                {
                    return SharpMap.Converters.WellKnownText.CoordinateSystemWktReader.Parse(wkt.WKT) as ICoordinateSystem;
                }
            }
            return null;
        }
    }
}
