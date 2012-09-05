using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using DelftTools.Utils;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;

namespace SharpMap.Layers
{
    [NotifyPropertyChanged]
    public class RegularGridCoverageLayer : Layer, IRegularGridCoverageLayer, ITimeNavigatable
    {
        private IRegularGridCoverage grid;

        private RegularGridCoverageRenderer renderer;
        private DateTime? timeSelectionStart;
        private DateTime? timeSelectionEnd;

        public RegularGridCoverageLayer()
        {
            renderer = new RegularGridCoverageRenderer(this);
            CustomRenderers.Add(renderer);
        }

        /// <summary>
        /// Returns the extent of the layer
        /// </summary>
        /// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
        public override IEnvelope Envelope
        {
            get
            {
                if(grid != null && grid.Geometry != null)
                {
                    return grid.Geometry.EnvelopeInternal;
                }

                return DataSource.GetExtents();
            }
        }

        public virtual IRegularGridCoverage Grid
        {
            get
            {
                if(grid == null && DataSource != null && DataSource.Features.Count > 0)
                {
                    // use grid from the underlying DataSource
                    return (IRegularGridCoverage) DataSource.GetFeature(0); 
                }
                
                return grid;
            }
            set
            {
                UnSubscribeToGridEvents();
                grid = value;
                SubscribeToGridEvents();
                RenderRequired = true;
            }
        }

        private void SubscribeToGridEvents()
        {
            if (grid != null)
            {
                ((INotifyPropertyChanged)grid).PropertyChanged += Grid_PropertyChanged;    
            }
            
        }

        private void UnSubscribeToGridEvents()
        {
            if (grid != null)
            {
                ((INotifyPropertyChanged) grid).PropertyChanged -= Grid_PropertyChanged;
            }
        }

        void Grid_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RenderRequired = true;
        }

        public override void OnRender(Graphics g, Map map)
        {
            if(CustomRenderers.Count > 0)
            {
                foreach (IFeatureRenderer customRenderer in CustomRenderers)
                {
                    customRenderer.Render(Grid, g, this);
                }

                return; // do not use default renderer when custom rederers are set!
            }

            //renderer.Render(Grid, g, this);
        }

        /// <summary>
        /// Clones the layer
        /// </summary>
        /// <returns>cloned object</returns>
        public override object Clone()
        {
            var gridCoverageLayerClone = (RegularGridCoverageLayer) base.Clone();
            
            gridCoverageLayerClone.Grid = Grid;

            return gridCoverageLayerClone;
        }

        public virtual ICoverage Coverage
        {
            get { return Grid; }
            set { Grid = (IRegularGridCoverage) value; }
        }

        public virtual void SetCurrentTime(DateTime value)
        {
            //TODO: implement it nicely ala NetworkCoverageLayer
        }

        public override bool ReadOnly
        {
            get
            {
                if (Grid != null)
                {
                    return !Grid.IsEditable;
                }
                return false;
            }
        }

        public virtual DateTime? TimeSelectionStart
        {
            get { return timeSelectionStart; }
            set
            {
                timeSelectionStart = value;
                RenderRequired = true;
            }
        }

        public virtual DateTime? TimeSelectionEnd
        {
            get { return timeSelectionEnd; }
            set { throw new NotImplementedException(); }
        }

        public virtual IEnumerable<DateTime> Times
        {
            get { return Coverage.Time.Values; }
        }
    }
}
