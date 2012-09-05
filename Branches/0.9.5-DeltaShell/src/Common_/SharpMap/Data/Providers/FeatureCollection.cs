using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Utils;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using System.Collections.ObjectModel;

namespace SharpMap.Data.Providers
{
    //TODO: get this class generic FeatureCollection<F>:where F: is IFeature. This will speed up and prettify the code :)
   
    public class FeatureCollection : IFeatureProvider, ITimeNavigatable
    {
        private long id;
        private int srid;
        private Type featureType;
        private IList features;
        private IEnumerable<DateTime> times;
        public FeatureCollection() 
        {
            features = new List<IFeature>();
            FilterFeaturesByTime();
        }

        public FeatureCollection(IList features, Type featureType)
        {
            if (!featureType.IsClass)
            {
                // We only accept a class because we want to use Activator to create object
                throw new ArgumentException("Can only instantiate FeatureCollection with class");
            }
            if (!typeof(IFeature).IsAssignableFrom((featureType)))
            {
                throw new ArgumentException("Feature type should be IFeature");
            }
            Features = features;
            FeatureType = featureType;
        }

        public virtual int GetFeatureCount()
        {
            return Features.Count;
        }

        public virtual IFeature GetFeature(int index)
        {
            return (IFeature) Features[index];
        }

        /// <summary>
        /// Get the feature by its geometry
        /// </summary>
        /// <param name="geom"></param>
        /// <returns></returns>
        public virtual IFeature GetFeature(IGeometry geom)
        {
            foreach (IFeature feature in Features)
            {
                if (feature.Geometry == geom)
                    return feature;
            }
            return null;
        }

        public virtual bool Contains(IFeature feature)
        {
            if (Features.Count == 0)
            {
                return false;
            }
            // Since Features can be strongly collection typed we must prevent searching objects of an invalid type
            if (FeatureType != null)
            {
                // test if feature we are looking for is derived from FeatureType
                if (!FeatureType.IsAssignableFrom(feature.GetType()))
                {
                    return false;
                }
            }
            else
            {
                // if FeatureType is not set use type of first object in collection.
                if (Features[0].GetType() != feature.GetType())
                {
                    return false;
                }
            }
            return Features.Contains(feature);
        }

        public virtual int IndexOf(IFeature feature)
        {
            if (Features.Count == 0 || Features[0].GetType() != feature.GetType())
            {
                return -1;
            }
            return Features.IndexOf(feature);
        }

        public virtual int SRID
        {
            get { return srid; }
            set { srid = value; }
        }

        
        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual IEnvelope GetExtents()
        {
            IEnvelope envelope = new Envelope();

            if (Features == null) 
                return envelope;

            foreach (IFeature feature in Features)
            {
                if(feature.Geometry == null)
                {
                    continue;
                }

                // HACK: probably we should not use EnvelopeInternal here but Envelope

                if (envelope.IsNull)
                {
                    envelope = (IEnvelope)feature.Geometry.EnvelopeInternal.Clone();
                }

                envelope.ExpandToInclude(feature.Geometry.EnvelopeInternal);
            }

            return envelope;
        }

        public virtual Collection<IGeometry> GetGeometriesInView(IEnvelope bbox)
        {
            Collection<IGeometry> result = new Collection<IGeometry>();

            int i = 0;
            foreach (IFeature feature in Features)
            {
                if (feature.Geometry.EnvelopeInternal.Intersects(bbox))
                {
                    result.Add(feature.Geometry);
                }
                i++;
            }

            return result;
        }

        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox, double minGeometrySize)
        {
            Collection<IGeometry> result = new Collection<IGeometry>();

            int i = 0;
            foreach (IFeature feature in Features)
            {
                if (feature.Geometry.EnvelopeInternal.Intersects(bbox))
                {
                    result.Add(feature.Geometry);
                }
                i++;
            }

            return result;
        }

        public virtual ICollection<int> GetObjectIDsInView(IEnvelope envelope)
        {
            Collection<int> ids = new Collection<int>();

            int i = 0;
            foreach (IFeature feature in Features)
            {
                if (feature.Geometry.EnvelopeInternal.Intersects(envelope))
                {
                    ids.Add(i);
                }
                i++;
            }

            return ids;
        }

