using GeoAPI.Geometries;
using SharpMap.Geometries;
using IGeometry = GeoAPI.Geometries.IGeometry;

namespace SharpMap.Data.Providers
{
    public delegate byte[] ToStoreGeometryHandler<in TGeometry>(TGeometry geometry)
        where TGeometry : IGeometry;

    public delegate TGeometry FromStoreGeometryHandler<out TGeometry>(byte[] bytes)
        where TGeometry : IGeometry;

    public delegate byte[] ToStoreEnvelopeHandler(Envelope envelope);

    public class DbGeometryConverter<TGeometry>
        where TGeometry : IGeometry
    {
        public DbGeometryConverter(
            string toStoreGeometryDecorator, 
            ToStoreGeometryHandler<TGeometry> toStoreGeometry, 
            string fromStoreGeometryDecorator, 
            FromStoreGeometryHandler<TGeometry> fromStoreGeometry, 
            ToStoreEnvelopeHandler toStoreEnvelope)
        {
            _toStoreGeometry = toStoreGeometry;
            _fromStoreGeometry = fromStoreGeometry;
            _toStoreGeometryDecorator = toStoreGeometryDecorator;
            _fromStoreGeometryDecorator = fromStoreGeometryDecorator;
            _toStoreEnvelope = toStoreEnvelope;
        }

        private readonly ToStoreGeometryHandler<TGeometry> _toStoreGeometry;
        private readonly FromStoreGeometryHandler<TGeometry> _fromStoreGeometry;
        private readonly ToStoreEnvelopeHandler _toStoreEnvelope;

        private readonly string _toStoreGeometryDecorator;
        public string ToStoreGeometryDecorator
        {
            get { return _toStoreGeometryDecorator; }
        }

        private readonly string _fromStoreGeometryDecorator;
        public string FromStoreGeometryDecorator
        {
            get { return _fromStoreGeometryDecorator; }
        }

        public TGeometry Read(byte[] bytes)
        {
            return _fromStoreGeometry(bytes);
        }

        public byte[] Write(TGeometry geometry)
        {
            return _toStoreGeometry(geometry);
        }
    }

    /*
    public class WkbGeometryConverter : DbGeometryConverter<IGeometry> 
    {
        public WkbGeometryConverter(
            string toStoreGeometryDecorator,
            string fromStoreGeometryDecorator, ToStoreEnvelopeHandler toStoreEnvelope) 
            : base(toStoreGeometryDecorator, 
                Converters.WellKnownBinary.GeometryToWKB.Write, 
                fromStoreGeometryDecorator, 
                Converters.WellKnownBinary.GeometryFromWKB.Parse,
                toStoreEnvelope
            )
        {
        }
    }
    */
}