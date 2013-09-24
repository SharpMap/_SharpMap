using System;
using System.IO;
using System.Linq;
using GeoAPI.CoordinateSystems;

namespace SharpMap.Extensions.CoordinateSystems
{
    /// <summary>
    /// See http://www.gdal.org/ogr/classOGRSpatialReference.html for the documentation of all SpatialReference methods.
    /// </summary>
    public class OgrCoordinateSystem : OSGeo.OSR.SpatialReference, ICoordinateSystem
    {
        /// <summary>
        /// supported coordinate system ids (geographic and projected)
        /// </summary>
        private static OgrCoordinateSystem[] supportedCoordinateSystems;

        static OgrCoordinateSystem()
        {
            // expect gdal_data at the same location as gdal_wrap.dll
            var gdalDataPath = Path.GetDirectoryName(typeof(OSGeo.OSR.SpatialReference).Assembly.Location);
            gdalDataPath = Path.Combine(gdalDataPath, "gdal_data");
            OSGeo.GDAL.Gdal.SetConfigOption("GDAL_DATA", gdalDataPath);
        }

        public OgrCoordinateSystem(IntPtr cPtr, bool cMemoryOwn, object parent) : base(cPtr, cMemoryOwn, parent)
        {
        }

        public OgrCoordinateSystem() : this("")
        {
        }

        public OgrCoordinateSystem(string wkt) : base(wkt)
        {
            if (!string.IsNullOrEmpty(wkt))
            {
                AutoIdentifyEPSG();
                Authority = IsProjected() == 1 ? GetAuthorityName("PROJCS") : GetAuthorityName("GEOGCS");

                if (Authority != null)
                {
                    AuthorityCode = IsProjected() == 1
                                        ? int.Parse(GetAuthorityCode("PROJCS"))
                                        : int.Parse(GetAuthorityCode("GEOGCS"));
                }
            }
        }

        public string Name
        {
            get
            {
                return GetAttrValue(IsGeographic() == 1 ? "GEOGCS" : "PROJCS", 0);
            }
        }

        public static OgrCoordinateSystem[] SupportedCoordinateSystems
        {
            get
            {
                if (supportedCoordinateSystems == null)
                {
                    supportedCoordinateSystems = Properties.Resources.cs_ids.Split('\n').Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith("#"))
                        .Select(int.Parse)
                        .Select(id => { var crs = new OgrCoordinateSystem { AuthorityCode = id, Authority = "EPSG" }; crs.ImportFromEPSG(id);
                                          return crs;
                        }).ToArray();
                }

                return supportedCoordinateSystems;
            }
        }

        public string Authority { get; private set; }
        
        public long AuthorityCode { get; private set; }
        
        public string Alias { get; private set; }
        
        public string Abbreviation { get; private set; }
        
        public string Remarks { get; private set; }

        public string WKT
        {
            get
            {
                var s = "";
                ExportToPrettyWkt(out s, 0);
                return s;
            }
        }
        
        public string XML 
        {
            get
            {
                var xml = "";
                ExportToXML(out xml, string.Empty);
                return xml;
            } 
        }

        public string PROJ4
        { 
            get
            {
                var proj4 = "";
                ExportToProj4(out proj4);
                return proj4;
            
            } 
        }

        public bool EqualParams(object obj)
        {
            throw new NotImplementedException();
        }

        public int Dimension { get; private set; }
        
        public AxisInfo GetAxis(int dimension)
        {
            throw new NotImplementedException();
        }

        public IUnit GetUnits(int dimension)
        {
            throw new NotImplementedException();
        }

        public double[] DefaultEnvelope { get; private set; }

        public bool IsGeographic { get { return base.IsGeographic() == 1; } }
    }
}
