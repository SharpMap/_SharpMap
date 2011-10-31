using GeoAPI.Geometries;

namespace SharpMap.Web.Wms
{
    /// <summary>
    /// Spatial referenced boundingbox
    /// </summary>
    /// <remarks>
    /// The spatial referenced boundingbox is used to communicate boundingboxes of WMS layers together 
    /// with their spatial reference system in which they are specified
    /// </remarks>
    public class SpatialReferencedBoundingBox : Envelope
    {
        int _srid;

        /// <summary>
        /// Initializes a new SpatialReferencedBoundingBox which stores a boundingbox together with the SRID
        /// </summary>
        /// <remarks>This class is used to communicate all the boundingboxes of a WMS server between client.cs and wmslayer.cs</remarks>
        /// <param name="minX">The minimum x-ordinate value</param>
        /// <param name="minY">The minimum y-ordinate value</param>
        /// <param name="maxX">The maximum x-ordinate value</param>
        /// <param name="maxY">The maximum y-ordinate value</param>
        /// <param name="srid">The spatial reference ID</param>
        public SpatialReferencedBoundingBox(double minX, double minY, double maxX, double maxY, int srid) : base(minX, minY, maxX, maxY)
        {
            _srid = srid;
        }

        /// <summary>
        /// The spatial reference ID (CRS)
        /// </summary>
        public int SRID
        {
            get { return _srid; }
            set { _srid = value; }
        }
    }
}
