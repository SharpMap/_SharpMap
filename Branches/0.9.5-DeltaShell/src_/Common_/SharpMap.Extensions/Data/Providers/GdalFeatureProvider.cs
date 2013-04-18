using System;
using System.Collections;
using System.Collections.Generic;
using DelftTools.Utils.IO;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using OSGeo.GDAL;
using SharpMap.Data.Providers;

namespace SharpMap.Extensions.Data.Providers
{
    /// <summary>
    /// Works as a proxy to GdalFunctionStore. 
    /// 
    /// Returns 1st function of the GdalFunctionStore as a feature.
    /// </summary>
    public class GdalFeatureProvider : IFeatureProvider, IFileBased
    {
        private long id;
 

        private readonly GdalFunctionStore store;

        public virtual IRegularGridCoverage Grid
        {
            get { return store.Grid; }
        }

        public GdalFeatureProvider()
        {
            store = new GdalFunctionStore();
        }

        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual Type FeatureType
        {
            get { return typeof(RegularGridCoverage); }
        }

        public virtual IList Features
        {
            get { return (IList)store.Functions; }
            set { throw new System.NotImplementedException(); }
        }

        public virtual IFeature Add(IGeometry geometry)
        {
            throw new System.NotImplementedException();
        }

        public virtual Func<IFeatureProvider, IGeometry, IFeature> AddNewFeatureFromGeometryDelegate { get; set; }

        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox)
        {
            return GetGeometriesInView(bbox, -1);
        }

        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox, double minGeometrySize)
        {
            return null;
        }

        public virtual ICollection<int> GetObjectIDsInView(IEnvelope envelope)
        {
            return null;
        }

        public virtual IGeometry GetGeometryByID(int oid)
        {
            return Grid.Geometry;
        }
        public virtual void ReConnect()
        {

        }

        public virtual void RelocateTo(string newPath)
        {
            throw new NotImplementedException();
        }

        public virtual void CopyTo(string newPath)
        {
            throw new NotImplementedException();
        }

        public virtual IList GetFeatures(IGeometry geom)
        {
            IList result = new ArrayList();

            if (Grid.Geometry.Intersects(geom))
            {
                result.Add(Grid);
            }

            return result;
        }

        public virtual IList GetFeatures(IEnvelope box)
        {
            IList result = new ArrayList();

            if (Grid.Geometry.EnvelopeInternal.Intersects(box))
            {
                result.Add(Grid);
            }

            return result;
        }

        public virtual int GetFeatureCount()
        {
            return 1;
        }

        public virtual IFeature GetFeature(int index)
        {
            return Grid;
        }

        public virtual bool Contains(IFeature feature)
        {
            // Only true if the grid itself is searched for
            return Grid.Equals(feature);
        }

        public virtual int IndexOf(IFeature feature)
        {
            // The grid is the only feature, which is 'at position 0' (the first element)
            return 0;
        }

        public virtual IEnvelope GetExtents()
        {
            return store.GetExtents();
        }

        public virtual string Path
        {
            get { return store.Path; }
            set { store.Path = value; }
        }

        public virtual void CreateNew(string path)
        {
            store.CreateNew(path);
        }

        public virtual void Close()
        {
            store.Close();
        }

        public virtual void Open(string path)
        {
            store.Open(path);
        }

        public virtual void Open()
        {
            if (!string.IsNullOrEmpty(Path))
            {
                Open(Path);
            }
        }

        public virtual bool IsOpen
        {
            get { return store.IsOpen; }
        }

        public virtual int SRID { get; set; }

        public virtual Dataset GdalDataset
        {
            get { return store.GdalDataset; }
        }

        public virtual void Dispose()
        {
        }

    }
}
