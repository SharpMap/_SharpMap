using System;
using System.Collections.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Coverages;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Editors.Interactors;
using SharpMap.Editors.Snapping;
using SharpMap.Layers;

namespace SharpMap.Editors
{
    public class FeatureEditor : IFeatureEditor
    {
        private IList<ISnapRule> snapRules;

        public virtual IList<ISnapRule> SnapRules
        {
            get
            {
                if (snapRules == null)
                {
                    snapRules = new List<ISnapRule>();
                }

                return snapRules;
            }

            set
            {
                snapRules = value;
            }
        }

        public virtual IFeature AddNewFeatureByGeometry(ILayer layer, IGeometry geometry)
        {
            if (CreateNewFeature != null)
            {
                var feature = CreateNewFeature(layer);
                feature.Geometry = geometry;
                AddFeatureToDataSource(layer, feature);
                return feature;
            }

            if(addingNewFeature)
            {
                throw new InvalidOperationException("loop detected, something is wrong with your feature provider (check AddNewFeatureFromGeometryDelegate)");
            }

            addingNewFeature = true;
            IFeature newFeature = null;

            try
            {
                newFeature = (IFeature)Activator.CreateInstance(layer.DataSource.FeatureType);
                newFeature.Geometry = geometry;
                AddFeatureToDataSource(layer, newFeature);
            }
            finally
            {
                addingNewFeature = false;
            }

            return newFeature;
        }

        protected virtual void AddFeatureToDataSource(ILayer layer, IFeature feature)
        {
            layer.DataSource.Features.Add(feature);
        }

        private bool addingNewFeature;

        public virtual Func<ILayer, IFeature> CreateNewFeature { get; set; }

        public virtual IFeatureInteractor CreateInteractor(ILayer layer, IFeature feature)
        {
            if (null == feature)
                return null;

            var vectorLayer = layer as VectorLayer;
            var vectorStyle = (vectorLayer != null ? vectorLayer.Style : null);

            // most specific type should be first
            if (feature is RegularGridCoverageCell)
                return new LineStringInteractor(layer, feature, vectorStyle, null);

            if (feature is IGridFace || feature is IGridVertex)
                return new LineStringInteractor(layer, feature, vectorStyle, null);
            
            if (feature.Geometry is ILineString)
                return new LineStringInteractor(layer, feature, vectorStyle, null);
            
            if (feature.Geometry is IPoint)
                return new PointInteractor(layer, feature, vectorStyle, null);
            
            // todo implement custom mutator for Polygon and MultiPolygon
            // LineStringMutator will work as long as moving is not supported.
            if (feature.Geometry is IPolygon)
                return new LineStringInteractor(layer, feature, vectorStyle, null);
            
            if (feature.Geometry is IMultiPolygon)
                return new LineStringInteractor(layer, feature, vectorStyle, null);
            
            return null;
            //throw new ArgumentException("Unsupported type " + feature.Geometry);
        }
    }
}