namespace SharpMap
{
    using System;

    using GeoAPI.Geometries;

    using Microsoft.Practices.ServiceLocation;

    ///<summary>
    ///</summary>
    public class GeometryServices
    {

        private static GeometryServices _instance;

        protected GeometryServices()
        {
            GeometryFactory = ServiceLocator.Current.GetInstance<IGeometryFactory>();
            CoordinateSequenceFactory = GeometryFactory.CoordinateSequenceFactory;
            //GeometryIO = ServiceLocator.Current.GetInstance(IGeometryIO);

            //ServiceLocator.Current.
        }


        /// <summary>
        /// Gets the current <see cref="GeometryServices"/> instance
        /// </summary>
        public static GeometryServices Instance 
        { 
            get
            {
                return _instance ?? (_instance = new GeometryServices());
            }
        }

        public readonly IGeometryFactory GeometryFactory ;

        ///<summary>
        /// The current coordinate sequence factory
        ///</summary>
        public readonly ICoordinateSequenceFactory CoordinateSequenceFactory;

        public IGeometry Read(byte[] bytes)
        {}

        public byte[] Write(IGeometry geometry)
        {}

    }
}