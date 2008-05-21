using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using SharpMap.Geometries;
using SharpMap.Layers;

namespace SharpMap.Data.Providers
{
    public class BufferedDataProviderProxy<TRealProvider>
        : IProvider
        where TRealProvider : IProvider
    {
        public BufferedDataProviderProxy(TRealProvider realProvider)
            : this(realProvider, 1.0)
        { }


        public BufferedDataProviderProxy(TRealProvider realProvider, double bufferScaleFactor)
        {
            this.RealProvider = realProvider;
            this.BufferScaleFactor = bufferScaleFactor;

        }

        TRealProvider _realProvider;
        /// <summary>
        /// Returns the real provider being proxied.
        /// </summary>
        public TRealProvider RealProvider
        {
            get
            {
                return _realProvider;
            }
            private set
            {
                _realProvider = value;
            }
        }

        private double _bufferScaleFactor;
        /// <summary>
        /// The scale factor we are multiplying the box by.
        /// A scale factor &gt; 1.0 will expand the BoundingBox used in ExecuteIntersectionQuery
        /// &lt; 1.0 will shrink the BoundingBox
        /// </summary>
        public double BufferScaleFactor
        {
            get { return _bufferScaleFactor; }
            set { _bufferScaleFactor = value; }
        }

        #region IProvider Members

        /// <summary>
        /// returns a BoundingBox whos width and height are 
        /// scaled by BufferScaleFactor
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        private BoundingBox ExpandBox(BoundingBox box)
        {
            double dx = (box.Width / 2) * BufferScaleFactor;
            double dy = (box.Height / 2) * BufferScaleFactor;

            Point p = box.GetCentroid();
            return new BoundingBox(
                p.X - dx,
                p.Y - dy,
                p.X + dx,
                p.Y + dy);

        }

        public Collection<Geometry> GetGeometriesInView(BoundingBox bbox)
        {
            return RealProvider.GetGeometriesInView(ExpandBox(bbox));
        }

        public Collection<uint> GetObjectIDsInView(BoundingBox bbox)
        {
            return RealProvider.GetObjectIDsInView(ExpandBox(bbox));
        }

        public Geometry GetGeometryByID(uint oid)
        {
            return RealProvider.GetGeometryByID(oid);
        }

        public void ExecuteIntersectionQuery(Geometry geom, FeatureDataSet ds)
        {
            /* Buffer is not implemented in any of the Geometries 
             * so we will just relay to the real provider. 
             * Alternatively you could use NTS to buffer properly.
             */
            RealProvider.ExecuteIntersectionQuery(geom, ds);
        }

        public void ExecuteIntersectionQuery(BoundingBox box, FeatureDataSet ds)
        {
            RealProvider.ExecuteIntersectionQuery(ExpandBox(box), ds);
        }

        public int GetFeatureCount()
        {
            return RealProvider.GetFeatureCount();
        }

        public FeatureDataRow GetFeature(uint RowID)
        {
            return RealProvider.GetFeature(RowID);
        }

        public BoundingBox GetExtents()
        {
            return RealProvider.GetExtents();
        }

        public string ConnectionID
        {
            get { return RealProvider.ConnectionID; }
        }

        public void Open()
        {
            RealProvider.Open();
        }

        public void Close()
        {
            RealProvider.Close();
        }

        public bool IsOpen
        {
            get { return RealProvider.IsOpen; }
        }

        public int SRID
        {
            get
            {
                return RealProvider.SRID;
            }
            set
            {
                RealProvider.SRID = value;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            RealProvider.Dispose();
        }

        #endregion
    }
}