        public virtual IGeometry GetGeometryByID(int oid)
        {
            return ((IFeature) Features[oid]).Geometry;
        }

        public virtual IList GetFeatures(IGeometry boundingGeometry)
        {
            IList intersectedFeatures = new ArrayList();
            IEnvelope box = boundingGeometry.EnvelopeInternal;

            foreach (IFeature feature in Features)
            {
                if (feature.Geometry == null)
                {
                    continue;
                }

                if (feature.Geometry.EnvelopeInternal.Intersects(box))
                {
                    if (feature.Geometry.Intersects(boundingGeometry))
                    {
                        intersectedFeatures.Add(feature);
                    }
                }
            }

            return intersectedFeatures;
        }

        public virtual IList GetFeatures(IEnvelope box)
        {
            IList intersectedFeatures = new ArrayList();

            if (Features != null)
            {
                foreach (IFeature feature in Features)
                {
                    if (feature.Geometry == null)
                    {
                        continue;
                    }

                    if (feature.Geometry.EnvelopeInternal.Intersects(box))
                    {
                        intersectedFeatures.Add(feature);
                    }
                }
            }
            return intersectedFeatures;
        }
        
        public virtual IList Features
        {
            get
            {
                if (times != null)
                {
                    return timeFilteredFeatures;
                }

                return features;
            }
            set
            {
                features = value;
                GuessFeatureType();

                if (features.Count > 0 && features[0] is ITimeDependent)
                {
                    times = features.Cast<ITimeDependent>().Select(f => f.Time).Distinct().OrderBy(t => t);
                }
                FilterFeaturesByTime();
            }
        }

        private void GuessFeatureType()
        {
            if(featureType != null)
            {
                return;
            }

            // try to obtain feature type from given collection of features
            Type featuresCollectionType = Features.GetType();
            if (featuresCollectionType.IsGenericType && !featuresCollectionType.IsInterface)
            {
                featureType = featuresCollectionType.GetGenericArguments()[0];
            }

            // guess feature type from the first feature
            if (featureType == null && Features.Count > 0)
            {
                featureType = Features[0].GetType();
            }
        }

        public virtual void Dispose()
        {
        }

        public virtual Type FeatureType
        {
            get { return featureType; }
            set { featureType = value; }
        }

        public virtual Func<IFeatureProvider,IGeometry,IFeature> AddNewFeatureFromGeometryDelegate { get; set; }

        public virtual IFeature Add(IGeometry geometry)
        {
            if (featureType == null)
            {
                GuessFeatureType();
                if (featureType == null)
                {
                    throw new NotSupportedException("FeatureType must be set in order to add a new feature geometry");
                }
            }

            IFeature newFeature;
            if (AddNewFeatureFromGeometryDelegate != null)
            {
                newFeature = AddNewFeatureFromGeometryDelegate(this, geometry);
            }
            else
            {
                newFeature = (IFeature) Activator.CreateInstance(featureType);
                newFeature.Geometry = geometry;
                Features.Add(newFeature);
            }

            return newFeature;
        }

        public virtual DateTime? TimeSelectionStart
        {
            get
            {
                return timeSelectionStart;
            }
            set 
            { 
                timeSelectionStart = value;
                FilterFeaturesByTime();
            }
        }

        public virtual DateTime? TimeSelectionEnd
        {
            get
            {
                return timeSelectionEnd;
            }
            set
            {
                timeSelectionEnd = value;
                FilterFeaturesByTime();
            }
        }

        private void FilterFeaturesByTime()
        {
            if(TimeSelectionStart == null || features.Count == 0 || !(features[0] is ITimeDependent))
            {
                timeFilteredFeatures = features;
                return;
            }

            timeFilteredFeatures = features.Cast<IFeature>()
                .Where(f =>
                           {
                               var timeDependent = f as ITimeDependent;

                               return timeDependent.Time >= TimeSelectionStart 
                                   && (TimeSelectionEnd == null || timeDependent.Time <= TimeSelectionEnd);
                           })
                .Select(f => f)
                .ToList();
        }

        public virtual IEnumerable<DateTime> Times
        {
            get { return times; }
        }

        private IList timeFilteredFeatures;
        private DateTime? timeSelectionStart;
        private DateTime? timeSelectionEnd;
    }
}