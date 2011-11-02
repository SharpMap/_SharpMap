using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using BruTile.Web;
using GeoAPI.CoordinateSystems;
using GeoAPI.IO;

#if DotSpatialProjections
using CS = DotSpatial.Projections.ProjectionInfo;
#else
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using CS = GeoAPI.CoordinateSystems.ICoordinateSystem;
#endif

namespace SharpMap
{
    using GeoAPI.Geometries;

    /// <summary>
    /// 
    /// </summary>
    public sealed class GeometryServices
    {

        private static readonly Microsoft.Practices.ServiceLocation.IServiceLocator ServiceLocator;

        private static readonly Dictionary<int, IGeometryFactory> SpatiallyReferencedFactories = new Dictionary<int, IGeometryFactory>();
        private static GeometryServices _instance;

        static GeometryServices()
        {
            ServiceLocator = Microsoft.Practices.ServiceLocation.ServiceLocator.Current;
            Debug.Assert(ServiceLocator != null, "ServiceLocator != null");
        }

        private GeometryServices()
        {
            GeometryFactory = ServiceLocator.GetInstance<IGeometryFactory>();
            CoordinateSequenceFactory = GeometryFactory.CoordinateSequenceFactory;
            
        }

        private static readonly object _lockObject = new object();
        /// <summary>
        /// Gets the current <see cref="GeometryServices"/> instance
        /// </summary>
        public static GeometryServices Instance 
        { 
            get
            {
                lock (_lockObject)
                {
                    return _instance ?? (_instance = new GeometryServices());
                }
            }
        }

        /// <summary>
        /// Gets a spatially referenced geometry factory. If none is found in the cache, a new one is created and added to the cache
        /// </summary>
        /// <param name="index">The coordinate system</param>
        public IGeometryFactory this[ICoordinateSystem index]
        {
            get 
            {
                if (index == null)
                    return GeometryFactory;

                return this[(int) index.AuthorityCode];
            }
        }

        /// <summary>
        /// Gets a spatially referenced geometry factory. If none is found in the cache, a new one is created and added to the cache
        /// </summary>
        /// <param name="srid">The coordinate system</param>
        public IGeometryFactory this[int srid]
        {
            get
            {
                if (srid <= 0)
                    return GeometryFactory;

                IGeometryFactory factory;
                if (SpatiallyReferencedFactories.TryGetValue(srid, out factory))
                    return factory;

                var pm = PrecisionModels.Floating;

                factory = ServiceLocator.GetInstance<IGeometryFactory>();
                Debug.Assert(factory.SRID == srid);

                SpatiallyReferencedFactories.Add(factory.SRID, factory);
                return factory;
            }
        }

        /// <summary>
        /// Default, not spatially specified geometry factory
        /// </summary>
        public readonly IGeometryFactory GeometryFactory ;

        ///<summary>
        /// The current coordinate sequence factory
        ///</summary>
        public readonly ICoordinateSequenceFactory CoordinateSequenceFactory;

        /// <summary>
        /// Returns a binary geometry writer
        /// </summary>
        /// <param name="key">The key of the binary geometry writer</param>
        /// <returns>The binary geometry writer.</returns>
        public IBinaryGeometryWriter GetBinaryWriter(string key)
        {
            var ret = ServiceLocator.GetInstance<IBinaryGeometryWriter>(key + "BinaryWriter");
            Debug.Assert(ret != null, "ret != null");
            return ret;
        }

        /// <summary>
        /// Returns a binary geometry reader
        /// </summary>
        /// <param name="key">The key of the binary geometry reader</param>
        /// <returns>The binary geometry reader.</returns>
        public IBinaryGeometryReader GetBinaryReader(string key)
        {
            var ret = ServiceLocator.GetInstance<IBinaryGeometryReader>(key + "BinaryReader");
            Debug.Assert(ret != null, "ret != null");
            return ret;
        }

        /// <summary>
        /// Returns a text geometry writer
        /// </summary>
        /// <param name="key">The key of the text geometry writer</param>
        /// <returns>The binary text writer.</returns>
        public IBinaryGeometryWriter GetTextWriter(string key)
        {
            var ret = ServiceLocator.GetInstance<IBinaryGeometryWriter>(key + "TextWriter");
            Debug.Assert(ret != null, "ret != null");
            return ret;
        }

        /// <summary>
        /// Returns a text geometry reader
        /// </summary>
        /// <param name="key">The key of the text geometry reader</param>
        /// <returns>The binary text reader.</returns>
        public IBinaryGeometryReader GetTextReader(string key)
        {
            var ret = ServiceLocator.GetInstance<IBinaryGeometryReader>(key + "TextReader");
            Debug.Assert(ret != null, "ret != null");
            return ret;
        }

        private static string GetInitializationString(string format, string authority, int code)
        {
            var webRequest = WebRequest.Create(string.Format(format, authority, code));
            string result;
            using (var webResponse = webRequest.GetResponse())
            {
                if (webResponse.ContentType != "mime/text")
                    throw new WebResponseFormatException();
                using (var stream = webResponse.GetResponseStream())
                {
                    Debug.Assert(stream != null, "stream != null");
                    var sr = new StreamReader(stream);
                    result = sr.ReadToEnd();
                }
            }
            return result;
        }


#pragma warning disable 1587
#if DotSpatialProjections
        /// <summary>
        /// Creates a
        /// </summary>
        /// <param name="wkt"></param>
        /// <returns></returns>
        public CS Create(string proj4)
        {
            return ProjectionInfo.ReadProj4String(proj4);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public CS Create(string authority, int code)
        {
            const string formatString = "http://spatialreference.org/ref/{0}/{1}/proj4/";
            return ProjectionInfo.ReadEsriString(GetInitializationString(formatString, authority, code));
        }
            
#else
        private readonly CoordinateSystemFactory _coordinateSytemFactory = new CoordinateSystemFactory();
        private readonly CoordinateTransformationFactory _coordinateTransformationFactory = new CoordinateTransformationFactory();
        
        /// <summary>
        /// Creates a
        /// </summary>
        /// <param name="wkt"></param>
        /// <returns></returns>
        public CS Create(string wkt)
        {
            return _coordinateSytemFactory.CreateFromWkt(wkt);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public CS Create(string authority, int code)
        {
            const string formatString = "http://spatialreference.org/ref/{0}/{1}/ogcwkt/";
            return _coordinateSytemFactory.CreateFromWkt(GetInitializationString(formatString, authority, code));
        }
#endif
#pragma warning restore 1587
    }
}