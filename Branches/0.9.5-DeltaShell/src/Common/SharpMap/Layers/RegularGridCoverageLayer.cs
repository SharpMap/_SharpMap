using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Utils;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Geometries;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;

namespace SharpMap.Layers
{
    [NotifyPropertyChange]
    public class RegularGridCoverageLayer : Layer, IRegularGridCoverageLayer, ITimeNavigatable
    {
        private IRegularGridCoverage grid;

        private RegularGridCoverageRenderer renderer;
        private DateTime? timeSelectionStart;
        private DateTime? timeSelectionEnd;

        public override IList<IFeatureRenderer> CustomRenderers
        {
            get
            {
                if(renderer == null && base.CustomRenderers.Count == 0)
                {
                    renderer = new RegularGridCoverageRenderer(this);
                    base.CustomRenderers.Add(renderer);
                }

                return base.CustomRenderers;
            }
            set { base.CustomRenderers = value; }
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
                grid.ValuesChanged += Grid_ValuesChanged;

                if (Grid.IsTimeDependent)
                {
                    grid.Time.ValuesChanged += TimeValuesChanged;
                }
            }
            
        }

        private void Grid_ValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            RenderRequired = true;
        }

        private void UnSubscribeToGridEvents()
        {
            if (grid != null)
            {
                ((INotifyPropertyChanged) grid).PropertyChanged -= Grid_PropertyChanged;
                grid.ValuesChanged -= Grid_ValuesChanged;

                if (grid.IsTimeDependent)
                {
                    grid.Time.ValuesChanged -= TimeValuesChanged;
                }
            }
        }

        void Grid_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RenderRequired = true;
        }

        public override void OnRender(Graphics g, Map map)
        {
            if(CustomRenderers.Count > 0 && Grid != null)
            {
                foreach (IFeatureRenderer customRenderer in CustomRenderers)
                {
                    customRenderer.Render(Grid, g, this);
                }

                return; 
            }
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

        void TimeValuesChanged(object sender, FunctionValuesChangingEventArgs e)
        {
            if (TimesChanged != null)
            {
                TimesChanged();
            }
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
        }

        public virtual DateTime? TimeSelectionEnd
        {
            get { return timeSelectionEnd; }
        }

        public virtual TimeNavigatableLabelFormatProvider CustomDateTimeFormatProvider
        {
            get { return null; }
        }

        public virtual void SetCurrentTimeSelection(DateTime? start, DateTime? end)
        {
            if (start == null)
            {
                throw new InvalidOperationException("Cannot render null time");
            }
            timeSelectionStart = start;
            UpdateRenderedCoverage(start);
            RenderRequired = true;
        }

        public virtual event Action CurrentTimeSelectionChanged;

        private void UpdateRenderedCoverage(DateTime? start)
        {
            var timeFilter = RenderedCoverage.Filters.OfType<IVariableValueFilter>().FirstOrDefault();
            if (timeFilter != null)
            {
                timeFilter.Values[0] = start.Value;
            }
        }

        public virtual IEnumerable<DateTime> Times
        {
            get
            {
                if (Coverage.Time == null)
                {
                    return new List<DateTime>();
                }
                return Coverage.Time.Values;
            }
        }

        public virtual event Action TimesChanged;

        private IRegularGridCoverage renderedCoverage;
        public virtual IRegularGridCoverage RenderedCoverage
        {
            get
            {
                if (renderedCoverage == null)
                {
                    if (Coverage.IsTimeDependent)
                    {
                        var timeValues = Coverage.Time.Values;
                        var time = timeValues.Count > 0 ? timeValues[0] : DateTime.MinValue;
                        renderedCoverage = (IRegularGridCoverage) Grid.FilterTime(time);
                    }
                    else
                    {
                        renderedCoverage = Grid;
                    }
                }
                return renderedCoverage;
            }
        }

        public virtual TimeSelectionMode SelectionMode
        {
            get { return TimeSelectionMode.Single; }
        }

        public virtual SnappingMode SnappingMode
        {
            get { return SnappingMode.Nearest; }
        }
    }
}
